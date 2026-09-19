using DiagnosticCatalog.Sonar;
using System.Diagnostics.CodeAnalysis;

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
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = "Scaffolding: the body still throws, so it reads no instance state yet. Revisited when the three rules land - if validation is still a pure function then, the type becomes static instead of keeping this suppression.")]
public sealed class ThemeValidator
{
    /// <summary>Distinct nouns a theme needs before it is accepted.</summary>
    public const int MinimumNouns = 100;

    /// <summary>Per noun, on pool(noun). Participles are explicitly out of this floor.</summary>
    public const int MinimumPoolPerNoun = 100;

    /// <summary>Combinations a single category must reach, so that no branch of the theme is poor on its own.</summary>
    public const int MinimumCombinationsPerCategory = 40_000;

    /// <param name="theme">The theme to check.</param>
    /// <param name="allowSmall">
    /// The effective override: the theme's own allowSmall, or --allow-small-theme for this run.
    /// </param>
    public ThemeValidationResult Validate(Theme theme, bool allowSmall = false) => throw new NotImplementedException();
}
