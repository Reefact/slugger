#region Usings declarations

using FirstClassErrors;

using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Domain.Analysis;

/// <summary>
///     Measures a theme without judging it. Where <see cref="ThemeValidator" /> answers whether a
///     theme loads, this answers by how much - which is the question an author cannot ask today.
/// </summary>
/// <remarks>
///     It composes the validator rather than repeating it: the refusals and remarks come from
///     <see cref="ThemeValidator" /> itself, so there is one answer to "is this theme good" and not
///     two that can drift apart.
/// </remarks>
internal static class ThemeAnalyzer {

    #region Static members

    /// <summary>Measures a theme that could be built, refusals and all.</summary>
    /// <param name="theme">The theme to measure.</param>
    internal static ThemeAnalysis Analyze(ThemeDocument theme) {
        ArgumentNullException.ThrowIfNull(theme);

        return Analyze(ThemeResolver.AsDeclared(theme), GenerationOptions.Default.WithDefaultsOf(theme));
    }

    /// <summary>
    ///     Measures a surface already resolved, which is how a run's <c>--max-length</c> is reported:
    ///     the report answers for the theme the run will actually draw from, not for the one on disk
    ///     (DEC0018).
    /// </summary>
    /// <param name="resolver">The surface to measure, whole or already narrowed.</param>
    /// <param name="style">How the slug will be formatted, which is what decides its length.</param>
    internal static ThemeAnalysis Analyze(ThemeResolver resolver, GenerationOptions style) {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(style);

        return new ThemeAnalysis(
            resolver.Document.Name,
            ThemeValidator.Validate(resolver),
            ThemeValidator.Remarks(resolver.Document),
            Measure(resolver.Document, resolver, style));
    }

    /// <summary>Reports a document that could not be read at all, which leaves nothing to measure.</summary>
    /// <param name="name">The theme the report is about.</param>
    /// <param name="refusals">Why it could not be read.</param>
    internal static ThemeAnalysis Unreadable(string name, IReadOnlyList<Error> refusals) {
        return new ThemeAnalysis(name, refusals, [], null);
    }

    private static ThemeMeasurements Measure(ThemeDocument theme, ThemeResolver resolver, GenerationOptions style) {
        ThemeCombinatorics combinatorics = new(resolver);
        Exposure[]         exposure      = [.. ExposureOfEveryAdjective(theme)];
        SegmentMode        drawn         = ThemeValidator.DrawnMode(resolver);
        int                wordsBefore   = drawn.PutsAParticipleBesideAnAdjective() ? 2 : 1;

        return new ThemeMeasurements(
            theme.Nouns.Count,
            theme.Nouns.Select(noun => noun.Value).Distinct(StringComparer.Ordinal).Count(),
            drawn,
            Poorest(theme, noun => resolver.Pool(noun).Count, AdjectiveFloor(drawn)),
            theme.HasParticiples
                ? Poorest(theme, noun => resolver.ParticiplePool(noun).Count, ParticipleFloor(drawn))
                : null,
            drawn == SegmentMode.Either
                ? Poorest(
                    theme,
                    noun => resolver.Pool(noun).Count + resolver.ParticiplePool(noun).Count,
                    ThemeValidator.MinimumPoolPerNoun)
                : null,
            PoorestCouple(theme, resolver, drawn),
            PoorestCategory(resolver, combinatorics),
            combinatorics.Total(),
            combinatorics.Total(drawn),
            ThemeValidator.Longest(resolver, wordsBefore, style) ?? string.Empty,
            resolver.Budget?.MaxLength                           ?? theme.MaxLength.For(drawn),
            [.. Duplicated(theme)],
            [.. Unreachable(resolver)],
            exposure.MinBy(word => word.Nouns) ?? new Exposure(string.Empty, 0),
            exposure.MaxBy(word => word.Nouns) ?? new Exposure(string.Empty, 0),
            Words(theme.Adjectives).Count(Compound),
            Words(theme.Adjectives).Count(),
            theme.Nouns.Count(noun => Compound(noun.Value)),
            LongestSlug(theme));
    }

    /// <summary>
    ///     The floors the report shows are the ones a load would apply, and no others: a margin
    ///     against a floor that does not hold is worse than no margin at all. Both are null where
    ///     the mode asks nothing of that pool - the count stays, the threshold goes.
    /// </summary>
    private static int? AdjectiveFloor(SegmentMode drawn) {
        return drawn is SegmentMode.Adjective || drawn.PutsAParticipleBesideAnAdjective()
            ? ThemeValidator.MinimumPoolPerNoun
            : null;
    }

    /// <inheritdoc cref="AdjectiveFloor" />
    private static int? ParticipleFloor(SegmentMode drawn) {
        return drawn switch {
            SegmentMode.Participle                     => ThemeValidator.MinimumPoolPerNoun,
            SegmentMode.Both or SegmentMode.ThreeOrTwo => ThemeValidator.MinimumParticiplePoolPerNoun,
            _                                          => null
        };
    }

    private static PoolFloor Poorest(ThemeDocument theme, Func<NounEntry, int> size, int? floor) {
        NounEntry poorest = theme.Nouns.MinBy(size)!;

        return new PoolFloor(size(poorest), poorest.Value, floor);
    }

    /// <summary>
    ///     What an incompatibility costs at its worst, which the unconditional participle count
    ///     cannot show: a noun with 40 participles and an adjective refusing 35 of them reads as
    ///     comfortable and is not.
    /// </summary>
    private static CoupleFloor? PoorestCouple(ThemeDocument theme, ThemeResolver resolver, SegmentMode drawn) {
        if (!drawn.PutsAParticipleBesideAnAdjective() || (!theme.HasIncompatibilities && resolver.Budget is null)) { return null; }

        (NounEntry Noun, string Adjective, int Left)? worst = null;
        foreach (NounEntry noun in theme.Nouns) {
            if (ThemeValidator.Starved(noun, resolver) is not { } starved) { continue; }

            if (worst is null || starved.Left < worst.Value.Left) {
                worst = (noun, starved.Adjective, starved.Left);
            }
        }

        return worst is { } found
            ? new CoupleFloor(
                found.Left,
                found.Noun.Value,
                found.Adjective,
                ThemeValidator.MinimumParticiplePoolPerNoun)
            : null;
    }

    /// <summary>
    ///     The categories walked are the ones a noun that is still drawn carries, which is the set
    ///     ThemeValidator measures. Reading the whole theme instead reports a floor for a category
    ///     nothing reaches any more - zero combinations against a floor of forty thousand - and the
    ///     report then contradicts its own verdict on a narrowed surface (DEC0023).
    /// </summary>
    private static CategoryFloor? PoorestCategory(ThemeResolver resolver, ThemeCombinatorics combinatorics) {
        string[] inUse = [.. resolver.Nouns.SelectMany(noun => noun.Categories).Distinct(StringComparer.Ordinal)];
        if (inUse.Length == 0) { return null; }

        string poorest = inUse.MinBy(combinatorics.CombinationsForCategory)!;

        return new CategoryFloor(
            combinatorics.CombinationsForCategory(poorest),
            poorest,
            ThemeValidator.MinimumCombinationsPerCategory);
    }

    /// <summary>
    ///     A value written twice is drawn twice as often, because the draw indexes the list while
    ///     the size rule counts distinct values - so the file looks right and the odds are not.
    /// </summary>
    private static IEnumerable<string> Duplicated(ThemeDocument theme) {
        return theme.Nouns
                    .GroupBy(noun => noun.Value, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .Order(StringComparer.Ordinal);
    }

    /// <summary>
    ///     The mirror of the rule that refuses a noun naming a category the theme does not declare:
    ///     nothing looks the other way, so a category nobody carries is simply never drawn from.
    /// </summary>
    private static IEnumerable<string> Unreachable(ThemeResolver resolver) {
        ThemeDocument theme = resolver.Document;
        HashSet<string> carried = new(
            resolver.Nouns.SelectMany(noun => noun.Categories).Append(ThemeResolver.CommonCategory),
            StringComparer.Ordinal);

        return theme.Adjectives.Keys
                    .Concat(theme.Participles.Keys)
                    .Distinct(StringComparer.Ordinal)
                    .Where(category => !carried.Contains(category))
                    .Order(StringComparer.Ordinal);
    }

    private static IEnumerable<Exposure> ExposureOfEveryAdjective(ThemeDocument theme) {
        Dictionary<string, int> reached = new(StringComparer.Ordinal);
        foreach (NounEntry noun in theme.Nouns) {
            HashSet<string> categories = new(noun.Categories, StringComparer.Ordinal) { ThemeResolver.CommonCategory };
            foreach (string word in categories.Where(theme.Adjectives.ContainsKey).SelectMany(c => theme.Adjectives[c]).Distinct(StringComparer.Ordinal)) {
                reached[word] = reached.GetValueOrDefault(word) + 1;
            }
        }

        return reached.Select(pair => new Exposure(pair.Key, pair.Value));
    }

    /// <summary>
    ///     An upper bound, not a draw: the longest adjective, the longest participle and the longest
    ///     noun need not meet, but a slug can never exceed their sum. Both a value and a word can be
    ///     compound, so the two multiply.
    /// </summary>
    private static int LongestSlug(ThemeDocument theme) {
        int longest = Words(theme.Adjectives).DefaultIfEmpty(string.Empty).Max(SegmentsIn)
                    + theme.Nouns.Select(noun => noun.Value).DefaultIfEmpty(string.Empty).Max(SegmentsIn);

        return theme.HasParticiples
            ? longest + Words(theme.Participles).DefaultIfEmpty(string.Empty).Max(SegmentsIn)
            : longest;
    }

    private static IEnumerable<string> Words(IReadOnlyDictionary<string, IReadOnlyList<string>> groups) {
        return groups.Values.SelectMany(words => words).Distinct(StringComparer.Ordinal);
    }

    private static bool Compound(string value) {
        return value.Contains(' ', StringComparison.Ordinal);
    }

    private static int SegmentsIn(string value) {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    #endregion

}