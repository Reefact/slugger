using FirstClassErrors;
using Slugger.Cli.Rendering;
using Slugger.Domain;
using Slugger.Domain.Validation;

namespace Slugger.Cli.UnitTests;

public sealed class ThemeReportRendererTests
{
    [Fact]
    public void Names_every_reason_when_there_are_few_of_them()
    {
        // Setup
        Outcome<Theme> refused = Refuse(ThemeErrors.TooFewNouns(3, 100), ThemeErrors.PoolTooSmall("willow", 2, 100));

        // Exercise
        string report = string.Join("\n", ThemeReportRenderer.Render(refused));

        // Verify
        Assert.Contains("refused for 2 reasons", report, StringComparison.Ordinal);
        Assert.Contains("3 nouns", report, StringComparison.Ordinal);
        Assert.Contains("\"willow\" reaches 2 adjectives", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// A theme where every noun is short reports one error per noun, and a few hundred of them
    /// would bury the three findings that differ. The count is kept, the listing is not.
    /// </summary>
    [Fact]
    public void Counts_the_rest_once_one_kind_runs_long()
    {
        // Setup
        DomainError[] many = Enumerable.Range(0, 10)
            .Select(index => ThemeErrors.PoolTooSmall($"noun{index}", index, 100))
            .ToArray();

        // Exercise
        string report = string.Join("\n", ThemeReportRenderer.Render(Refuse(many)));

        // Verify
        Assert.Contains($"and {many.Length - ThemeReportRenderer.MaxNamedPerKind} more of the same kind", report, StringComparison.Ordinal);
        Assert.DoesNotContain("noun9", report, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_category_lists_the_ones_the_theme_does_declare()
    {
        // Setup
        Outcome<Theme> refused = Refuse(ThemeErrors.UnknownCategory("willow", "vegetal", ["common", "stadium"]));

        // Exercise
        string report = string.Join("\n", ThemeReportRenderer.Render(refused));

        // Verify
        Assert.Contains("\"vegetal\"", report, StringComparison.Ordinal);
        Assert.Contains("it declares common, stadium", report, StringComparison.Ordinal);
    }

    private static Outcome<Theme> Refuse(params DomainError[] reasons) =>
        Outcome<Theme>.Failure(ThemeErrors.Rejected("broken", reasons));

    [Fact]
    public void A_loaded_theme_renders_as_loaded()
    {
        // Exercise
        IReadOnlyList<string> report = ThemeReportRenderer.Render(Themes.LoadEmbeddedResult("docker"));

        // Verify
        Assert.Equal(["theme \"docker\" loaded."], report);
    }
}
