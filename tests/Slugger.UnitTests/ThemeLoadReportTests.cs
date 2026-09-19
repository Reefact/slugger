using FirstClassErrors;
using Slugger.Domain;
using Slugger.Domain.Validation;

namespace Slugger.UnitTests;

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
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
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
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "broken");

        // Verify
        Error[] sections = Reasons(outcome)
            .Where(reason => reason.Code == ThemeErrors.Codes.MalformedSection)
            .ToArray();
        Assert.Equal(3, sections.Length);
        Assert.Contains(sections, reason => reason.DiagnosticMessage.Contains("defaults.sep", StringComparison.Ordinal));
        Assert.Contains(sections, reason => reason.DiagnosticMessage.Contains("defaults.casing", StringComparison.Ordinal));
        Assert.Contains(sections, reason => reason.DiagnosticMessage.Contains("defaults.tokenLength", StringComparison.Ordinal));
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
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "broken");

        // Verify
        ErrorCode[] reported = Reasons(outcome).Select(reason => reason.Code).Distinct().ToArray();
        Assert.Contains(ThemeErrors.Codes.MalformedSection, reported);
        Assert.Contains(ThemeErrors.Codes.UnknownCategory, reported);
        Assert.Contains(ThemeErrors.Codes.TooFewNouns, reported);
        Assert.Contains(ThemeErrors.Codes.PoolTooSmall, reported);
    }

    [Fact]
    public void Malformed_json_is_terminal_because_nothing_can_be_read_from_it()
    {
        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult("{ \"adjectives\": ", "broken");

        // Verify - one reason, and no rule failure invented on top of a document that never parsed.
        Error only = Assert.Single(Reasons(outcome));
        Assert.Equal(ThemeErrors.Codes.MalformedJson, only.Code);
    }

    /// <summary>
    /// A malformed "nouns" already says everything; adding "0 nouns, at least 100 required" on
    /// top of it would be noise rather than a second finding.
    /// </summary>
    [Fact]
    public void A_section_the_rules_read_being_malformed_does_not_also_fail_the_rules()
    {
        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult("""{ "adjectives": {}, "nouns": "moon" }""", "broken");

        // Verify
        Error only = Assert.Single(Reasons(outcome));
        Assert.Equal(ThemeErrors.Codes.MalformedSection, only.Code);
    }

    /// <summary>
    /// What lets the shipped themes drop 339 lines of "categories": [] - the key being absent
    /// has to keep meaning exactly what an empty array meant, or the compaction silently
    /// changes every one of those nouns.
    /// </summary>
    [Fact]
    public void An_absent_categories_key_means_the_same_as_an_empty_one()
    {
        // Setup
        const string Written = """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon", "categories": [] }] }""";
        const string Omitted = """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }] }""";

        // Exercise
        Theme written = Themes.LoadFromJsonResult(Written, "written", allowSmall: true).GetResultOrThrow();
        Theme omitted = Themes.LoadFromJsonResult(Omitted, "omitted", allowSmall: true).GetResultOrThrow();

        // Verify
        Assert.Equal(written.Nouns[0].Categories, omitted.Nouns[0].Categories);
        Assert.Empty(omitted.Nouns[0].Categories);
    }

    [Fact]
    public void A_refused_load_names_the_theme_it_is_about()
    {
        // Setup
        string name = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult("not json at all", name);

        // Verify
        Assert.True(outcome.IsFailure);
        Assert.Equal(ThemeErrors.Codes.Rejected, outcome.Error!.Code);
        Assert.Contains(name, outcome.Error.DiagnosticMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void The_throwing_loader_carries_the_whole_report_not_only_the_first_reason()
    {
        // Exercise
        DomainException rejected = Assert.Throws<DomainException>(
            () => Themes.LoadFromJson("""{ "adjectives": {}, "nouns": [{ "value": "moon" }] }""", "broken"));

        // Verify
        Assert.True(rejected.Error.InnerErrors.Count > 1, $"only {rejected.Error.InnerErrors.Count} reason(s) carried");
    }

    /// <summary>
    /// allowSmall lets an author accept a small theme, not one that cannot draw at all. Without
    /// this rule the refusal arrived from the generator instead, as an ArgumentException nobody
    /// caught - a stack trace on the terminal for a perfectly ordinary theme file.
    /// </summary>
    [Fact]
    public void A_theme_with_no_noun_is_refused_even_under_allow_small()
    {
        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(
            """{ "adjectives": { "common": ["keen"] }, "nouns": [], "allowSmall": true }""",
            "empty",
            allowSmall: true);

        // Verify
        Error only = Assert.Single(Reasons(outcome));
        Assert.Equal(ThemeErrors.Codes.NoNounToDrawFrom, only.Code);
    }

    [Fact]
    public void A_small_theme_is_still_accepted_under_allow_small()
    {
        // Setup - one noun and one adjective, far below every floor but able to draw.
        const string Json = """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }] }""";

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "tiny", allowSmall: true);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    private static IReadOnlyList<Error> Reasons(Outcome<Theme> outcome) => outcome.Error?.InnerErrors ?? [];
}
