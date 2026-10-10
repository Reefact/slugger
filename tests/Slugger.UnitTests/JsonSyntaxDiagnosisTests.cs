#region Usings declarations

using System.Text.Json;

using Slugger.Infrastructure.Serialization;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What an author reads about a theme file that is not JSON: one line and one column, both counted
///     from one as an editor shows them, and the mistake in a few words - never the parser's advice to
///     change its reader options, and never its own position counted from zero in bytes.
/// </summary>
/// <remarks>
///     Every document here is a literal on purpose: the exact character at fault is the whole case,
///     and the column asserted is counted on it.
/// </remarks>
public sealed class JsonSyntaxDiagnosisTests {

    #region Static members

    private static string Diagnosis(string json) {
        ThemeParseResult parsed = new JsonThemeSerializer().Deserialize("theme", json);

        return Assert.Single(parsed.ShapeErrors).DiagnosticMessage;
    }

    #endregion

    [Fact]
    public void A_trailing_comma_in_an_object_is_named_where_the_object_closes() {
        // Exercise
        string diagnosis = Diagnosis("{\n  \"adjectives\": { \"common\": [\"keen\"] },\n}");

        // Verify
        Assert.Equal("The file is not valid JSON at line 3, column 1: a trailing comma is not allowed.", diagnosis);
    }

    [Fact]
    public void A_trailing_comma_in_an_array_is_named_where_the_array_closes() {
        // Exercise
        string diagnosis = Diagnosis("""{ "nouns": ["moon", "sun", ] }""");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 28: a trailing comma is not allowed.", diagnosis);
    }

    /// <summary>
    ///     System.Text.Json ends a line on a line feed alone, so a carriage return before it is the
    ///     end of the previous line rather than a line of its own.
    /// </summary>
    [Fact]
    public void Counts_the_lines_of_a_file_written_with_carriage_returns_as_an_editor_does() {
        // Exercise
        string diagnosis = Diagnosis("{\r\n  \"adjectives\": {},\r\n}");

        // Verify
        Assert.Equal("The file is not valid JSON at line 3, column 1: a trailing comma is not allowed.", diagnosis);
    }

    [Fact]
    public void A_closing_brace_that_follows_no_comma_is_not_called_a_trailing_comma() {
        // Exercise - one brace too many, after a complete document.
        string diagnosis = Diagnosis("""{ "adjectives": {} }}""");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 21: '}' is invalid after a single JSON value.", diagnosis);
    }

    [Fact]
    public void A_closing_bracket_at_the_very_start_is_not_called_a_trailing_comma() {
        // Exercise
        string diagnosis = Diagnosis("]");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 1: ']' is an invalid start of a value.", diagnosis);
    }

    [Fact]
    public void A_comment_is_named_where_it_starts() {
        // Exercise
        string diagnosis = Diagnosis("{\n  // the nouns come later\n  \"adjectives\": {}\n}");

        // Verify
        Assert.Equal("The file is not valid JSON at line 2, column 3: a comment is not allowed.", diagnosis);
    }

    [Fact]
    public void A_file_that_ends_before_its_object_closes_is_told_so() {
        // Exercise
        string diagnosis = Diagnosis("""{ "adjectives": """);

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 17: the file ends before the JSON is complete.", diagnosis);
    }

    [Fact]
    public void A_file_that_ends_inside_a_string_is_told_so() {
        // Exercise
        string diagnosis = Diagnosis("""{ "adjectives": "keen""");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 22: the file ends before the JSON is complete.", diagnosis);
    }

    [Fact]
    public void An_empty_file_is_told_so() {
        // Exercise
        string diagnosis = Diagnosis("");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 1: the file is empty.", diagnosis);
    }

    [Fact]
    public void A_file_of_nothing_but_white_space_is_empty_too() {
        // Exercise
        string diagnosis = Diagnosis("  ");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 3: the file is empty.", diagnosis);
    }

    [Fact]
    public void A_key_written_without_quotes_is_named_and_quoted_back() {
        // Exercise
        string diagnosis = Diagnosis("""{ adjectives: {} }""");

        // Verify
        Assert.Equal(
            "The file is not valid JSON at line 1, column 3: a key or a word must be written between double quotes, and adjectives is not.",
            diagnosis);
    }

    [Fact]
    public void A_word_written_without_quotes_after_a_comma_is_named() {
        // Exercise
        string diagnosis = Diagnosis("""{ "a": ["keen",moon] }""");

        // Verify
        Assert.Equal(
            "The file is not valid JSON at line 1, column 16: a key or a word must be written between double quotes, and moon is not.",
            diagnosis);
    }

    [Fact]
    public void A_word_written_without_quotes_at_the_start_of_an_array_is_named() {
        // Exercise
        string diagnosis = Diagnosis("[moon]");

        // Verify
        Assert.Equal(
            "The file is not valid JSON at line 1, column 2: a key or a word must be written between double quotes, and moon is not.",
            diagnosis);
    }

    /// <summary>
    ///     A word that starts like true, false or null is read as that literal for as long as it
    ///     matches, so the parser stops after its first letters. The column is the word's own.
    /// </summary>
    [Fact]
    public void A_word_that_starts_like_a_literal_is_named_from_its_first_letter() {
        // Exercise
        string diagnosis = Diagnosis("""{"a":tru}""");

        // Verify
        Assert.Equal(
            "The file is not valid JSON at line 1, column 6: a key or a word must be written between double quotes, and tru is not.",
            diagnosis);
    }

    [Fact]
    public void A_document_that_is_only_words_is_named_by_its_first_one() {
        // Exercise
        string diagnosis = Diagnosis("not json at all");

        // Verify
        Assert.Equal(
            "The file is not valid JSON at line 1, column 1: a key or a word must be written between double quotes, and not is not.",
            diagnosis);
    }

    [Fact]
    public void A_word_between_single_quotes_is_told_which_quotes_to_use() {
        // Exercise
        string diagnosis = Diagnosis("""{ "a": 'keen' }""");

        // Verify
        Assert.Equal(
            "The file is not valid JSON at line 1, column 8: a key or a word must be written between double quotes, not single ones.",
            diagnosis);
    }

    /// <summary>
    ///     A literal JSON knows is not a word to quote: two of them side by side are missing a comma,
    ///     which the parser's own sentence says better than a quoting hint would.
    /// </summary>
    [Fact]
    public void A_literal_out_of_place_is_not_mistaken_for_a_word_to_quote() {
        // Exercise
        string diagnosis = Diagnosis("""{ "a": [true false] }""");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 14: 'f' is invalid after a value.", diagnosis);
    }

    /// <summary>The same for each literal, whichever of them comes second.</summary>
    /// <param name="json">Two literals side by side.</param>
    /// <param name="expected">What the parser says about the second.</param>
    [Theory]
    [InlineData("""{ "a": [false true] }""", "The file is not valid JSON at line 1, column 15: 't' is invalid after a value.")]
    [InlineData("""{ "a": [true null] }""", "The file is not valid JSON at line 1, column 14: 'n' is invalid after a value.")]
    public void No_literal_is_mistaken_for_a_word_to_quote(string json, string expected) {
        // Exercise
        string diagnosis = Diagnosis(json);

        // Verify
        Assert.Equal(expected, diagnosis);
    }

    [Fact]
    public void Letters_inside_a_number_are_not_mistaken_for_a_word_to_quote() {
        // Exercise
        string diagnosis = Diagnosis("""{ "a": 1.e }""");

        // Verify
        Assert.Equal(
            "The file is not valid JSON at line 1, column 10: 'e' is invalid within a number, immediately after a decimal point ('.').",
            diagnosis);
    }

    [Fact]
    public void Letters_after_digits_are_not_mistaken_for_a_word_to_quote() {
        // Exercise
        string diagnosis = Diagnosis("""{ "a": 12abc }""");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 10: 'a' is an invalid end of a number.", diagnosis);
    }

    /// <summary>
    ///     Anything else keeps the parser's first sentence, starting lowercase after the colon, and
    ///     loses the rest - its advice and its own count of the position.
    /// </summary>
    [Fact]
    public void Any_other_mistake_keeps_the_first_sentence_of_the_parser_alone() {
        // Exercise
        string diagnosis = Diagnosis("""{ "a": 01 }""");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 9: invalid leading zero before '1'.", diagnosis);
    }

    /// <summary>The parser counts bytes, and "é" takes two of them in UTF-8 for one column on screen.</summary>
    [Fact]
    public void Counts_a_column_in_characters_rather_than_in_bytes() {
        // Exercise
        string diagnosis = Diagnosis("""{ "é": 1, }""");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 11: a trailing comma is not allowed.", diagnosis);
    }

    /// <summary>A character outside the basic plane is four bytes and two UTF-16 units, and still one column.</summary>
    [Fact]
    public void Counts_a_character_written_as_a_surrogate_pair_as_one_column() {
        // Exercise
        string diagnosis = Diagnosis("{ \"\U0001F319\": 1, }");

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 11: a trailing comma is not allowed.", diagnosis);
    }

    [Fact]
    public void Keeps_a_sentence_that_starts_with_an_acronym_as_it_is_written() {
        // Exercise
        string diagnosis = JsonSyntaxDiagnosis.Describe("{}", new JsonException("JSON is not welcome here.", null, 0, 0)).DiagnosticMessage;

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 1: JSON is not welcome here.", diagnosis);
    }

    [Fact]
    public void Reads_a_position_the_parser_does_not_give_as_the_start_of_the_file() {
        // Exercise
        string diagnosis = JsonSyntaxDiagnosis.Describe("{}", new JsonException("Something went wrong.")).DiagnosticMessage;

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 1: something went wrong.", diagnosis);
    }

    [Fact]
    public void Stops_a_position_past_the_end_of_the_file_at_its_end() {
        // Exercise
        string diagnosis = JsonSyntaxDiagnosis.Describe("{}", new JsonException("Too far.", null, 0, 99)).DiagnosticMessage;

        // Verify
        Assert.Equal("The file is not valid JSON at line 1, column 3: the file ends before the JSON is complete.", diagnosis);
    }

}
