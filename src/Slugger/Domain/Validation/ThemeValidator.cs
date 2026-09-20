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

    /// <summary>Per noun, on pool(noun). Participles are explicitly out of this floor.</summary>
    public const int MinimumPoolPerNoun = 100;

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
