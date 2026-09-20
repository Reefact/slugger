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

    /// <summary>
    /// System.Text.Json counts lines from zero and an author counts from one. The report is for
    /// the author, so it says the line their editor shows - and the broken key below is on the
    /// third line of the file, whatever the parser calls it.
    /// </summary>
    [Fact]
    public void Reports_the_line_the_file_broke_on_as_the_author_counts_it()
    {
        // Setup - the colon after "nouns" is missing, on line 3.
        const string Json = "{\n  \"adjectives\": {},\n  \"nouns\" []\n}";

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "broken");

        // Verify
        Error only = Assert.Single(Reasons(outcome));
        Assert.Contains("line 3", only.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// A name that does not exist is only half the answer; the other half is the names that do,
    /// which is what turns a refusal into the next command to type.
    /// </summary>
    [Fact]
    public void A_theme_nobody_carries_is_refused_with_the_ones_that_do_exist()
    {
        // Exercise
        DomainError refusal = ThemeErrors.NotFound("dokcer", ["docker", "heroku", "slugger"]);

        // Verify
        Assert.Contains("docker, heroku, slugger", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>The same refusal with nothing to offer must not trail an empty list.</summary>
    [Fact]
    public void A_theme_nobody_carries_is_refused_plainly_when_there_are_none_at_all()
    {
        // Exercise
        DomainError refusal = ThemeErrors.NotFound("docker", []);

        // Verify
        Assert.Contains("no theme is available at all", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("Available:", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void A_noun_reaching_for_a_category_that_does_not_exist_is_told_which_ones_do()
    {
        // Setup - the noun asks for "sea"; the theme declares "sky" and "land".
        const string Json = """
                            {
                              "adjectives": { "sky": ["keen"], "land": ["broad"] },
                              "nouns": [{ "value": "moon", "categories": ["sea"] }]
                            }
                            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        // The list is sorted rather than given in the order the file declares them, so the same
        // theme always reads the same way whoever wrote it.
        Error refusal = Assert.Single(Reasons(outcome), reason => reason.Code == ThemeErrors.Codes.UnknownCategory);
        Assert.Contains("land, sky", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// The setting is what the author has to change, so the refusal names it rather than
    /// describing the shortage in the abstract.
    /// </summary>
    [Fact]
    public void A_theme_asking_for_participles_it_lacks_names_the_setting_that_asked()
    {
        // Setup - "either" draws a participle half the time, and this theme declares none.
        const string Json = """
                            {
                              "adjectives": { "common": ["keen"] },
                              "nouns": [{ "value": "moon" }],
                              "defaults": { "segmentMode": "either" }
                            }
                            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Error refusal = Assert.Single(Reasons(outcome));
        Assert.Contains("defaults.segmentMode", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// The CLI prints diagnostic messages; a library consumer shows the public one. Every
    /// refusal the engine can raise therefore has to carry one worth showing. Compared against
    /// the library's sentinel rather than against emptiness: FirstClassErrors substitutes
    /// <see cref="Error.MissingShortMessage"/> for a missing one, so a refusal that forgot its
    /// message surfaces that sentinel in a consumer's user interface rather than a blank.
    /// </summary>
    [Fact]
    public void Every_refusal_carries_a_message_a_consumer_could_show()
    {
        // Setup - one of every refusal, built where they are built rather than provoked one by one.
        DomainError[] refusals =
        [
            ThemeErrors.NotFound("docker", ["heroku"]),
            ThemeErrors.AlreadyRegistered("docker"),
            ThemeErrors.NotAFile("docker"),
            ThemeErrors.MalformedJson("unexpected token", 2),
            ThemeErrors.MalformedSection("nouns", "an array"),
            ThemeErrors.MalformedNoun(0, "not an object"),
            ThemeErrors.UnknownCategory("moon", "sea", ["sky"]),
            ThemeErrors.NoNounToDrawFrom("docker"),
            ThemeErrors.TooFewNouns(3, 100),
            ThemeErrors.PoolTooSmall("moon", 4, 100),
            ThemeErrors.CategoryTooPoor("sea", 12, 40_000),
            ThemeErrors.ParticiplesRequestedButAbsent(SegmentMode.Either),
        ];

        // Verify
        Assert.All(refusals, refusal => Assert.NotEqual(Error.MissingShortMessage, refusal.ShortMessage));

        DomainError rejected = ThemeErrors.Rejected("docker", refusals);
        Assert.NotEqual(Error.MissingShortMessage, rejected.ShortMessage);
        Assert.False(string.IsNullOrWhiteSpace(rejected.DetailedMessage));
    }

    private static IReadOnlyList<Error> Reasons(Outcome<Theme> outcome) => outcome.Error?.InnerErrors ?? [];
}
