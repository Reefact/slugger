using System.Text.Json;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger.Infrastructure.Tests;

public class EmbeddedThemeCatalogTests
{
    [Fact]
    public void The_three_built_in_themes_are_embedded_in_the_assembly() =>
        new EmbeddedThemeCatalog().ListNames().ShouldBe(["docker", "heroku", "slugger"]);

    [Fact]
    public void An_unknown_theme_has_no_stream() =>
        new EmbeddedThemeCatalog().OpenStream("not-a-theme").ShouldBeNull();

    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Each_built_in_theme_is_a_well_formed_theme_document(string name)
    {
        using var document = JsonDocument.Parse(ReadEmbedded(name));
        var root = document.RootElement;

        root.TryGetProperty("adjectives", out var adjectives).ShouldBeTrue();
        adjectives.ValueKind.ShouldBe(JsonValueKind.Object);

        root.TryGetProperty("nouns", out var nouns).ShouldBeTrue();
        nouns.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    /// <summary>
    /// The spec claims all three built-in themes clear the minimum size rules on their own,
    /// with no allowSmall. This pins the two counts that can be checked without the resolver.
    /// </summary>
    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Each_built_in_theme_clears_the_minimum_counts_without_allow_small(string name)
    {
        using var document = JsonDocument.Parse(ReadEmbedded(name));
        var root = document.RootElement;

        root.GetProperty("nouns").GetArrayLength().ShouldBeGreaterThanOrEqualTo(ThemeValidator.MinimumNouns);

        var adjectiveCount = root.GetProperty("adjectives")
            .EnumerateObject()
            .Sum(category => category.Value.GetArrayLength());
        adjectiveCount.ShouldBeGreaterThanOrEqualTo(ThemeValidator.MinimumPoolPerNoun);

        var declaresAllowSmall = root.TryGetProperty("allowSmall", out var allowSmall)
                                 && allowSmall.ValueKind == JsonValueKind.True;
        declaresAllowSmall.ShouldBeFalse();
    }

    private static string ReadEmbedded(string name)
    {
        using var stream = new EmbeddedThemeCatalog().OpenStream(name);
        if (stream is null)
        {
            throw new InvalidOperationException($"theme '{name}' is not embedded");
        }

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
