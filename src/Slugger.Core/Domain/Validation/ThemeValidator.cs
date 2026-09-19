using FirstClassErrors;
using Slugger.Domain.Resolution;

namespace Slugger.Domain.Validation;

/// <summary>
/// Everything that must hold before a theme may be used, checked on resolved pools rather
/// than on raw list counts:
/// <list type="number">
///   <item>every category a noun references is declared, in "adjectives" or in "participles";</item>
///   <item>at least <see cref="MinimumNouns"/> distinct nouns;</item>
///   <item>every noun resolves to at least <see cref="MinimumPoolPerNoun"/> adjectives;</item>
///   <item>every category totals at least <see cref="MinimumCombinationsPerCategory"/> combinations.</item>
/// </list>
/// Rules 2 to 4 are waived by the theme's own <c>allowSmall</c> or by <c>--allow-small-theme</c>.
/// Rule 1 is never waived: it is an incoherent file, not a small one.
/// </summary>
/// <remarks>
/// <para>
/// <b>It never stops at the first failure.</b> Every rule runs over every noun and every
/// category, and the result carries all of them, so one run tells a theme author everything
/// their file needs rather than one thing per run.
/// </para>
/// <para>
/// Rule 1 accepts a category declared in "participles" alone. The spec asks for it to exist in
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

        errors.AddRange(UndeclaredCategories(theme));
        errors.AddRange(ParticiplesAskedForButAbsent(theme));

        if (!allowSmall && !theme.AllowSmall)
        {
            errors.AddRange(SizeFailures(theme, resolver));
        }

        return errors;
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
