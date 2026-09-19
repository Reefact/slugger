using System.Text.Json;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger.Core.UnitTests;

public sealed class EmbeddedThemeCatalogTests
{
    [Fact]
    public void Serves_the_three_built_in_themes()
    {
        // Setup
        EmbeddedThemeCatalog catalog = new();

        // Exercise
        IReadOnlyList<string> names = catalog.ListNames();

        // Verify
        Assert.Equal(["docker", "heroku", "slugger"], names);
    }

    [Fact]
    public void Has_no_stream_for_a_theme_it_does_not_carry()
    {
        // Setup
        string unknownName = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();

        // Exercise
        Stream? stream = EmbeddedThemeCatalog.OpenStream(unknownName);

        // Verify
        Assert.Null(stream);
    }

    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Carries_a_well_formed_theme_document(string name)
    {
        // Setup
        string json = ReadEmbedded(name);

        // Exercise
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        // Verify
        Assert.True(root.TryGetProperty("adjectives", out JsonElement adjectives));
        Assert.Equal(JsonValueKind.Object, adjectives.ValueKind);
        Assert.True(root.TryGetProperty("nouns", out JsonElement nouns));
        Assert.Equal(JsonValueKind.Array, nouns.ValueKind);
    }

    /// <summary>
    /// The spec claims all three built-in themes clear the minimum size rules on their own,
    /// with no allowSmall. This pins the two counts that can be checked without the resolver.
    /// </summary>
    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Clears_the_minimum_counts_without_asking_for_allow_small(string name)
    {
        // Setup
        using JsonDocument document = JsonDocument.Parse(ReadEmbedded(name));
        JsonElement root = document.RootElement;

        // Exercise
        int nounCount = root.GetProperty("nouns").GetArrayLength();
        int adjectiveCount = root.GetProperty("adjectives")
            .EnumerateObject()
            .Sum(category => category.Value.GetArrayLength());
        bool asksForAllowSmall = root.TryGetProperty("allowSmall", out JsonElement allowSmall)
                                 && allowSmall.ValueKind == JsonValueKind.True;

        // Verify
        Assert.True(nounCount >= ThemeValidator.MinimumNouns, $"{name}: {nounCount} nouns");
        Assert.True(adjectiveCount >= ThemeValidator.MinimumPoolPerNoun, $"{name}: {adjectiveCount} adjectives");
        Assert.False(asksForAllowSmall);
    }

    private static string ReadEmbedded(string name)
    {
        using Stream? stream = EmbeddedThemeCatalog.OpenStream(name);
        if (stream is null)
        {
            throw new InvalidOperationException($"theme '{name}' is not embedded");
        }

        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }
}
