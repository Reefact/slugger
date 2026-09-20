using System.Globalization;
using Slugger.Domain;

namespace Slugger.UnitTests;

/// <summary>Theme documents built for a test to read, with only the part under test spelled out.</summary>
internal static class ThemeFiles
{
    /// <summary>A document that clears every rule, so a test can break exactly one thing in it.</summary>
    /// <remarks>
    /// The participle count is the floor a theme declaring participles has to clear, not a round
    /// number: it was four until that floor existed, and this fixture promises to clear every
    /// rule. Raise it with the floor rather than the other way round.
    /// </remarks>
    internal static string Valid(
        int nouns = 120,
        int adjectives = 120,
        int participles = 20,
        SegmentMode? segmentMode = null) =>
        $$"""
          {
            {{Defaults(segmentMode)}}"adjectives": { "common": [{{Words("adj", adjectives)}}] },
            {{Section("participles", participles)}}"nouns": [{{Nouns(nouns)}}]
          }
          """;

    /// <summary>
    /// The floors follow the theme's own segment mode (DEC0016), so a test about one of them
    /// has to be able to declare it. Absent means absent: the document says nothing, which is
    /// "both".
    /// </summary>
    private static string Defaults(SegmentMode? segmentMode) => segmentMode is { } mode
        ? $$"""
            "defaults": { "segmentMode": "{{mode.ToString().ToLowerInvariant()}}" },
            """
        : string.Empty;

    /// <summary>A section, or nothing at all when it holds no word - which is not the same file.</summary>
    private static string Section(string name, int count) => count > 0
        ? $$"""
            "{{name}}": { "common": [{{Words(name[..4], count)}}] },
            """
        : string.Empty;

    private static string Words(string prefix, int count) => string.Join(
        ", ",
        Enumerable.Range(0, count).Select(index => $"\"{prefix}{index.ToString(CultureInfo.InvariantCulture)}\""));

    private static string Nouns(int count) => string.Join(
        ", ",
        Enumerable.Range(0, count).Select(index => $"{{ \"value\": \"noun{index.ToString(CultureInfo.InvariantCulture)}\" }}"));
}
