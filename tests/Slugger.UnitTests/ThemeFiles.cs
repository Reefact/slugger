#region Usings declarations

using System.Globalization;

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>Theme documents built for a test to read, with only the part under test spelled out.</summary>
internal static class ThemeFiles {

    #region Static members

    /// <summary>A document that clears every rule, so a test can break exactly one thing in it.</summary>
    /// <remarks>
    ///     The participle count is the floor a theme declaring participles has to clear, not a round
    ///     number: it was four until that floor existed, and this fixture promises to clear every
    ///     rule. Raise it with the floor rather than the other way round.
    /// </remarks>
    internal static string Valid(int          nouns                      = 120,
                                 int          adjectives                 = 120,
                                 int          participles                = 20,
                                 SegmentMode? segmentMode                = null,
                                 int          refusedByTheFirstAdjective = 0) {
        return $$"""
                 {
                   {{Defaults(segmentMode)}}"adjectives": { "common": [{{Words("adj", adjectives)}}] },
                   {{Section("participles", participles)}}{{Incompatible(refusedByTheFirstAdjective)}}"nouns": [{{Nouns(nouns)}}]
                 }
                 """;
    }

    /// <summary>
    ///     "adj0" refuses the first few participles, which is what takes a noun under the floor for
    ///     one adjective while leaving its unconditional count untouched (DEC0017).
    /// </summary>
    private static string Incompatible(int refused) {
        return refused > 0
            ? $$"""
                "incompatible": { "adj0": [{{Words("part", refused)}}] },
                """
            : string.Empty;
    }

    /// <summary>
    ///     The floors follow the theme's own segment mode (DEC0016), so a test about one of them
    ///     has to be able to declare it. Absent means absent: the document says nothing, which is
    ///     "both".
    /// </summary>
    private static string Defaults(SegmentMode? segmentMode) {
        return segmentMode is { } mode
            ? $$"""
                "defaults": { "segmentMode": "{{Spelling.Of(mode)}}" },
                """
            : string.Empty;
    }

    /// <summary>A section, or nothing at all when it holds no word - which is not the same file.</summary>
    private static string Section(string name, int count) {
        return count > 0
            ? $$"""
                "{{name}}": { "common": [{{Words(name[..4], count)}}] },
                """
            : string.Empty;
    }

    private static string Words(string prefix, int count) {
        return string.Join(
            ", ",
            Enumerable.Range(0, count).Select(index => $"\"{prefix}{index.ToString(CultureInfo.InvariantCulture)}\""));
    }

    private static string Nouns(int count) {
        return string.Join(
            ", ",
            Enumerable.Range(0, count).Select(index => $"{{ \"value\": \"noun{index.ToString(CultureInfo.InvariantCulture)}\" }}"));
    }

    #endregion

}