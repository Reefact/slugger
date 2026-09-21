using Slugger.Domain;
using Slugger.Infrastructure.Serialization;

namespace Slugger.UnitTests;

/// <summary>
/// The serializer on its own, rather than only through the loader: what it does to values on
/// the way in is invisible from the outside and easy to lose in a refactor.
/// </summary>
public sealed class JsonThemeSerializerTests
{
    [Fact]
    public void Normalises_every_value_it_reads()
    {
        // Setup - a theme written by a human, spacing and casing included.
        const string Json = """
                            {
                              "adjectives": { "common": ["  GORGEOUS  "] },
                              "nouns": [{ "value": " John     Doe " }]
                            }
                            """;

        // Exercise
        ThemeParseResult parsed = new JsonThemeSerializer().Deserialize("theme", Json);

        // Verify - trimmed, collapsed and lowercased; the separator is still the formatter's job.
        Theme theme = parsed.Theme!;
        Assert.Equal(["gorgeous"], theme.Adjectives["common"]);
        Assert.Equal("john doe", theme.Nouns[0].Value);
    }

    /// <summary>
    /// The pool is handed in rather than made per file, because a per-file one would only
    /// deduplicate within that file - and the words that repeat are the ones across themes.
    /// </summary>
    [Fact]
    public void Interns_through_the_pool_it_was_given()
    {
        // Setup
        StringInternPool pool = new();
        JsonThemeSerializer serializer = new(pool);
        const string Json = """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }] }""";

        // Exercise - the same word arriving from two different files.
        Theme first = serializer.Deserialize("one", Json).Theme!;
        Theme second = serializer.Deserialize("two", Json).Theme!;

        // Verify
        Assert.Same(first.Adjectives["common"][0], second.Adjectives["common"][0]);
        Assert.Same(first.Nouns[0].Value, second.Nouns[0].Value);
    }

    [Fact]
    public void Says_whether_the_rules_are_worth_running_on_what_it_read()
    {
        // Exercise - "nouns" is not an array, so counting them would say nothing the shape error does not.
        ThemeParseResult parsed = new JsonThemeSerializer().Deserialize("theme", """{ "adjectives": {}, "nouns": "moon" }""");

        // Verify
        Assert.False(parsed.RulesCanRun);
        Assert.NotEmpty(parsed.ShapeErrors);
    }

    [Fact]
    public void Hands_back_the_theme_it_could_build_even_when_the_shape_complained()
    {
        // Exercise - a malformed default, with everything the rules read intact.
        ThemeParseResult parsed = new JsonThemeSerializer().Deserialize(
            "theme",
            """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }], "defaults": { "casing": "SHOUT" } }""");

        // Verify - both, which is what lets one run report the shape and the rules together.
        Assert.NotNull(parsed.Theme);
        Assert.True(parsed.RulesCanRun);
        Assert.NotEmpty(parsed.ShapeErrors);
    }

    /// <summary>
    /// DEC0006 at the shape level: a file is never refused one reason
    /// at a time. Four things are wrong here and the author is told all four, each naming the
    /// section it belongs to - which is the only thing that makes the report actionable.
    /// </summary>
    [Fact]
    public void Every_malformed_section_is_reported_in_the_same_run()
    {
        // Setup - adjectives, participles and defaults have the wrong shape, and nouns is missing.
        const string Json = """{ "adjectives": [], "participles": 3, "defaults": "snake" }""";

        // Exercise
        ThemeParseResult parsed = Parse(Json);

        // Verify
        Assert.Equal(
            [
                "\"adjectives\" must be an object of category to words.",
                "\"participles\" must be an object of category to words.",
                "\"nouns\" must be an array of { value, categories }.",
                "\"defaults\" must be an object.",
            ],
            Messages(parsed));
    }

    [Fact]
    public void A_document_that_is_not_an_object_is_refused_as_a_whole()
    {
        // Exercise - a theme file holding an array, which nothing below the top level can explain.
        ThemeParseResult parsed = Parse("""["keen", "moon"]""");

        // Verify - one reason, and no attempt to run rules over a document that has no sections.
        Assert.Equal("\"(document)\" must be an object.", Assert.Single(Messages(parsed)));
        Assert.False(parsed.RulesCanRun);
    }

    /// <summary>
    /// Malformed JSON is the one terminal case: nothing can be read from a document that did not
    /// parse, so the rules must not be handed an empty theme to judge.
    /// </summary>
    [Fact]
    public void Json_that_does_not_parse_leaves_the_rules_unrun()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": """);

        // Verify
        Assert.False(parsed.RulesCanRun);
        Assert.Null(parsed.Theme);
        Assert.NotEmpty(parsed.ShapeErrors);
    }

    [Fact]
    public void A_word_list_that_is_not_an_array_names_the_category_it_belongs_to()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": { "common": "keen" }, "nouns": [] }""");

        // Verify - the category, not just the section: a theme has many, and one of them is wrong.
        Assert.Equal("\"adjectives.common\" must be an array of strings.", Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void A_word_list_holding_something_other_than_a_string_names_the_category()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": { "sea": ["keen", 7] }, "nouns": [] }""");

        // Verify
        Assert.Equal("\"adjectives.sea\" must be an array of strings.", Assert.Single(Messages(parsed)));
    }

    /// <summary>A theme without participles is ordinary; one whose participles are junk is not.</summary>
    [Fact]
    public void Participles_may_be_absent_but_not_malformed()
    {
        // Exercise
        ThemeParseResult absent = Parse("""{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }] }""");
        ThemeParseResult malformed = Parse("""{ "adjectives": { "common": ["keen"] }, "participles": [], "nouns": [{ "value": "moon" }] }""");

        // Verify
        Assert.Empty(absent.ShapeErrors);
        Assert.Equal("\"participles\" must be an object of category to words.", Assert.Single(Messages(malformed)));
    }

    /// <summary>
    /// A noun has no name to be called by until it has been read, so the report calls it by its
    /// position - and the position has to be the one the author will count to in their file.
    /// </summary>
    [Fact]
    public void A_noun_that_is_not_an_object_is_named_by_its_position()
    {
        // Setup - two good nouns, then a bare string where an object belongs.
        const string Json = """{ "adjectives": {}, "nouns": [{ "value": "moon" }, { "value": "sun" }, "star"] }""";

        // Exercise
        ThemeParseResult parsed = Parse(Json);

        // Verify
        Assert.Equal("nouns[2]: not an object.", Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void A_noun_without_a_usable_value_is_named_by_its_position()
    {
        // Setup - one without the key at all, one holding blanks, after a good one.
        const string Json = """{ "adjectives": {}, "nouns": [{ "value": "moon" }, { "categories": [] }, { "value": "   " }] }""";

        // Exercise
        ThemeParseResult parsed = Parse(Json);

        // Verify
        Assert.Equal(
            ["nouns[1]: no non-empty \"value\".", "nouns[2]: no non-empty \"value\"."],
            Messages(parsed));
    }

    /// <summary>
    /// A name reaches the file written the way people write it, and comes out holding only what
    /// a slug may carry. This is the load-time half of that promise; WordNormalizerTests pins the
    /// rule itself.
    /// </summary>
    [Fact]
    public void A_name_written_with_punctuation_arrives_as_words_a_slug_can_join()
    {
        // Exercise
        ThemeParseResult parsed = Parse(
            """{ "adjectives": {}, "nouns": [{ "value": "Jack O'Neil" }, { "value": "Jean-Luc Picard" }] }""");

        // Verify
        Assert.Empty(Messages(parsed));
        Assert.Equal(["jack o neil", "jean luc picard"], parsed.Theme!.Nouns.Select(noun => noun.Value));
    }

    /// <summary>
    /// The hole that reducing punctuation to boundaries opens: "!?&amp;" is not blank, so it clears
    /// the check above, and normalization then leaves nothing to draw. Refused rather than
    /// carried as a noun with no name.
    /// </summary>
    [Fact]
    public void A_noun_written_only_of_punctuation_is_refused_and_quoted_back()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [{ "value": "!?&" }] }""");

        // Verify
        Assert.Equal("nouns[0]: \"!?&\" holds no letter or digit.", Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void An_adjective_written_only_of_punctuation_is_refused_with_its_category()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": { "common": ["keen", "---"] }, "nouns": [] }""");

        // Verify
        Assert.Equal(
            "\"adjectives.common\" must be an array of words, each holding a letter or a digit.",
            Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void A_noun_may_refuse_words_its_categories_would_otherwise_reach()
    {
        // Exercise
        ThemeParseResult parsed = Parse(
            """{ "adjectives": {}, "nouns": [{ "value": "Wozniak", "except": ["Boring", "dull"] }] }""");

        // Verify - read through the same normalization as a word list, so "Boring" matches "boring".
        Assert.Empty(Messages(parsed));
        Assert.Equal(["boring", "dull"], parsed.Theme!.Nouns[0].Except);
    }

    [Fact]
    public void An_except_that_is_not_an_array_is_named_by_its_noun()
    {
        // Exercise
        ThemeParseResult parsed = Parse(
            """{ "adjectives": {}, "nouns": [{ "value": "moon", "except": "boring" }] }""");

        // Verify
        Assert.Equal("nouns[0]: \"except\" is not an array.", Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void Categories_that_are_not_an_array_are_named_by_their_noun()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [{ "value": "moon", "categories": "sea" }] }""");

        // Verify
        Assert.Equal("nouns[0]: \"categories\" is not an array.", Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void Categories_holding_something_other_than_a_string_are_named_by_their_noun()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [{ "value": "moon" }, { "value": "sun", "categories": ["sea", 7] }] }""");

        // Verify
        Assert.Equal("nouns[1]: \"categories\" holds something other than a string.", Assert.Single(Messages(parsed)));
    }

    /// <summary>
    /// An author who misspells a casing needs the list, not a verdict: the whole value of the
    /// message is the three words it ends with.
    /// </summary>
    [Fact]
    public void An_unknown_casing_is_refused_with_the_ones_that_exist()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "casing": "SHOUT" } }""");

        // Verify
        Assert.Equal("\"defaults.casing\" must be one of kebab, snake, camel.", Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void An_unknown_segment_mode_is_refused_with_the_ones_that_exist()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "segmentMode": "prefix" } }""");

        // Verify
        Assert.Equal(
            "\"defaults.segmentMode\" must be one of adjective, participle, either, both.",
            Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void A_separator_must_be_exactly_one_character()
    {
        // Exercise - too long, and not a string at all.
        ThemeParseResult tooLong = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "sep": "--" } }""");
        ThemeParseResult notAString = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "sep": 7 } }""");

        // Verify
        Assert.Equal("\"defaults.sep\" must be a single character.", Assert.Single(Messages(tooLong)));
        Assert.Equal("\"defaults.sep\" must be a single character.", Assert.Single(Messages(notAString)));
    }

    /// <summary>
    /// Nothing is a value for this key where it is not one for "sep": an empty word separator is
    /// how a theme asks for its compound values glued, so reading it as absent would silently
    /// hand back the separator instead.
    /// </summary>
    [Fact]
    public void A_word_separator_is_a_single_character_or_nothing_at_all()
    {
        // Exercise - the empty one, then too long, then not a string at all.
        ThemeParseResult glued = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "wordSep": "" } }""");
        ThemeParseResult tooLong = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "wordSep": "--" } }""");
        ThemeParseResult notAString = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "wordSep": 7 } }""");

        // Verify
        Assert.Empty(Messages(glued));
        Assert.Equal("", glued.Theme!.Defaults.WordSeparator);
        Assert.Equal("\"defaults.wordSep\" must be a single character or nothing.", Assert.Single(Messages(tooLong)));
        Assert.Equal("\"defaults.wordSep\" must be a single character or nothing.", Assert.Single(Messages(notAString)));
    }

    [Fact]
    public void A_numeric_default_that_is_not_a_number_names_the_key()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "tokenLength": "two" } }""");

        // Verify
        Assert.Equal("\"defaults.tokenLength\" must be a whole number.", Assert.Single(Messages(parsed)));
    }

    /// <summary>
    /// Every other malformed key inside "defaults" is reported as defaults.something; a boolean
    /// one must read the same way, or the author is told a key is wrong without being told where
    /// it lives - and "tokenHex" appears nowhere else in the file to look for.
    /// </summary>
    [Fact]
    public void A_boolean_default_names_the_section_it_lives_in()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "tokenHex": "yes" } }""");

        // Verify
        Assert.Equal("\"defaults.tokenHex\" must be true or false.", Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void A_theme_may_ask_for_its_own_accents_to_be_folded()
    {
        // Exercise
        ThemeParseResult parsed = Parse(
            """{ "adjectives": {}, "nouns": [], "defaults": { "foldAccents": true } }""");

        // Verify
        Assert.Empty(Messages(parsed));
        Assert.True(parsed.Theme!.Defaults.FoldAccents);
    }

    [Fact]
    public void A_theme_may_ask_for_an_ascii_slug()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [], "defaults": { "ascii": true } }""");

        // Verify
        Assert.Empty(Messages(parsed));
        Assert.True(parsed.Theme!.Defaults.Ascii);
    }

    [Fact]
    public void Allow_small_is_false_when_the_file_says_nothing()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }] }""");

        // Verify
        Assert.False(parsed.Theme!.AllowSmall);
        Assert.Empty(parsed.ShapeErrors);
    }

    [Fact]
    public void Allow_small_must_be_true_or_false()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [], "allowSmall": "yes" }""");

        // Verify - top level, so the bare name is the whole address.
        Assert.Equal("\"allowSmall\" must be true or false.", Assert.Single(Messages(parsed)));
    }

    /// <summary>
    /// Both halves of a pair go through the same normalization as the word lists (DEC0017), for
    /// the reason an exclusion does: a pair that missed on casing would fail open, and a pair
    /// that fails open is worse than no pair at all.
    /// </summary>
    [Fact]
    public void A_pair_is_normalized_on_both_sides_like_every_other_word()
    {
        // Exercise
        ThemeParseResult parsed = Parse(
            """{ "adjectives": {}, "nouns": [], "incompatible": { "  Frozen ": ["BURNING"] } }""");

        // Verify
        Assert.Empty(Messages(parsed));
        Assert.Equal(["burning"], Assert.Contains("frozen", parsed.Theme!.Incompatible));
    }

    [Fact]
    public void Incompatible_must_be_an_object_of_adjective_to_participles()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": {}, "nouns": [], "incompatible": ["frozen"] }""");

        // Verify
        Assert.Equal(
            "\"incompatible\" must be an object of adjective to participles.",
            Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void What_an_adjective_refuses_must_be_an_array_of_participles()
    {
        // Exercise
        ThemeParseResult parsed = Parse(
            """{ "adjectives": {}, "nouns": [], "incompatible": { "frozen": "burning" } }""");

        // Verify
        Assert.Equal(
            "\"incompatible.frozen\" must be an array of participles.",
            Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void A_theme_declares_no_pair_when_the_file_says_nothing()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": { "common": ["keen"] }, "nouns": [] }""");

        // Verify
        Assert.False(parsed.Theme!.HasIncompatibilities);
    }

    /// <summary>
    /// At the root rather than in "defaults" (DEC0018): "defaults" are switched off as soon as a
    /// second theme is in scope, and a promise that lapses when a theme is added is not a promise.
    /// </summary>
    [Fact]
    public void A_theme_declares_its_length_promise_at_the_root_and_one_shape_at_a_time()
    {
        // Exercise
        ThemeParseResult parsed = Parse(
            """{ "adjectives": {}, "nouns": [], "maxLength": { "twoWords": 63 } }""");

        // Verify
        Assert.Empty(Messages(parsed));
        Assert.Equal(63, parsed.Theme!.MaxLength.TwoWords);
        Assert.Null(parsed.Theme.MaxLength.ThreeWords);
    }

    [Fact]
    public void A_length_promise_must_be_a_whole_number_of_characters_above_zero()
    {
        // Exercise
        ThemeParseResult parsed = Parse(
            """{ "adjectives": {}, "nouns": [], "maxLength": { "twoWords": 0 } }""");

        // Verify
        Assert.Equal(
            "\"maxLength.twoWords\" must be a whole number of characters above zero.",
            Assert.Single(Messages(parsed)));
    }

    [Fact]
    public void A_theme_promises_nothing_about_length_when_the_file_says_nothing()
    {
        // Exercise
        ThemeParseResult parsed = Parse("""{ "adjectives": { "common": ["keen"] }, "nouns": [] }""");

        // Verify
        Assert.False(parsed.Theme!.MaxLength.Declared);
    }

    private static ThemeParseResult Parse(string json) => new JsonThemeSerializer().Deserialize("theme", json);

    private static IReadOnlyList<string> Messages(ThemeParseResult parsed) =>
        [.. parsed.ShapeErrors.Select(error => error.DiagnosticMessage)];
}
