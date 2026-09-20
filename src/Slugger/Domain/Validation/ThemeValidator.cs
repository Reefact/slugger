using FirstClassErrors;
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
    /// Per noun, on partPool(noun), and only under "both" - the one mode where a participle is a
    /// second word rather than the word.
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

        List<DomainError> errors = [];
        ThemeResolver resolver = new(theme);

        errors.AddRange(NoNounAtAll(theme));
        errors.AddRange(UndeclaredCategories(theme));
        errors.AddRange(ParticiplesAskedForButAbsent(theme));
        errors.AddRange(ExclusionsMatchingNothing(theme));
        errors.AddRange(IncompatibilitiesMatchingNothing(theme));

        if (!allowSmall && !theme.AllowSmall)
        {
            errors.AddRange(SizeFailures(theme, resolver));
        }

        return errors;
    }

    /// <summary>
    /// Never waived by allowSmall: that flag accepts a small theme, not one that cannot draw.
    /// Without this the refusal arrives later, from the generator, as an exception nobody caught.
    /// </summary>
    private static IEnumerable<DomainError> NoNounAtAll(Theme theme)
    {
        if (theme.Nouns.Count == 0)
        {
            yield return ThemeErrors.NoNounToDrawFrom(theme.Name);
        }
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

        if (DrawnMode(theme) != SegmentMode.Both)
        {
            yield return
                $"the theme draws \"{DrawnMode(theme).ToString().ToLowerInvariant()}\", which puts one word in "
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
    internal static SegmentMode DrawnMode(Theme theme) =>
        theme.HasParticiples ? theme.Defaults.SegmentMode ?? SegmentMode.Both : SegmentMode.Adjective;

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
    /// actually draws from. "both" is the one mode carrying two floors, because it is the one
    /// mode drawing two words - and the participle's is its own, far lower one, since a
    /// participle there is a second word rather than the word.
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
                if (adjectives < MinimumPoolPerNoun)
                {
                    yield return ThemeErrors.PoolTooSmall(noun.Value, adjectives, MinimumPoolPerNoun);
                }

                if (drawn != SegmentMode.Both)
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
                    yield return ThemeErrors.IncompatibilityStarvesTheNoun(
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
        if (!resolver.Theme.HasIncompatibilities)
        {
            return null;
        }

        (string Adjective, int Left)? worst = null;
        foreach (string adjective in resolver.Pool(noun).Where(resolver.RefusesAnything))
        {
            int left = resolver.ParticiplePool(noun, adjective).Count;
            if (worst is null || left < worst.Value.Left)
            {
                worst = (adjective, left);
            }
        }

        return worst;
    }

    private static IEnumerable<DomainError> SizeFailures(Theme theme, ThemeResolver resolver)
    {
        int distinctNouns = theme.Nouns
            .Select(noun => noun.Value)
            .Distinct(StringComparer.Ordinal)
            .Count();

        if (distinctNouns < MinimumNouns)
        {
            yield return ThemeErrors.TooFewNouns(distinctNouns, MinimumNouns);
        }

        SegmentMode drawn = DrawnMode(theme);
        foreach (DomainError failure in theme.Nouns.SelectMany(noun => PrefixFailures(drawn, noun, resolver)))
        {
            yield return failure;
        }

        ThemeCombinatorics combinatorics = new(resolver);
        string[] categoriesInUse = theme.Nouns
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
