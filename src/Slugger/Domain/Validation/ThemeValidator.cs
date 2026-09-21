using FirstClassErrors;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;

namespace Slugger.Domain.Validation;

/// <summary>
/// Everything that must hold before a theme may be used, checked on resolved pools rather
/// than on raw list counts:
/// <list type="number">
///   <item>at least one noun, because a theme with none cannot draw;</item>
///   <item>every category a noun references is declared, in "adjectives" or in "participles";</item>
///   <item>at least <see cref="MinimumNouns"/> distinct nouns;</item>
///   <item>every noun reaches at least <see cref="MinimumPoolPerNoun"/> words of whatever the
///         theme's segment mode puts in front of it;</item>
///   <item>every category totals at least <see cref="MinimumCombinationsPerCategory"/> combinations.</item>
/// </list>
/// The last three are waived by the theme's own <c>allowSmall</c> or by <c>--allow-small-theme</c>.
/// The first two never are: they describe an incoherent file, not a small one.
/// </summary>
/// <remarks>
/// <para>
/// <b>It never stops at the first failure.</b> Every rule runs over every noun and every
/// category, and the result carries all of them, so one run tells a theme author everything
/// their file needs rather than one thing per run.
/// </para>
/// <para>
/// The per-noun floor follows the theme's own <c>defaults.segmentMode</c> (DEC0016), because
/// what repeats is what the mode draws: "either" draws one word from the two pools at once
/// (DEC0015), so their sum carries the floor and neither has one of its own; "both" draws one of each, so each
/// has its own. The per-category floor does not follow it, and counts the two pools multiplied
/// whatever the mode: it asks whether a branch of the theme is worth carrying, and
/// <c>--segment</c> reaches that whole space from any theme.
/// </para>
/// <para>
/// Rule 1 accepts a category declared in "participles" alone (DEC0002). It need only exist in
/// "adjectives", but heroku's nouns reference six capability categories - eau, mobile, lumineux,
/// sonore, vivant, chaleur - that only "participles" declares, and the literal rule refuses the
/// shipped theme. See <see cref="ThemeResolver"/> for the other half of that reading.
/// </para>
/// </remarks>
public static class ThemeValidator
{
    /// <summary>Distinct nouns a theme needs before it is accepted.</summary>
    public const int MinimumNouns = 100;

    /// <summary>
    /// Per noun, on whichever pool the theme's segment mode draws the word before the noun from:
    /// pool(noun) under "adjective", partPool(noun) under "participle", the two added under
    /// "either".
    /// </summary>
    public const int MinimumPoolPerNoun = 100;

    /// <summary>
    /// Per noun, on partPool(noun), and only under "both" and "threeOrTwo" - the modes where a
    /// participle is a second word rather than the word.
    /// </summary>
    /// <remarks>
    /// Deliberately far below the adjective floor, and deliberately not raised to fit: slugger
    /// ships 40 participles for its poorest noun, and heroku sat exactly on 20 until "either"
    /// stopped holding it to this floor at all. A ratchet, like the warning and mutation ones -
    /// raise it once the shipped themes have been grown, never lower it to make a red load green.
    /// </remarks>
    public const int MinimumParticiplePoolPerNoun = 20;

    /// <summary>How many offenders a remark names before counting the rest, as a refusal does.</summary>
    private const int MaxNamedPerRemark = 3;

    /// <summary>Combinations a single category must reach, so that no branch of the theme is poor on its own.</summary>
    public const int MinimumCombinationsPerCategory = 40_000;

    /// <param name="theme">The theme to check.</param>
    /// <param name="allowSmall">
    /// The run's override: <c>--allow-small-theme</c>. The theme's own <c>allowSmall</c> counts
    /// for as much, so either one waives the size rules.
    /// </param>
    public static IReadOnlyList<DomainError> Validate(Theme theme, bool allowSmall = false)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return Validate(new ThemeResolver(theme), allowSmall);
    }

    /// <summary>
    /// The same rules on a surface already resolved, which is how a run's length budget is
    /// judged: a theme reduced by <c>--max-length</c> is a theme like any other, and it clears
    /// the floors or it does not (DEC0018).
    /// </summary>
    /// <param name="resolver">The surface to check, whole or already narrowed.</param>
    /// <param name="allowSmall">The run's override, as above.</param>
    public static IReadOnlyList<DomainError> Validate(ThemeResolver resolver, bool allowSmall = false)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        Theme theme = resolver.Theme;
        List<DomainError> errors = [];

        errors.AddRange(NoNounAtAll(resolver));
        errors.AddRange(UndeclaredCategories(theme));
        errors.AddRange(ParticiplesAskedForButAbsent(theme));
        errors.AddRange(ExclusionsMatchingNothing(theme));
        errors.AddRange(IncompatibilitiesMatchingNothing(theme));
        errors.AddRange(LongerThanItPromises(theme, resolver));

        if (!allowSmall && !theme.AllowSmall)
        {
            errors.AddRange(SizeFailures(resolver));
        }

        return errors;
    }

    /// <summary>
    /// Never waived by allowSmall: that flag accepts a small theme, not one that cannot draw.
    /// Without this the refusal arrives later, from the generator, as an exception nobody caught.
    /// </summary>
    private static IEnumerable<DomainError> NoNounAtAll(ThemeResolver resolver)
    {
        if (resolver.Nouns.Count > 0)
        {
            yield break;
        }

        // Two ways to have nothing to draw, and an author needs to know which: a file holding no
        // noun, or a budget that left room for none of them.
        yield return resolver.Theme.Nouns.Count == 0
            ? ThemeErrors.NoNounToDrawFrom(resolver.Theme.Name)
            : ThemeErrors.NothingFitsTheLimit(resolver.Theme.Name, resolver.Budget!.MaxLength);
    }

    private static IEnumerable<DomainError> UndeclaredCategories(Theme theme)
    {
        string[] declared = theme.Adjectives.Keys
            .Concat(theme.Participles.Keys)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        HashSet<string> lookup = new(declared, StringComparer.Ordinal);

        return theme.Nouns
            .SelectMany(noun => noun.Categories.Select(category => (noun, category)))
            .Where(pair => !lookup.Contains(pair.category))
            .Select(pair => ThemeErrors.UnknownCategory(pair.noun.Value, pair.category, declared));
    }

    /// <summary>
    /// An exclusion naming a word nowhere in the theme is refused, not ignored. A safety list
    /// that fails open is worse than none: "boaring" would leave the noun reading as protected
    /// while every draw still reaches "boring".
    /// </summary>
    /// <summary>
    /// What a theme may do and probably did not mean to. Nothing here refuses anything: these
    /// are handed to an author at the moment they register a theme, where a second look is
    /// cheap and a catalogue is what they are heading into.
    /// </summary>
    /// <param name="theme">The theme to look over.</param>
    public static IReadOnlyList<string> Remarks(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        List<string> remarks = [];

        // Legal - "charming" and "boring" are adjectives and present participles alike - so the
        // generator emits the word once rather than twice. Worth saying all the same: an author
        // who did not intend it is losing a segment on those draws.
        string[] inBoth = [.. theme.Adjectives.Values.SelectMany(words => words)
            .Intersect(theme.Participles.Values.SelectMany(words => words), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        if (inBoth.Length > 0)
        {
            remarks.Add(
                $"{Name(inBoth)} declared as both an adjective and a participle; "
                + "a draw that lands on the same word twice writes it once.");
        }

        remarks.AddRange(IncompatibilitiesThatNeverFire(theme));

        return remarks;
    }

    /// <summary>
    /// A pair can be written correctly and still never apply: only "both" draws two words, and
    /// only a noun reaching the adjective <i>and</i> the participle can ever put them together.
    /// Neither is a refusal - a theme may well carry pairs for the day it changes mode - but
    /// both are worth a second look at the one moment a second look is cheap.
    /// </summary>
    private static IEnumerable<string> IncompatibilitiesThatNeverFire(Theme theme)
    {
        if (!theme.HasIncompatibilities)
        {
            yield break;
        }

        if (!DrawnMode(theme).PutsAParticipleBesideAnAdjective())
        {
            yield return
                $"the theme draws \"{Spelling.Of(DrawnMode(theme))}\", which puts one word in "
                + "front of the noun, so none of its incompatible pairs can ever apply.";
        }

        string[] dead = [.. DeadPairs(theme).Order(StringComparer.Ordinal)];
        if (dead.Length > 0)
        {
            yield return
                $"{Name(dead)} never drawn together by any noun, so the pair changes nothing; "
                + "deliberate if you are writing ahead, a typo otherwise.";
        }
    }

    /// <summary>Pairs no noun can put side by side, because nothing reaches both of their words.</summary>
    private static IEnumerable<string> DeadPairs(Theme theme)
    {
        ThemeResolver resolver = new(theme);
        (HashSet<string> Adjectives, HashSet<string> Participles)[] reach =
        [
            .. theme.Nouns.Select(noun => (
                new HashSet<string>(resolver.Pool(noun), StringComparer.Ordinal),
                new HashSet<string>(resolver.ParticiplePool(noun), StringComparer.Ordinal)))
        ];

        return from pair in theme.Incompatible
               from participle in pair.Value
               where !reach.Any(noun => noun.Adjectives.Contains(pair.Key) && noun.Participles.Contains(participle))
               select $"{pair.Key} / {participle}";
    }

    /// <summary>Names a few and counts the rest, as a refusal does.</summary>
    private static string Name(string[] words)
    {
        string named = string.Join(", ", words.Take(MaxNamedPerRemark).Select(word => $"\"{word}\""));

        return words.Length <= MaxNamedPerRemark
            ? named
            : $"{named} and {words.Length - MaxNamedPerRemark} more";
    }

    private static IEnumerable<DomainError> ExclusionsMatchingNothing(Theme theme)
    {
        HashSet<string> declared = new(
            theme.Adjectives.Values.Concat(theme.Participles.Values).SelectMany(words => words),
            StringComparer.Ordinal);

        return theme.Nouns
            .SelectMany(noun => noun.Except.Select(word => (noun, word)))
            .Where(pair => !declared.Contains(pair.word))
            .Select(pair => ThemeErrors.ExclusionMatchesNothing(pair.noun.Value, pair.word));
    }

    /// <summary>
    /// What the theme will actually put in front of a noun, which is what the floors are about:
    /// its declared mode, defaulting to "both", and degraded exactly as
    /// <c>SlugGenerator.DrawPrefix</c> degrades it. A theme declaring no participle anywhere
    /// draws adjectives whatever its defaults asked for, and that incoherence is reported once
    /// by <see cref="ParticiplesAskedForButAbsent"/> rather than again by every noun in the file.
    /// </summary>
    /// <param name="theme">The theme whose mode is wanted.</param>
    internal static SegmentMode DrawnMode(Theme theme) => DrawnMode(new ThemeResolver(theme));

    /// <inheritdoc cref="DrawnMode(Theme)"/>
    /// <remarks>
    /// A narrowed surface belongs to a run, and it is the run's mode that decides what is drawn
    /// in front of its nouns - not the mode the theme would have chosen for itself (DEC0018).
    /// </remarks>
    /// <param name="resolver">The surface whose mode is wanted.</param>
    internal static SegmentMode DrawnMode(ThemeResolver resolver)
    {
        SegmentMode asked = resolver.Budget?.SegmentMode
            ?? resolver.Theme.Defaults.SegmentMode
            ?? SegmentMode.Both;

        return resolver.Theme.HasParticiples ? asked : SegmentMode.Adjective;
    }

    /// <summary>
    /// A pair naming a word the theme declares nowhere is refused, not ignored - the reasoning
    /// of <see cref="ExclusionsMatchingNothing"/>, one axis further. Never waived by allowSmall:
    /// this describes an incoherent file, not a small one.
    /// </summary>
    private static IEnumerable<DomainError> IncompatibilitiesMatchingNothing(Theme theme)
    {
        HashSet<string> adjectives = new(theme.Adjectives.Values.SelectMany(words => words), StringComparer.Ordinal);
        HashSet<string> participles = new(theme.Participles.Values.SelectMany(words => words), StringComparer.Ordinal);

        foreach ((string adjective, IReadOnlyList<string> refused) in theme.Incompatible)
        {
            if (!adjectives.Contains(adjective))
            {
                yield return ThemeErrors.IncompatibleAdjectiveNotDeclared(adjective, participles.Contains(adjective));
            }

            foreach (string word in refused.Where(word => !participles.Contains(word)))
            {
                yield return ThemeErrors.IncompatibleParticipleNotDeclared(
                    adjective, word, adjectives.Contains(word));
            }
        }
    }

    /// <summary>
    /// The theme's own promise about the length of its slugs, checked against what it can
    /// actually produce (DEC0018). Never waived by allowSmall: that flag accepts a small theme,
    /// not one whose declaration is untrue - and an untrue one is what lets a slug through that
    /// its destination refuses.
    /// </summary>
    /// <remarks>
    /// Both shapes are checked whatever the theme's own segment mode, because both are what the
    /// keys mean: "twoWords" is a promise about every mode drawing one word in front of the noun,
    /// "threeWords" about "both". A key left out promises nothing and is not checked.
    /// </remarks>
    private static IEnumerable<DomainError> LongerThanItPromises(Theme theme, ThemeResolver resolver)
    {
        if (!theme.MaxLength.Declared)
        {
            yield break;
        }

        GenerationOptions style = GenerationOptions.Default.WithDefaultsOf(theme);

        foreach ((string shape, int wordsBefore, int? promised) in Shapes(theme.MaxLength))
        {
            if (promised is not { } ceiling || Longest(resolver, wordsBefore, style) is not { } longest)
            {
                continue;
            }

            if (longest.Length > ceiling)
            {
                yield return ThemeErrors.LongerThanPromised(shape, longest, ceiling);
            }
        }
    }

    private static IEnumerable<(string Shape, int WordsBefore, int? Promised)> Shapes(MaxLength maxLength)
    {
        yield return ("twoWords", 1, maxLength.TwoWords);
        yield return ("threeWords", 2, maxLength.ThreeWords);
    }

    /// <summary>
    /// The longest slug the theme can produce in one shape, formatted as its own defaults would
    /// format it. Computed per noun rather than over the whole file: the longest word may be out
    /// of reach of the longest noun, and a promise measured on a pair that cannot be drawn is not
    /// a promise about this theme.
    /// </summary>
    /// <param name="resolver">The surface to measure, whole or already narrowed.</param>
    /// <param name="wordsBefore">One under every mode but "both", which draws two.</param>
    /// <param name="style">How the slug will be formatted, which is what decides its length.</param>
    internal static string? Longest(ThemeResolver resolver, int wordsBefore, GenerationOptions style)
    {
        string? longest = null;
        foreach (Noun noun in resolver.Nouns)
        {
            if (LongestFor(resolver, noun, wordsBefore, style) is not { } segments)
            {
                continue;
            }

            string slug = Format(segments, style);
            if (longest is null || slug.Length > longest.Length)
            {
                longest = slug;
            }
        }

        return longest;
    }

    private static string Format(IReadOnlyList<string> segments, GenerationOptions style) =>
        SlugFormatter.Format(segments, Token(style), style);

    private static string? Token(GenerationOptions style) =>
        style.TokenLength > 0 ? new string('0', style.TokenLength) : null;

    /// <summary>
    /// The longest this one noun can come out in that shape, or null where it cannot produce the
    /// shape at all - a noun reaching no participle never draws three words, so it has nothing
    /// to say about a promise made about three.
    /// </summary>
    private static IReadOnlyList<string>? LongestFor(
        ThemeResolver resolver,
        Noun noun,
        int wordsBefore,
        GenerationOptions style)
    {
        IReadOnlyList<string> adjectives = resolver.Pool(noun);
        IReadOnlyList<string> participles = resolver.ParticiplePool(noun);

        if (wordsBefore >= 2)
        {
            return adjectives.Count > 0 && participles.Count > 0
                ? WidestPair(resolver, noun, adjectives, style)
                : null;
        }

        // Every other mode draws one word, and "either" draws it from the two pools as one
        // (DEC0015) - so the widest of both is what the shape can reach.
        return Widest([.. adjectives, .. participles], style) is { } one
            ? [one, noun.Value]
            : [noun.Value];
    }

    /// <summary>
    /// The longest pair this noun can actually draw. The widest adjective and the widest
    /// participle are not a pair: an incompatibility may refuse them to each other (DEC0017),
    /// and a budget may leave the second no room behind the first (DEC0018). So every adjective
    /// is measured against what it really leaves, and the best of those wins.
    /// </summary>
    private static IReadOnlyList<string> WidestPair(
        ThemeResolver resolver,
        Noun noun,
        IReadOnlyList<string> adjectives,
        GenerationOptions style)
    {
        IReadOnlyList<string> longest = [Widest(adjectives, style)!, noun.Value];
        int length = Format(longest, style).Length;

        foreach (string adjective in adjectives)
        {
            if (Widest(resolver.ParticiplePool(noun, adjective), style) is not { } participle)
            {
                continue;
            }

            string[] candidate = [adjective, participle, noun.Value];
            int candidateLength = Format(candidate, style).Length;
            if (candidateLength > length)
            {
                longest = candidate;
                length = candidateLength;
            }
        }

        return longest;
    }

    private static string? Widest(IReadOnlyList<string> words, GenerationOptions style) =>
        words.Count == 0 ? null : words.MaxBy(word => SlugBudget.LengthOf([word], style));

    private static IEnumerable<DomainError> ParticiplesAskedForButAbsent(Theme theme)
    {
        if (theme.HasParticiples)
        {
            yield break;
        }

        if (theme.Defaults.SegmentMode is SegmentMode.Participle or SegmentMode.Either)
        {
            yield return ThemeErrors.ParticiplesRequestedButAbsent(theme.Defaults.SegmentMode.Value);
        }
    }

    /// <summary>
    /// The floor on the words one noun can have in front of it, applied to the pool the mode
    /// actually draws from. "both" and "threeOrTwo" are the modes carrying two floors, because
    /// they are the ones putting a participle beside an adjective - and the participle's is its
    /// own, far lower one, since a participle there is a second word rather than the word.
    /// "threeOrTwo" draws it less often than "both", never less variously: the absence takes a
    /// share of the draws, not a share of the pool (DEC0020).
    /// </summary>
    private static IEnumerable<DomainError> PrefixFailures(SegmentMode drawn, Noun noun, ThemeResolver resolver)
    {
        int adjectives = resolver.Pool(noun).Count;
        int participles = resolver.ParticiplePool(noun).Count;

        switch (drawn)
        {
            case SegmentMode.Participle when participles < MinimumPoolPerNoun:
                yield return ThemeErrors.ParticiplePoolTooSmall(
                    noun.Value, participles, MinimumPoolPerNoun, drawn);

                break;

            case SegmentMode.Either when adjectives + participles < MinimumPoolPerNoun:
                yield return ThemeErrors.CombinedPoolTooSmall(
                    noun.Value, adjectives, participles, MinimumPoolPerNoun);

                break;

            case SegmentMode.Adjective:
            case SegmentMode.Both:
            case SegmentMode.ThreeOrTwo:
                if (adjectives < MinimumPoolPerNoun)
                {
                    yield return ThemeErrors.PoolTooSmall(noun.Value, adjectives, MinimumPoolPerNoun);
                }

                if (!drawn.PutsAParticipleBesideAnAdjective())
                {
                    break;
                }

                if (participles < MinimumParticiplePoolPerNoun)
                {
                    yield return ThemeErrors.ParticiplePoolTooSmall(
                        noun.Value, participles, MinimumParticiplePoolPerNoun, drawn);

                    break;
                }

                // The pool the floor is really about: what is left once the adjective drawn in
                // front has refused what it refuses (DEC0017). Only checked once the unconditional
                // pool clears, so a thin theme is told it is thin before it is told which pair
                // makes it thinner.
                if (Starved(noun, resolver) is { } starved && starved.Left < MinimumParticiplePoolPerNoun)
                {
                    // Two causes, two messages: a pair is dropped or the theme is grown, where a
                    // ceiling is raised or the slug is shortened. Advice for the wrong one sends
                    // an author looking for an incompatibility that is not there.
                    yield return resolver.Budget is { } budget
                        ? ThemeErrors.TheLimitStarvesTheNoun(
                            noun.Value,
                            starved.Adjective,
                            starved.Left,
                            MinimumParticiplePoolPerNoun,
                            budget.MaxLength)
                        : ThemeErrors.IncompatibilityStarvesTheNoun(
                            noun.Value, starved.Adjective, starved.Left, MinimumParticiplePoolPerNoun);
                }

                break;

            default:
                break;
        }
    }

    /// <summary>
    /// The worst an incompatibility does to one noun: the adjective it can draw that leaves it
    /// fewest participles, or null where no adjective it reaches refuses anything at all.
    /// </summary>
    /// <remarks>
    /// Shared with the analysis rather than written twice: the report has to name the same
    /// couple the refusal does, and two implementations of "the worst one" drift.
    /// </remarks>
    /// <param name="noun">The noun to measure.</param>
    /// <param name="resolver">A resolver already warmed on its theme.</param>
    internal static (string Adjective, int Left)? Starved(Noun noun, ThemeResolver resolver)
    {
        if (!resolver.Theme.HasIncompatibilities && resolver.Budget is null)
        {
            return null;
        }

        (string Adjective, int Left)? worst = null;
        foreach (string adjective in resolver.Pool(noun).Where(resolver.NarrowsTheParticiples))
        {
            int left = resolver.ParticiplePool(noun, adjective).Count;
            if (worst is null || left < worst.Value.Left)
            {
                worst = (adjective, left);
            }
        }

        return worst;
    }

    private static IEnumerable<DomainError> SizeFailures(ThemeResolver resolver)
    {
        int distinctNouns = resolver.Nouns
            .Select(noun => noun.Value)
            .Distinct(StringComparer.Ordinal)
            .Count();

        if (distinctNouns < MinimumNouns)
        {
            yield return ThemeErrors.TooFewNouns(distinctNouns, MinimumNouns);
        }

        SegmentMode drawn = DrawnMode(resolver);
        foreach (DomainError failure in resolver.Nouns.SelectMany(noun => PrefixFailures(drawn, noun, resolver)))
        {
            yield return failure;
        }

        ThemeCombinatorics combinatorics = new(resolver);
        string[] categoriesInUse = resolver.Nouns
            .SelectMany(noun => noun.Categories)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        foreach (string category in categoriesInUse)
        {
            long combinations = combinatorics.CombinationsForCategory(category);
            if (combinations < MinimumCombinationsPerCategory)
            {
                yield return ThemeErrors.CategoryTooPoor(category, combinations, MinimumCombinationsPerCategory);
            }
        }
    }
}
