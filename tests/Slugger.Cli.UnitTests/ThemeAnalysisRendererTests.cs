#region Usings declarations

using System.Globalization;

using Slugger.Cli.Rendering;
using Slugger.Domain;
using Slugger.Domain.Analysis;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The report an author reads after <c>--analyze</c>: every sentence of it is written by the
///     renderer, so every sentence a reader could misread is pinned here.
/// </summary>
public sealed class ThemeAnalysisRendererTests {

    #region Static members

    private static string AnyThemeName() {
        return Any.String().WithChars("abcdefghijklmnopqrstuvwxyz").WithLengthBetween(5, 12).Generate();
    }

    /// <summary>A theme of that many nouns, every one of them reaching every word it declares.</summary>
    private static ThemeDocument ThemeWith(int nouns, int adjectives = 1, int participles = 1) {
        // No participle at all is a section left out, not one holding an empty list.
        Dictionary<string, IReadOnlyList<string>> declaredParticiples = [];
        if (participles > 0) {
            declaredParticiples["common"] = Words("part", participles);
        }

        return new ThemeDocument(
            AnyThemeName(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = Words("adj", adjectives) },
            declaredParticiples,
            [.. Enumerable.Range(0, nouns).Select(index => new NounEntry(Numbered("noun", index), []))]);
    }

    private static string[] Words(string prefix, int count) {
        return [.. Enumerable.Range(0, count).Select(index => Numbered(prefix, index))];
    }

    private static string Numbered(string prefix, int index) {
        return string.Create(CultureInfo.InvariantCulture, $"{prefix}{index}");
    }

    private static string Thousands(long value) {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    #endregion

    /// <summary>
    ///     The floor in the table is the one a load applies, read from the validator rather than
    ///     written a second time: a copy is right until the day the rule moves, and then the report
    ///     argues with the verdict above it.
    /// </summary>
    [Fact]
    public void Measures_the_distinct_nouns_against_the_floor_the_validator_applies() {
        // Setup
        int           nouns    = Any.Int32().Between(1, ThemeValidator.MinimumNouns - 1).Generate();
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith(nouns));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            $"| Distinct nouns | {nouns} | {ThemeValidator.MinimumNouns} | **{nouns - ThemeValidator.MinimumNouns}** |",
            report,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     The threshold under which a suffix is worth it is the per-category floor itself, set
    ///     where Docker and Heroku both reached for one (DEC0003) - so it moves with that floor.
    /// </summary>
    [Fact]
    public void Advises_a_suffix_below_the_threshold_the_category_floor_is_set_at() {
        // Setup - one noun drawing one adjective and one participle: a single slug.
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith(1));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            $"Below {Thousands(ThemeValidator.MinimumCombinationsPerCategory)} — the point where Docker and Heroku "
          + "both added a numeric suffix. A `tokenLength` in `defaults` is worth considering.",
            report,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Calls_a_suffix_a_style_choice_above_that_threshold() {
        // Setup - every noun draws every adjective beside every participle, which clears it.
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith(nouns: 100, adjectives: 20, participles: 21));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            $"Above {Thousands(ThemeValidator.MinimumCombinationsPerCategory)}, so a suffix is a style choice "
          + "here rather than a collision defence.",
            report,
            StringComparison.Ordinal);
    }

}
