#region Usings declarations

using Slugger.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     DEC0038: a theme measured from what a consumer holds - its JSON, a file, a stream, a built-in
///     name - whether or not it would load, with the numbers the command's report is written from.
/// </summary>
public sealed class ThemeAnalysisTests {

    #region Static members

    /// <summary>Literal on purpose: two nouns are the case, far under the hundred a theme needs.</summary>
    private const string TwoNouns = """
                                    {
                                      "adjectives": { "common": ["calm", "bold"] },
                                      "nouns": [{ "value": "otter" }, { "value": "heron" }]
                                    }
                                    """;

    private static ThemeMargin MarginFor(ThemeAnalysis analysis, ThemeRule rule) {
        return Assert.Single(analysis.Measurements!.Margins, margin => margin.Rule == rule);
    }

    #endregion

    [Fact]
    public void Measures_a_built_in_theme_as_accepted() {
        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeEmbedded("docker");

        // Verify
        Assert.True(analysis.IsAccepted);
        Assert.Empty(analysis.Refusals);
        ThemeMargin nouns = MarginFor(analysis, ThemeRule.DistinctNouns);
        Assert.Equal(ThemeFloors.MinimumNouns, nouns.Limit);
        Assert.Equal(nouns.WorstCase - nouns.Limit, nouns.Margin);
    }

    [Fact]
    public void Counts_the_combinations_a_generator_draws_in_the_themes_own_style() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");

        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeEmbedded("docker");

        // Verify
        Assert.Equal(SlugGenerator.ForOrThrow(docker).Combinations, analysis.Measurements!.Combinations);
    }

    [Fact]
    public void Measures_a_refused_theme_and_says_by_how_much_it_misses() {
        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeFromJson(TwoNouns, "small");

        // Verify
        Assert.False(analysis.IsAccepted);
        Assert.Contains(analysis.Refusals, reason => reason.Code == SluggerThemeError.Codes.TooFewNouns);
        ThemeMargin nouns = MarginFor(analysis, ThemeRule.DistinctNouns);
        Assert.Equal(2, nouns.WorstCase);
        Assert.Equal(2 - ThemeFloors.MinimumNouns, nouns.Margin);
    }

    [Fact]
    public void Accepts_a_small_theme_with_its_floors_waived_and_says_so() {
        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeFromJson(TwoNouns, "small", allowSmall: true);

        // Verify
        Assert.True(analysis.IsAccepted);
        Assert.True(analysis.FloorsWaived);
        Assert.True(MarginFor(analysis, ThemeRule.DistinctNouns).Margin < 0);
    }

    [Fact]
    public void Leaves_a_document_that_is_not_json_unmeasured() {
        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeFromJson("not json at all", "broken");

        // Verify
        Assert.False(analysis.IsAccepted);
        Assert.Null(analysis.Measurements);
        Assert.Contains(analysis.Refusals, reason => reason.Code == SluggerThemeError.Codes.MalformedJson);
    }

    [Fact]
    public void Reports_a_built_in_name_that_does_not_exist_rather_than_throwing() {
        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeEmbedded(Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Assert.Null(analysis.Measurements);
        Assert.Contains(analysis.Refusals, reason => reason.Code == SluggerThemeError.Codes.NotFound);
    }

    [Fact]
    public void Reports_a_file_that_does_not_exist_rather_than_throwing() {
        // Setup
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeFromFile(path);

        // Verify
        Assert.Null(analysis.Measurements);
        Assert.Contains(analysis.Refusals, reason => reason.Code == SluggerThemeError.Codes.NotFound);
    }

    /// <summary>
    ///     Literal on purpose: cocktails promises a maximum length, so its longest slug is measured against
    ///     a ceiling - and a ceiling's room is counted below it, not above.
    /// </summary>
    [Fact]
    public void Counts_the_room_under_a_length_ceiling_downwards() {
        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeFromFile(Path.Combine(RepositoryDirectory.Themes, "cocktails.json"));

        // Verify
        ThemeMargin characters = MarginFor(analysis, ThemeRule.CharactersInThreeWords);
        Assert.Equal(characters.Limit - characters.WorstCase, characters.Margin);
        Assert.Equal(characters.WorstCase, characters.Subject!.Length);
    }

    /// <summary>
    ///     Literal on purpose: the theme draws "both", three words, but promises five characters for two -
    ///     a load refuses it for "verylong-x", and the margins have to show that very row.
    /// </summary>
    [Fact]
    public void Shows_the_margin_of_every_length_a_load_checks_whatever_the_mode() {
        // Setup
        const string Json = """
                            {
                              "allowSmall": true,
                              "defaults": { "segmentMode": "both" },
                              "maxLength": { "twoWords": 5, "threeWords": 30 },
                              "adjectives": { "common": ["verylong"] },
                              "participles": { "common": ["short"] },
                              "nouns": [{ "value": "x" }]
                            }
                            """;

        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeFromJson(Json, "promising");

        // Verify
        Assert.Contains(analysis.Refusals, reason => reason.Code == SluggerThemeError.Codes.LongerThanPromised);
        ThemeMargin twoWords = MarginFor(analysis, ThemeRule.CharactersInTwoWords);
        Assert.Equal("verylong-x", twoWords.Subject);
        Assert.Equal(5 - "verylong-x".Length, twoWords.Margin);
        Assert.True(MarginFor(analysis, ThemeRule.CharactersInThreeWords).Margin > 0);
    }

    [Fact]
    public void Measures_a_stream_as_its_json_and_leaves_it_open() {
        // Setup
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(TwoNouns));

        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeFromStream(stream, "small");

        // Verify
        Assert.True(stream.CanRead);
        Assert.Equal(SluggerTheme.AnalyzeFromJson(TwoNouns, "small").Refusals.Count, analysis.Refusals.Count);
    }

    [Fact]
    public void Loads_a_theme_from_a_stream_and_leaves_it_open() {
        // Setup
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(TwoNouns));

        // Exercise
        SluggerTheme theme = SluggerTheme.LoadFromStreamOrThrow(stream, "small", allowSmall: true);

        // Verify
        Assert.Equal("small", theme.Name);
        Assert.True(stream.CanRead);
    }

    /// <summary>
    ///     Literal on purpose: a malformed "meta" leaves the theme unmeasured, and the one noun and one
    ///     adjective still miss their floors - the analysis has to say all three, as a load does.
    /// </summary>
    [Fact]
    public void Lists_every_reason_a_load_gives_even_when_it_cannot_measure() {
        // Setup
        const string Json = """{ "adjectives": { "common": ["calm"] }, "nouns": [{ "value": "otter" }], "meta": 123 }""";
        Outcome<SluggerTheme> loaded = SluggerTheme.LoadFromJson(Json, "broken");

        // Exercise
        ThemeAnalysis analysis = SluggerTheme.AnalyzeFromJson(Json, "broken");

        // Verify
        Assert.Null(analysis.Measurements);
        Assert.Equal(
            loaded.Error!.InnerErrors.Select(reason => reason.Code.Dehydrate()),
            analysis.Refusals.Select(reason => reason.Code.Dehydrate()));
    }

}
