namespace Slugger.Domain.Validation;

/// <summary>
/// Everything that must hold before a theme may be used, checked on resolved pools rather
/// than on raw list counts:
/// <list type="number">
///   <item>every category a noun references exists as a key in "adjectives";</item>
///   <item>at least <see cref="MinimumNouns"/> distinct nouns;</item>
///   <item>every noun resolves to at least <see cref="MinimumPoolPerNoun"/> adjectives;</item>
///   <item>every category totals at least <see cref="MinimumCombinationsPerCategory"/> combinations.</item>
/// </list>
/// Rules 2 to 4 are waived by the theme's own <c>allowSmall</c> or by <c>--allow-small-theme</c>.
/// Rule 1 is never waived: it is an incoherent file, not a small one.
/// </summary>
public sealed class ThemeValidator
{
    public const int MinimumNouns = 100;

    /// <summary>Per noun, on pool(noun). Participles are explicitly out of this floor.</summary>
    public const int MinimumPoolPerNoun = 100;

    public const int MinimumCombinationsPerCategory = 40_000;

    /// <param name="theme">The theme to check.</param>
    /// <param name="allowSmall">
    /// The effective override: the theme's own allowSmall, or --allow-small-theme for this run.
    /// </param>
    public ThemeValidationResult Validate(Theme theme, bool allowSmall = false) => throw new NotImplementedException();
}
