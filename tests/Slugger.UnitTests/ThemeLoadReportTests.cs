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
    /// A remark is not a refusal. Declaring "charming" in both sections is legal and correct -
    /// it is an adjective and a present participle - so the theme loads; the author is simply
    /// told, at the one moment a second look is cheap.
    /// </summary>
    [Fact]
    public void A_word_declared_in_both_sections_is_remarked_on_and_not_refused()
    {
        // Setup
        const string Json = """
            {
              "adjectives": { "common": ["charming", "keen"] },
              "participles": { "common": ["charming", "waning"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        string remark = Assert.Single(ThemeValidator.Remarks(outcome.GetResultOrThrow()));
        Assert.Contains("\"charming\"", remark, StringComparison.Ordinal);
        Assert.DoesNotContain("keen", remark, StringComparison.Ordinal);
    }

    /// <summary>
    /// segmentMode is "both" by default, so a participle sits in the slug as much as an adjective
    /// does. A noun reaching three of them repeats its middle word forever, and no rule saw it:
    /// the per-category combination count sums over nouns, which hides a poverty that is per noun.
    /// </summary>
    [Fact]
    public void A_noun_reaching_too_few_participles_is_refused_and_named()
    {
        // Setup - everything else clears its floor, so only the participles can be at fault.
        string json = ThemeFiles.Valid(participles: 3);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Error refusal = Assert.Single(
            Reasons(outcome),
            reason => reason.Code == ThemeErrors.Codes.ParticiplePoolTooSmall && reason.DiagnosticMessage.Contains("noun0", StringComparison.Ordinal));
        Assert.Contains("3 participles", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// The floor only applies where the theme opted in. A theme with no participles at all is
    /// ordinary - it draws two segments - and must not be refused for lacking what it never
    /// claimed.
    /// </summary>
    [Fact]
    public void A_theme_declaring_no_participle_at_all_is_not_held_to_the_participle_floor()
    {
        // Setup
        const string Json = """
            {
              "adjectives": { "common": ["keen"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    /// DEC0016, and the case that pays for it: under "either" one word is drawn in front of the
    /// noun, from the two pools at once (DEC0015), so 60 and 60 is a pool of 120 and is rich
    /// enough. The rule it replaced held each pool to 100 alone and refused this file twice over.
    /// </summary>
    [Fact]
    public void Either_holds_a_noun_to_its_two_pools_added_rather_than_to_each_of_them()
    {
        // Setup
        string json = ThemeFiles.Valid(adjectives: 60, participles: 60, segmentMode: SegmentMode.Either);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    /// DEC0020 leans on DEC0016 rather than adding to it: "threeOrTwo" puts a participle beside
    /// an adjective exactly as "both" does, so it carries the same two floors. The absence takes
    /// a share of the draws, never a share of the pool - a thin pool is as repetitive here as
    /// there, only reached less often.
    /// </summary>
    [Fact]
    public void Three_or_two_holds_a_noun_to_the_floors_of_both()
    {
        // Setup - three participles, which "both" refuses and every other mode ignores.
        string json = ThemeFiles.Valid(participles: 3, segmentMode: SegmentMode.ThreeOrTwo);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Error refusal = Assert.Single(
            Reasons(outcome),
            reason => reason.Code == ThemeErrors.Codes.ParticiplePoolTooSmall
                && reason.DiagnosticMessage.Contains("noun0", StringComparison.Ordinal));
        Assert.Contains("threeOrTwo", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// The floor did not move, only what it is measured on (DEC0016): 40 and 40 is a pool of 80,
    /// and the refusal names both halves so an author knows which one to grow.
    /// </summary>
    [Fact]
    public void Either_refuses_a_noun_whose_two_pools_are_thin_even_together()
    {
        // Setup
        string json = ThemeFiles.Valid(adjectives: 40, participles: 40, segmentMode: SegmentMode.Either);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Error refusal = Assert.Single(
            Reasons(outcome),
            reason => reason.Code == ThemeErrors.Codes.CombinedPoolTooSmall
                && reason.DiagnosticMessage.Contains("noun0", StringComparison.Ordinal));
        Assert.Contains("80 words", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("40 in \"adjectives\"", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("40 in \"participles\"", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// DEC0016: under "participle" the participle is the word in front of the noun, not a second
    /// one beside an adjective, so it carries the whole floor of 100 rather than the 20 that
    /// "both" asks of it. The same file loads under "both", which is what makes the point.
    /// </summary>
    [Fact]
    public void Participle_holds_the_participle_pool_to_the_whole_floor()
    {
        // Setup - 60 clears the floor "both" applies and not the one "participle" does.
        string refused = ThemeFiles.Valid(participles: 60, segmentMode: SegmentMode.Participle);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(refused, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Error refusal = Assert.Single(
            Reasons(outcome),
            reason => reason.Code == ThemeErrors.Codes.ParticiplePoolTooSmall
                && reason.DiagnosticMessage.Contains("noun0", StringComparison.Ordinal));
        Assert.Contains("at least 100", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.True(
            Themes.LoadFromJsonResult(ThemeFiles.Valid(participles: 60), "theme").IsSuccess,
            "the same pools load under \"both\", where a participle is the second word rather than the word");
    }

    /// <summary>
    /// The floors follow what is drawn, and a theme with nothing in "participles" draws its
    /// adjective alone whatever its mode says (DEC0016) - so it is held to the adjective floor
    /// and to nothing else. Its sibling above proves the same with the size rules waived; this
    /// one proves it with them running, which is where the rule actually lives.
    /// </summary>
    [Fact]
    public void A_theme_declaring_no_participle_is_held_to_the_adjective_floor_and_no_other()
    {
        // Setup - no "participles" section at all, which is the ordinary two-segment theme.
        string json = ThemeFiles.Valid(participles: 0);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    /// DEC0016: "adjective" never draws a participle, so a thin participle section is dead
    /// weight rather than a fault. Refusing it would refuse a theme for words it never uses.
    /// </summary>
    [Fact]
    public void Adjective_asks_nothing_of_the_participles_it_never_draws()
    {
        // Setup - three participles, far under any floor, and never reached.
        string json = ThemeFiles.Valid(participles: 3, segmentMode: SegmentMode.Adjective);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    /// DEC0017, and the reason the floor could not stay where it was: "adj0" refuses five of the
    /// twenty participles every noun reaches, so the unconditional count still reads twenty and
    /// the draw that puts "adj0" in front only ever has fifteen to choose from.
    /// </summary>
    [Fact]
    public void An_incompatibility_taking_a_noun_under_the_participle_floor_is_refused()
    {
        // Setup
        string json = ThemeFiles.Valid(refusedByTheFirstAdjective: 5);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Error refusal = Assert.Single(
            Reasons(outcome),
            reason => reason.Code == ThemeErrors.Codes.IncompatibilityStarvesTheNoun
                && reason.DiagnosticMessage.Contains("noun0", StringComparison.Ordinal));
        Assert.Contains("15 participles", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("\"adj0\"", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// The floor is on what remains, not on how many words a pair names: refusing four of twenty
    /// leaves sixteen, which is still short, and refusing none of them is the ordinary theme.
    /// </summary>
    [Fact]
    public void An_incompatibility_leaving_the_floor_intact_loads()
    {
        // Setup - twenty-five participles, five refused, twenty left: exactly the floor.
        string json = ThemeFiles.Valid(participles: 25, refusedByTheFirstAdjective: 5);

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(json, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    /// The same reasoning as an exclusion matching nothing (DEC0011): a pair that matches nothing
    /// fails open, so the theme reads as protected and is not. The message says the likeliest
    /// cause rather than leaving the author to find it.
    /// </summary>
    [Fact]
    public void An_incompatibility_written_the_wrong_way_round_is_refused_and_told_so()
    {
        // Setup - "waning" is a participle and "keen" an adjective; the pair has them swapped.
        const string Json = """
            {
              "adjectives": { "common": ["keen"] },
              "participles": { "common": ["waning"] },
              "incompatible": { "waning": ["keen"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Error refusal = Assert.Single(
            Reasons(outcome),
            reason => reason.Code == ThemeErrors.Codes.IncompatibleAdjectiveNotDeclared);
        Assert.Contains("the wrong way round", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// The other half of the pair, and the ordinary typo: a refused word that is simply not a
    /// participle anywhere. No hint here, because nothing suggests an inversion.
    /// </summary>
    [Fact]
    public void An_incompatibility_refusing_a_word_the_theme_never_declares_is_refused()
    {
        // Setup - one letter off, which is the likeliest way a pair goes wrong.
        const string Json = """
            {
              "adjectives": { "common": ["keen"] },
              "participles": { "common": ["waning"] },
              "incompatible": { "keen": ["wanning"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Error refusal = Assert.Single(
            Reasons(outcome),
            reason => reason.Code == ThemeErrors.Codes.IncompatibleParticipleNotDeclared);
        Assert.Contains("wanning", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("the wrong way round", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// A pair can be written correctly and never apply - "either" puts one word in front of the
    /// noun, so two of them never meet. A remark rather than a refusal: a theme may carry pairs
    /// for the day it changes mode.
    /// </summary>
    [Fact]
    public void A_pair_in_a_theme_that_never_draws_two_words_is_remarked_on_and_not_refused()
    {
        // Setup
        const string Json = """
            {
              "defaults": { "segmentMode": "either" },
              "adjectives": { "common": ["keen"] },
              "participles": { "common": ["waning"] },
              "incompatible": { "keen": ["waning"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.Contains(
            ThemeValidator.Remarks(outcome.GetResultOrThrow()),
            remark => remark.Contains("can ever apply", StringComparison.Ordinal));
    }

    /// <summary>
    /// The other way a correct pair does nothing: no noun reaches both of its words, so they are
    /// never side by side to be separated. The mirror of a category nobody carries.
    /// </summary>
    [Fact]
    public void A_pair_no_noun_can_draw_together_is_remarked_on()
    {
        // Setup - "moon" reaches "keen" and "waning"; "rushing" belongs to a category it has not.
        const string Json = """
            {
              "adjectives": { "common": ["keen"] },
              "participles": { "common": ["waning"], "water": ["rushing"] },
              "incompatible": { "keen": ["rushing"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.Contains(
            ThemeValidator.Remarks(outcome.GetResultOrThrow()),
            remark => remark.Contains("keen / rushing", StringComparison.Ordinal));
    }

    /// <summary>
    /// The one that makes the feature worth having. An exclusion is a safety list, and a safety
    /// list that fails open is worse than none: "boaring" would leave the noun reading as
    /// protected while every draw still reaches "boring". Refused at load, with both names, so
    /// a typo is a red build rather than a discovery in production.
    /// </summary>
    [Fact]
    public void An_exclusion_matching_no_word_in_the_theme_is_refused_rather_than_ignored()
    {
        // Setup - one letter off, which is the likeliest way an exclusion goes wrong.
        const string Json = """
            {
              "adjectives": { "common": ["boring", "brilliant"] },
              "nouns": [{ "value": "wozniak", "except": ["boaring"] }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Error refusal = Assert.Single(Reasons(outcome), reason => reason.Code == ThemeErrors.Codes.ExclusionMatchesNothing);
        Assert.Contains("wozniak", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("boaring", refusal.DiagnosticMessage, StringComparison.Ordinal);
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
