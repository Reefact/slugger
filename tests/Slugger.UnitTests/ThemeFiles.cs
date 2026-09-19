using System.Globalization;

namespace Slugger.UnitTests;

/// <summary>Theme documents built for a test to read, with only the part under test spelled out.</summary>
internal static class ThemeFiles
{
    /// <summary>A document that clears every rule, so a test can break exactly one thing in it.</summary>
    internal static string Valid(int nouns = 120, int adjectives = 120, int participles = 4) =>
        $$"""
          {
            "adjectives": { "common": [{{Words("adj", adjectives)}}] },
            "participles": { "common": [{{Words("part", participles)}}] },
            "nouns": [{{Nouns(nouns)}}]
          }
          """;

    private static string Words(string prefix, int count) => string.Join(
        ", ",
        Enumerable.Range(0, count).Select(index => $"\"{prefix}{index.ToString(CultureInfo.InvariantCulture)}\""));

    private static string Nouns(int count) => string.Join(
        ", ",
        Enumerable.Range(0, count).Select(index => $"{{ \"value\": \"noun{index.ToString(CultureInfo.InvariantCulture)}\" }}"));
}
