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
    private static ThemeDocument ThemeWith(int nouns, int adjectives = 1, int participles = 1, SegmentMode? segmentMode = null) {
        // No participle at all is a section left out, not one holding an empty list.
        Dictionary<string, IReadOnlyList<string>> declaredParticiples = [];
        if (participles > 0) {
            declaredParticiples["common"] = Words("part", participles);
        }

        return new ThemeDocument(
            AnyThemeName(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = Words("adj", adjectives) },
            declaredParticiples,
            [.. Enumerable.Range(0, nouns).Select(index => new NounEntry(Numbered("noun", index), []))],
            new ThemeDefaults { SegmentMode = segmentMode });
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

    /// <summary>
    ///     A theme where every adjective reaches every noun has no spread, and naming its rarest and
    ///     its commonest adjective named one word twice: "from `affable` at 236 to `affable` at 236 -
    ///     a spread of 1×".
    /// </summary>
    [Fact]
    public void Says_every_adjective_reaches_every_noun_rather_than_naming_one_word_twice() {
        // Setup - several nouns, so the sentence is a plural one.
        int           nouns    = Any.Int32().Between(2, 50).Generate();
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith(nouns, adjectives: Any.Int32().Between(1, 20).Generate()));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains($"Every adjective reaches all {nouns} nouns.", report, StringComparison.Ordinal);
        Assert.DoesNotContain("a spread of", report, StringComparison.Ordinal);
    }

    /// <summary>The same evenness without a common category: no adjective is rarer than another, all the same.</summary>
    [Fact]
    public void Says_every_adjective_reaches_as_many_nouns_when_none_reaches_them_all() {
        // Setup - each adjective sits in a category one noun of the two carries.
        ThemeDocument theme = new(
            AnyThemeName(),
            new Dictionary<string, IReadOnlyList<string>> { ["north"] = ["boreal"], ["south"] = ["austral"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new NounEntry("pole", ["north"]), new NounEntry("cape", ["south"])]);

        // Exercise
        string report = ThemeAnalysisRenderer.Render(ThemeAnalyzer.Analyze(theme));

        // Verify
        Assert.Contains("Every adjective reaches 1 of the 2 nouns, no more and no fewer.", report, StringComparison.Ordinal);
    }

    /// <summary>
    ///     A spread is measured, and what it means is said plainly: an adjective in a category few
    ///     nouns carry is rare on purpose, which the sentence used to say without naming a category.
    /// </summary>
    [Fact]
    public void Measures_the_spread_and_says_a_wide_one_is_not_a_fault() {
        // Setup - "keen" reaches both nouns, "rushing" only the one carrying "water".
        ThemeDocument theme = new(
            AnyThemeName(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"], ["water"] = ["rushing"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new NounEntry("moon", []), new NounEntry("river", ["water"])]);

        // Exercise
        string report = ThemeAnalysisRenderer.Render(ThemeAnalyzer.Analyze(theme));

        // Verify
        Assert.Contains(
            "How many nouns can reach one adjective, from `rushing` at 1 to `keen` at 2 — a spread of 2×.",
            report,
            StringComparison.Ordinal);
        Assert.Contains(
            "A wide spread is not a fault: an adjective declared in a category that few nouns carry is drawn beside "
          + "those nouns only, which is usually why it was put there. This measures how uneven the reach is; it "
          + "does not ask for it to be even.",
            report,
            StringComparison.Ordinal);
        Assert.DoesNotContain("decorative", report, StringComparison.Ordinal);
    }

    /// <summary>
    ///     A theme declaring no participle has none to put in front of a noun, so the total does not
    ///     claim one - and since every mode draws the same shape from it, there is no second figure
    ///     "left alone" to set beside the first.
    /// </summary>
    [Fact]
    public void Counts_slugs_with_an_adjective_in_front_for_a_theme_declaring_no_participle() {
        // Setup
        int           nouns      = Any.Int32().Between(1, 50).Generate();
        int           adjectives = Any.Int32().Between(1, 20).Generate();
        ThemeAnalysis analysis   = ThemeAnalyzer.Analyze(ThemeWith(nouns, adjectives, participles: 0));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            $"{Thousands(nouns * adjectives)} distinct slugs with an adjective in front. The theme declares no "
          + "participle, so that is every slug it can produce, whatever `--segment` asks for.",
            report,
            StringComparison.Ordinal);
        Assert.DoesNotContain("a participle in front", report, StringComparison.Ordinal);
        Assert.DoesNotContain("Left alone it draws", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Counts_slugs_with_an_adjective_and_a_participle_in_front_for_a_theme_declaring_both() {
        // Setup
        int           nouns       = Any.Int32().Between(1, 50).Generate();
        int           participles = Any.Int32().Between(1, 20).Generate();
        ThemeAnalysis analysis    = ThemeAnalyzer.Analyze(ThemeWith(nouns, participles: participles));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            $"{Thousands(nouns * participles)} distinct slugs with an adjective and a participle in front, which is "
          + "what `--segment both` reaches.",
            report,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     The segment count assumes an adjective and a participle in front of the noun. A theme left
    ///     to a mode drawing one word there is told so in words - "That is the upper bound over every
    ///     mode; this theme draws fewer words than it left alone" was read and not understood.
    /// </summary>
    [Theory]
    [InlineData(SegmentMode.Adjective, "adjective")]
    [InlineData(SegmentMode.Participle, "participle")]
    [InlineData(SegmentMode.Either, "either")]
    public void Says_what_the_segment_count_assumes_for_a_theme_drawing_one_word_in_front(SegmentMode mode, string spelled) {
        // Setup
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith(Any.Int32().Between(1, 50).Generate(), segmentMode: mode));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            "That count puts an adjective and a participle in front of the noun, as `--segment both` does. Left to "
          + $"its own `segmentMode: {spelled}`, this theme puts one word there, so its slugs carry fewer segments "
          + "than that.",
            report,
            StringComparison.Ordinal);
        Assert.DoesNotContain("upper bound", report, StringComparison.Ordinal);
    }

    /// <summary>With no participle declared, the count never assumed one, and nothing is shorter than it.</summary>
    [Fact]
    public void Says_the_segment_count_is_the_shape_every_mode_draws_for_a_theme_declaring_no_participle() {
        // Setup
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith(Any.Int32().Between(1, 50).Generate(), participles: 0));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            "With no participle declared, that is the shape every mode draws: an adjective, then the noun.",
            report,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Points_a_theme_drawing_two_words_in_front_to_the_mode_drawing_one() {
        // Setup
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith(Any.Int32().Between(1, 50).Generate()));

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            "`--segment either` draws one word before the noun instead of two, if that is long for where the slug goes.",
            report,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     A file read to its last line was reported as one that "could not be read", which sends its
    ///     author looking for a fault that is not there. What stands between them and the margins is
    ///     the errors listed, and the report says so.
    /// </summary>
    [Fact]
    public void Says_the_file_was_read_when_its_errors_leave_nothing_to_measure() {
        // Setup
        ThemeAnalysis analysis = ThemeAnalyzer.Unmeasured(AnyThemeName(), [ThemeErrors.MalformedSection("allowSmall", "true or false")]);

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains(
            "The file was read, but these errors leave nothing that can be measured. Fix them and run `--analyze` "
          + "again to see the margins.",
            report,
            StringComparison.Ordinal);
        Assert.DoesNotContain("could not be read", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Says_a_file_that_is_not_json_could_not_be_read() {
        // Setup
        ThemeAnalysis analysis = ThemeAnalyzer.Unmeasured(AnyThemeName(), [ThemeErrors.MalformedJson("the file ends before the JSON is complete.", 1, 1)]);

        // Exercise
        string report = ThemeAnalysisRenderer.Render(analysis);

        // Verify
        Assert.Contains("The document could not be read, so there is nothing to measure.", report, StringComparison.Ordinal);
        Assert.DoesNotContain("see the margins", report, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The terminal is all some will read, so it says too that the report holds no numbers this
    ///     time, and what to do to get them.
    /// </summary>
    [Fact]
    public void Tells_the_terminal_what_to_fix_to_see_the_margins_when_nothing_was_measured() {
        // Setup
        ThemeAnalysis analysis = ThemeAnalyzer.Unmeasured(AnyThemeName(), [ThemeErrors.MalformedSection("allowSmall", "true or false")]);

        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(analysis));

        // Verify
        Assert.Contains(
            "the file was read, but these errors leave nothing that can be measured: fix them and run --analyze "
          + "again to see the margins.",
            console.Output);
    }

    [Fact]
    public void Tells_the_terminal_a_file_that_is_not_json_could_not_be_read() {
        // Setup
        ThemeAnalysis analysis = ThemeAnalyzer.Unmeasured(AnyThemeName(), [ThemeErrors.MalformedJson("the file ends before the JSON is complete.", 1, 1)]);

        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(analysis));

        // Verify
        Assert.Contains("the file could not be read, so nothing was measured.", console.Output);
    }

    /// <summary>A theme measured and refused has its margins in the report, and the terminal adds nothing about them.</summary>
    [Fact]
    public void Tells_the_terminal_only_the_reasons_when_the_theme_was_measured() {
        // Setup
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith(Any.Int32().Between(1, 50).Generate()));

        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(analysis));

        // Verify
        Assert.DoesNotContain(console.Output, line => line.Contains("measured", StringComparison.Ordinal));
    }

}
