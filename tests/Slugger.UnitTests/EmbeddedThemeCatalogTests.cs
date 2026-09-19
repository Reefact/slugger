using System.Text.Json;
using FirstClassErrors;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger.UnitTests;

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
    /// with no allowSmall. This runs the real rules over the real files rather than counting
    /// list lengths: it is what caught that docker and heroku ship nouns with no category at
    /// all, which the spec's literal pool rule refuses.
    /// </summary>
    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Each_built_in_theme_loads_without_asking_for_allow_small(string name)
    {
        // Exercise
        Outcome<Theme> outcome = Themes.LoadEmbeddedResult(name);

        // Verify
        Assert.True(outcome.IsSuccess, $"{name}: {outcome.Error?.DiagnosticMessage}");
        Assert.False(outcome.GetResultOrThrow().AllowSmall);
    }

    /// <summary>
    /// The files on disk stay indented so a theme remains readable and diffable; what is embedded
    /// is minified by the build. This pins that the build step actually ran - without it the
    /// assembly silently carries 25 KB of whitespace, and nothing else would notice.
    /// </summary>
    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Carries_the_theme_minified(string name)
    {
        // Exercise
        string embedded = ReadEmbedded(name);

        // Verify - and it still parses, which the loading tests above exercise in full.
        Assert.DoesNotContain('\n', embedded);
        Assert.DoesNotContain("  ", embedded, StringComparison.Ordinal);
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
