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
}
