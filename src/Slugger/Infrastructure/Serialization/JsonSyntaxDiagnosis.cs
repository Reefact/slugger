#region Usings declarations

using System.Text;
using System.Text.Json;

using FirstClassErrors;

using Slugger.Domain.Validation;

#endregion

namespace Slugger.Infrastructure.Serialization;

/// <summary>
///     What a theme's author is told about a file System.Text.Json cannot parse: the line and the
///     column their editor shows, and the mistake in a few words. The parser's own message is written
///     for whoever calls the parser - it counts lines from zero and positions in bytes, and advises
///     changing the reader options, which a theme file has no say in.
/// </summary>
/// <remarks>
///     The common mistakes are recognised from the text at the position the parser reports, not from
///     the wording of its message: a trailing comma, a comment, a word outside double quotes, a file
///     that ends early. Anything else falls back to the first sentence of the parser's message.
/// </remarks>
internal static class JsonSyntaxDiagnosis {

    /// <summary>The four characters JSON reads as white space, and no other.</summary>
    private const string JsonWhiteSpace = " \t\r\n";

    /// <summary>What may stand right before a key or a value: a bare word anywhere else is part of something else.</summary>
    private const string BeforeAKeyOrAValue = "{[,: \t\r\n";

    #region Static members

    /// <param name="json">The document that did not parse.</param>
    /// <param name="malformed">What the parser said about it.</param>
    internal static DomainError Describe(string json, JsonException malformed) {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(malformed);

        long lineNumber = malformed.LineNumber ?? 0;
        long line       = lineNumber + 1;
        int  lineStart  = LineStart(json, lineNumber);
        (int offset, int column) = PositionOf(json, lineStart, malformed.BytePositionInLine ?? 0);

        if (BareWordAround(json, offset) is { } word) {
            int wordColumn = column - (offset - word.Start);

            return ThemeErrors.MalformedJson($"a key or a word must be written between double quotes, and {word.Text} is not.", line, wordColumn);
        }

        return ThemeErrors.MalformedJson(Recognised(json, offset) ?? FirstSentenceOf(malformed.Message), line, column);
    }

    /// <summary>Where a line starts, counting lines from zero as the parser does: only a line feed ends one.</summary>
    private static int LineStart(string json, long lineNumber) {
        int start = 0;
        for (long line = 0; line < lineNumber; line++) {
            start = json.IndexOf('\n', start) + 1;
        }

        return start;
    }

    /// <summary>
    ///     The character the parser stopped at, and its column counted from one. The parser counts
    ///     the bytes of the line once written in UTF-8, so "é" is two of them and one column.
    /// </summary>
    private static (int Offset, int Column) PositionOf(string json, int lineStart, long bytes) {
        int  offset   = lineStart;
        int  column   = 1;
        long consumed = 0;
        while (consumed < bytes && offset < json.Length) {
            Rune.DecodeFromUtf16(json.AsSpan(offset), out Rune character, out int read);
            consumed += character.Utf8SequenceLength;
            offset   += read;
            column++;
        }

        return (offset, column);
    }

    /// <summary>
    ///     A word written where a key or a value belongs, without the double quotes that make it one:
    ///     <c>{ adjectives: ... }</c>, <c>["keen", moon]</c>. The parser stops at its first letter, or
    ///     after it when the word starts like <c>true</c>, <c>false</c> or <c>null</c>, so the word is
    ///     looked for on both sides of the position.
    /// </summary>
    private static (int Start, string Text)? BareWordAround(string json, int offset) {
        int start = offset;
        while (start > 0 && char.IsLetterOrDigit(json[start - 1])) {
            start--;
        }

        int end = offset;
        while (end < json.Length && char.IsLetterOrDigit(json[end])) {
            end++;
        }

        string word = json[start..end];
        if (word.Length == 0 || !char.IsLetter(word[0])) { return null; }
        if (word is "true" or "false" or "null") { return null; }
        if (start > 0 && !BeforeAKeyOrAValue.Contains(json[start - 1])) { return null; }

        return (start, word);
    }

    /// <summary>The mistakes a hand-written theme file makes most, named in the author's words, or null for any other.</summary>
    private static string? Recognised(string json, int offset) {
        if (json.AsSpan().Trim(JsonWhiteSpace).IsEmpty) { return "the file is empty."; }
        if (json.AsSpan(offset).Trim(JsonWhiteSpace).IsEmpty) { return "the file ends before the JSON is complete."; }
        if (json[offset] == '/') { return "a comment is not allowed."; }
        if (json[offset] == '\'') { return "a key or a word must be written between double quotes, not single ones."; }
        if (ClosesRightAfterAComma(json, offset)) { return "a trailing comma is not allowed."; }

        return null;
    }

    /// <summary>Whether the parser stopped at the end of an object or an array whose last entry is followed by a comma.</summary>
    private static bool ClosesRightAfterAComma(string json, int offset) {
        if (json[offset] is not ('}' or ']')) { return false; }

        int before = json.AsSpan(0, offset).TrimEnd(JsonWhiteSpace).Length;

        return before > 0 && json[before - 1] == ',';
    }

    /// <summary>
    ///     The parser's first sentence alone: the ones after it advise whoever configured the parser,
    ///     and then repeat the position in its own counting.
    /// </summary>
    private static string FirstSentenceOf(string message) {
        int    end      = message.IndexOf(". ", StringComparison.Ordinal);
        string sentence = end < 0 ? message.TrimEnd('.') : message[..end];

        return $"{LowercaseInitial(sentence)}.";
    }

    /// <summary>
    ///     The sentence follows a colon, so its capital goes - unless the first word is an acronym,
    ///     which "JSON" often is.
    /// </summary>
    private static string LowercaseInitial(string sentence) {
        if (!char.IsLower(sentence.ElementAtOrDefault(1))) { return sentence; }

        return char.ToLowerInvariant(sentence[0]) + sentence[1..];
    }

    #endregion

}
