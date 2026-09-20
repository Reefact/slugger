using FirstClassErrors;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

namespace Slugger.Domain.Analysis;

/// <summary>
/// Measures a theme without judging it. Where <see cref="ThemeValidator"/> answers whether a
/// theme loads, this answers by how much - which is the question an author cannot ask today.
/// </summary>
/// <remarks>
/// It composes the validator rather than repeating it: the refusals and remarks come from
/// <see cref="ThemeValidator"/> itself, so there is one answer to "is this theme good" and not
/// two that can drift apart.
/// </remarks>
internal static class ThemeAnalyzer
{
    /// <summary>Measures a theme that could be built, refusals and all.</summary>
    /// <param name="theme">The theme to measure.</param>
    internal static ThemeAnalysis Analyze(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        ThemeResolver resolver = new(theme);

        return new ThemeAnalysis(
            theme.Name,
            ThemeValidator.Validate(theme),
            ThemeValidator.Remarks(theme),
            Measure(theme, resolver));
    }

    /// <summary>Reports a document that could not be read at all, which leaves nothing to measure.</summary>
    /// <param name="name">The theme the report is about.</param>
    /// <param name="refusals">Why it could not be read.</param>
    internal static ThemeAnalysis Unreadable(string name, IReadOnlyList<Error> refusals) =>
        new(name, refusals, [], Measurements: null);

    private static ThemeMeasurements Measure(Theme theme, ThemeResolver resolver)
    {
        ThemeCombinatorics combinatorics = new(resolver);
        Exposure[] exposure = [.. ExposureOfEveryAdjective(theme)];
        SegmentMode drawn = ThemeValidator.DrawnMode(theme);

        return new ThemeMeasurements(
            Nouns: theme.Nouns.Count,
            DistinctNouns: theme.Nouns.Select(noun => noun.Value).Distinct(StringComparer.Ordinal).Count(),
            Drawn: drawn,
            Adjectives: Poorest(theme, noun => resolver.Pool(noun).Count, AdjectiveFloor(drawn)),
            Participles: theme.HasParticiples
                ? Poorest(theme, noun => resolver.ParticiplePool(noun).Count, ParticipleFloor(drawn))
                : null,
            WordsBeforeTheNoun: drawn == SegmentMode.Either
                ? Poorest(
                    theme,
                    noun => resolver.Pool(noun).Count + resolver.ParticiplePool(noun).Count,
                    ThemeValidator.MinimumPoolPerNoun)
                : null,
            Combinations: PoorestCategory(theme, combinatorics),
            TotalCombinations: combinatorics.Total(),
            CombinationsDrawn: combinatorics.Total(drawn),
            DuplicatedNouns: [.. Duplicated(theme)],
            UnreachableCategories: [.. Unreachable(theme)],
            LeastExposed: exposure.MinBy(word => word.Nouns) ?? new Exposure(string.Empty, 0),
            MostExposed: exposure.MaxBy(word => word.Nouns) ?? new Exposure(string.Empty, 0),
            TwoWordAdjectives: Words(theme.Adjectives).Count(Compound),
            TotalAdjectives: Words(theme.Adjectives).Count(),
            TwoWordNouns: theme.Nouns.Count(noun => Compound(noun.Value)),
            LongestSlugSegments: LongestSlug(theme));
    }

    /// <summary>
    /// The floors the report shows are the ones a load would apply, and no others: a margin
    /// against a floor that does not hold is worse than no margin at all. Both are null where
    /// the mode asks nothing of that pool - the count stays, the threshold goes.
    /// </summary>
    private static int? AdjectiveFloor(SegmentMode drawn) =>
        drawn is SegmentMode.Adjective or SegmentMode.Both ? ThemeValidator.MinimumPoolPerNoun : null;

    /// <inheritdoc cref="AdjectiveFloor"/>
    private static int? ParticipleFloor(SegmentMode drawn) => drawn switch
    {
        SegmentMode.Participle => ThemeValidator.MinimumPoolPerNoun,
        SegmentMode.Both => ThemeValidator.MinimumParticiplePoolPerNoun,
        _ => null
    };

    private static PoolFloor Poorest(Theme theme, Func<Noun, int> size, int? floor)
    {
        Noun poorest = theme.Nouns.MinBy(size)!;

        return new PoolFloor(size(poorest), poorest.Value, floor);
    }

    private static CategoryFloor? PoorestCategory(Theme theme, ThemeCombinatorics combinatorics)
    {
        string[] inUse = [.. theme.Nouns.SelectMany(noun => noun.Categories).Distinct(StringComparer.Ordinal)];
        if (inUse.Length == 0)
        {
            return null;
        }

        string poorest = inUse.MinBy(combinatorics.CombinationsForCategory)!;

        return new CategoryFloor(
            combinatorics.CombinationsForCategory(poorest),
            poorest,
            ThemeValidator.MinimumCombinationsPerCategory);
    }

    /// <summary>
    /// A value written twice is drawn twice as often, because the draw indexes the list while
    /// the size rule counts distinct values - so the file looks right and the odds are not.
    /// </summary>
    private static IEnumerable<string> Duplicated(Theme theme) => theme.Nouns
        .GroupBy(noun => noun.Value, StringComparer.Ordinal)
        .Where(group => group.Count() > 1)
        .Select(group => group.Key)
        .Order(StringComparer.Ordinal);

    /// <summary>
    /// The mirror of the rule that refuses a noun naming a category the theme does not declare:
    /// nothing looks the other way, so a category nobody carries is simply never drawn from.
    /// </summary>
    private static IEnumerable<string> Unreachable(Theme theme)
    {
        HashSet<string> carried = new(
            theme.Nouns.SelectMany(noun => noun.Categories).Append(ThemeResolver.CommonCategory),
            StringComparer.Ordinal);

        return theme.Adjectives.Keys
            .Concat(theme.Participles.Keys)
            .Distinct(StringComparer.Ordinal)
            .Where(category => !carried.Contains(category))
            .Order(StringComparer.Ordinal);
    }

    private static IEnumerable<Exposure> ExposureOfEveryAdjective(Theme theme)
    {
        Dictionary<string, int> reached = new(StringComparer.Ordinal);
        foreach (Noun noun in theme.Nouns)
        {
            HashSet<string> categories = new(noun.Categories, StringComparer.Ordinal) { ThemeResolver.CommonCategory };
            foreach (string word in categories.Where(theme.Adjectives.ContainsKey).SelectMany(c => theme.Adjectives[c]).Distinct(StringComparer.Ordinal))
            {
                reached[word] = reached.GetValueOrDefault(word) + 1;
            }
        }

        return reached.Select(pair => new Exposure(pair.Key, pair.Value));
    }

    /// <summary>
    /// An upper bound, not a draw: the longest adjective, the longest participle and the longest
    /// noun need not meet, but a slug can never exceed their sum. Both a value and a word can be
    /// compound, so the two multiply.
    /// </summary>
    private static int LongestSlug(Theme theme)
    {
        int longest = Words(theme.Adjectives).DefaultIfEmpty(string.Empty).Max(SegmentsIn)
            + theme.Nouns.Select(noun => noun.Value).DefaultIfEmpty(string.Empty).Max(SegmentsIn);

        return theme.HasParticiples
            ? longest + Words(theme.Participles).DefaultIfEmpty(string.Empty).Max(SegmentsIn)
            : longest;
    }

    private static IEnumerable<string> Words(IReadOnlyDictionary<string, IReadOnlyList<string>> groups) =>
        groups.Values.SelectMany(words => words).Distinct(StringComparer.Ordinal);

    private static bool Compound(string value) => value.Contains(' ', StringComparison.Ordinal);

    private static int SegmentsIn(string value) => value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
