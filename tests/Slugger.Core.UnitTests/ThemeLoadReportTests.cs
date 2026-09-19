using Slugger.Domain.Validation;

namespace Slugger.Core.UnitTests;

/// <summary>
/// The property the whole loading pipeline exists for: one run reports everything wrong with a
/// theme file, so fixing it is one pass rather than one run per problem.
/// </summary>
public sealed class ThemeLoadReportTests
{
    [Fact]
    public void A_valid_document_loads()
    {
        // Setup
        string json = ThemeFiles.Valid();

        // Exercise
        ThemeLoadResult result = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Assert.True(result.IsLoaded, string.Join(" | ", result.Errors.Select(error => error.Code)));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Every_malformed_section_is_reported_not_just_the_first()
    {
        // Setup - three independent shape problems in one document.
        const string Json = """
                            {
                              "adjectives": { "common": ["gorgeous"] },
                              "nouns": [{ "value": "moon" }],
                              "defaults": { "sep": "--", "casing": "SCREAMING", "tokenLength": "four" }
                            }
                            """;

        // Exercise
        ThemeLoadResult result = Themes.LoadFromJsonResult(Json, "broken");

        // Verify
        string[] sections = result.Errors
            .OfType<ThemeValidationError.MalformedSection>()
            .Select(error => error.Section)
            .ToArray();
        Assert.Equal(["defaults.sep", "defaults.casing", "defaults.tokenLength"], sections);
    }

    /// <summary>
    /// The same "stops at the first error" complaint, one stage up: fixing the shape only to be
    /// told about the rules on the next run would be the same waste.
    /// </summary>
    [Fact]
    public void Shape_problems_and_rule_failures_come_back_in_the_same_run()
    {
        // Setup - one malformed default, and a theme far below every size floor.
        const string Json = """
                            {
                              "adjectives": { "common": ["gorgeous", "keen"] },
                              "nouns": [{ "value": "willow", "categories": ["vegetal"] }],
                              "defaults": { "casing": "SCREAMING" }
                            }
                            """;

        // Exercise
        ThemeLoadResult result = Themes.LoadFromJsonResult(Json, "broken");

        // Verify
        ThemeValidationErrorCode[] reported = result.Errors.Select(error => error.Code).Distinct().ToArray();
        Assert.Contains(ThemeValidationErrorCode.MalformedSection, reported);
        Assert.Contains(ThemeValidationErrorCode.UnknownCategory, reported);
        Assert.Contains(ThemeValidationErrorCode.TooFewNouns, reported);
        Assert.Contains(ThemeValidationErrorCode.PoolTooSmall, reported);
    }

    [Fact]
    public void Malformed_json_is_terminal_because_nothing_can_be_read_from_it()
    {
        // Exercise
        ThemeLoadResult result = Themes.LoadFromJsonResult("{ \"adjectives\": ", "broken");

        // Verify - one error, and no rule failure invented on top of a document that never parsed.
        ThemeValidationError only = Assert.Single(result.Errors);
        Assert.Equal(ThemeValidationErrorCode.MalformedJson, only.Code);
    }

    /// <summary>
    /// A malformed "nouns" already says everything; adding "0 nouns, at least 100 required" on
    /// top of it would be noise rather than a second finding.
    /// </summary>
    [Fact]
    public void A_section_the_rules_read_being_malformed_does_not_also_fail_the_rules()
    {
        // Exercise
        ThemeLoadResult result = Themes.LoadFromJsonResult("""{ "adjectives": {}, "nouns": "moon" }""", "broken");

        // Verify
        ThemeValidationError only = Assert.Single(result.Errors);
        Assert.Equal(ThemeValidationErrorCode.MalformedSection, only.Code);
    }

    [Fact]
    public void A_refused_load_names_the_theme_and_carries_no_theme()
    {
        // Setup
        string name = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();

        // Exercise
        ThemeLoadResult result = Themes.LoadFromJsonResult("not json at all", name);

        // Verify
        Assert.False(result.IsLoaded);
        Assert.Null(result.Theme);
        Assert.Equal(name, result.ThemeName);
    }

    [Fact]
    public void The_throwing_loader_carries_the_whole_report_not_only_the_first_reason()
    {
        // Exercise
        ThemeRejectedException rejected = Assert.Throws<ThemeRejectedException>(
            () => Themes.LoadFromJson("""{ "adjectives": {}, "nouns": [{ "value": "moon" }] }""", "broken"));

        // Verify
        Assert.True(rejected.Errors.Count > 1, $"only {rejected.Errors.Count} reason(s) carried");
    }
}
