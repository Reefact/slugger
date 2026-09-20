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
///   <item>every noun resolves to at least <see cref="MinimumPoolPerNoun"/> adjectives;</item>
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

    /// <summary>Per noun, on pool(noun).</summary>
    public const int MinimumPoolPerNoun = 100;

    /// <summary>
    /// Per noun, on partPool(noun), and only in a theme that declares participles at all.
    /// </summary>
    /// <remarks>
    /// Deliberately far below the adjective floor, and deliberately not raised to fit: heroku
    /// ships 20 participles in "common" and 103 of its 216 nouns reach nothing else, so this is
    /// exactly its current minimum and leaves it no headroom. A ratchet, like the warning and
    /// mutation ones - raise it once the shipped themes have been grown, never lower it to make
    /// a red load green.
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

        return remarks;
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

        foreach (Noun noun in theme.Nouns)
        {
            int poolSize = resolver.Pool(noun).Count;
            if (poolSize < MinimumPoolPerNoun)
            {
                yield return ThemeErrors.PoolTooSmall(noun.Value, poolSize, MinimumPoolPerNoun);
            }

            // segmentMode is "both" by default, so a participle is in the slug as much as an
            // adjective is - and a noun reaching three of them repeats its middle word forever.
            // Only where the theme declares participles: having none stays valid.
            if (!theme.HasParticiples)
            {
                continue;
            }

            int participlePoolSize = resolver.ParticiplePool(noun).Count;
            if (participlePoolSize < MinimumParticiplePoolPerNoun)
            {
                yield return ThemeErrors.ParticiplePoolTooSmall(
                    noun.Value, participlePoolSize, MinimumParticiplePoolPerNoun);
            }
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
