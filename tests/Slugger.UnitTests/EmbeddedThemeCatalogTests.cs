#region Usings declarations

using System.Text.Json;

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.UnitTests;

public sealed class EmbeddedThemeCatalogTests {

    #region Static members

    private static string ReadEmbedded(string name) {
        using Stream? stream = EmbeddedThemeCatalog.OpenStream(name);
        if (stream is null) { throw new InvalidOperationException($"theme '{name}' is not embedded"); }

        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }

    #endregion

    [Fact]
    public void Serves_the_three_built_in_themes() {
        // Setup
        EmbeddedThemeCatalog catalog = new();

        // Exercise
        IReadOnlyList<string> names = catalog.ListNames();

        // Verify
        Assert.Equal(["docker", "heroku", "slugger"], names);
    }

    [Fact]
    public void Has_no_stream_for_a_theme_it_does_not_carry() {
        // Setup
        string unknownName = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();

        // Exercise
        Stream? stream = EmbeddedThemeCatalog.OpenStream(unknownName);

        // Verify
        Assert.Null(stream);
    }

    /// <summary>
    ///     A remark is not a refusal, so nothing forces the shipped themes to be free of them - which
    ///     is exactly why it is worth asserting. Measured when this was written: none of the three
    ///     declares a single word in both sections.
    /// </summary>
    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void A_built_in_theme_gives_an_author_nothing_to_reconsider(string name) {
        // Exercise
        IReadOnlyList<string> remarks = ThemeValidator.Remarks(Themes.LoadEmbedded(name));

        // Verify
        Assert.Empty(remarks);
    }

    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Carries_a_well_formed_theme_document(string name) {
        // Setup
        string json = ReadEmbedded(name);

        // Exercise
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement        root     = document.RootElement;

        // Verify
        Assert.True(root.TryGetProperty("adjectives", out JsonElement adjectives));
        Assert.Equal(JsonValueKind.Object, adjectives.ValueKind);
        Assert.True(root.TryGetProperty("nouns", out JsonElement nouns));
        Assert.Equal(JsonValueKind.Array, nouns.ValueKind);
    }

    /// <summary>
    ///     All three built-in themes clear the minimum size rules on their own, with no allowSmall.
    ///     This runs the real rules over the real files rather than counting list lengths: it is
    ///     what caught that docker and heroku ship nouns with no category at all, which a pool rule
    ///     without a shared floor refuses (DEC0002).
    /// </summary>
    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Each_built_in_theme_loads_without_asking_for_allow_small(string name) {
        // Exercise
        Outcome<ThemeDocument> outcome = Themes.LoadEmbeddedResult(name);

        // Verify
        Assert.True(outcome.IsSuccess, $"{name}: {outcome.Error?.DiagnosticMessage}");
        Assert.False(outcome.GetResultOrThrow().AllowSmall);
    }

    /// <summary>
    ///     The theme drawn when none is asked for is the one most people ever see, and it shipped
    ///     twenty-six compounds glued into one word - "firstclass", "soldout", "brandnew" - which
    ///     read as typos in a slug. Written with their hyphen, DEC0008 turns it into a word boundary
    ///     at load, so a kebab slug reads "first-class" again.
    /// </summary>
    /// <remarks>Literal on purpose: these are the words that shipped glued, and no other would do.</remarks>
    [Fact]
    public void The_default_theme_writes_its_compounds_as_they_are_spelled() {
        // Setup
        string[] glued = [
            "firstclass", "gametested", "goldstandard", "hardnosed", "ivycovered", "longawaited", "recordsetting",
            "welldeserved", "wellearned", "wellexecuted", "worldclass", "openair", "brokenin", "soldout",
            "battletested", "roadtested", "bluecollar", "hightech", "standardissue", "formfitting", "heavyduty",
            "brandnew", "brickwalled", "timehonored", "careerdefining", "richlydeserved"
        ];

        // Exercise
        string[] adjectives = [.. Themes.LoadEmbedded("slugger").Adjectives.Values.SelectMany(words => words)];

        // Verify
        Assert.Empty(adjectives.Intersect(glued, StringComparer.Ordinal));
        Assert.Contains("first class", adjectives);
        Assert.Contains("sold out", adjectives);
        Assert.Contains("brand new", adjectives);
        Assert.Contains("richly deserved", adjectives);
    }

    /// <summary>
    ///     <c>--theme-info slugger</c> is how someone finds out what the default theme is about, and
    ///     it used to say only that it was the default - never that it is baseball, which is the
    ///     whole of its charm.
    /// </summary>
    [Fact]
    public void The_default_theme_says_it_is_about_baseball() {
        // Exercise
        string? description = Themes.LoadEmbedded("slugger").Metadata.Description;

        // Verify
        Assert.NotNull(description);
        Assert.Contains("Baseball", description, StringComparison.Ordinal);
        Assert.Contains("players", description, StringComparison.Ordinal);
        Assert.Contains("ballparks", description, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The files on disk stay indented so a theme remains readable and diffable; what is embedded
    ///     is minified by the build. This pins that the build step actually ran - without it the
    ///     assembly silently carries 25 KB of whitespace, and nothing else would notice.
    /// </summary>
    [Theory]
    [InlineData("slugger")]
    [InlineData("heroku")]
    [InlineData("docker")]
    public void Carries_the_theme_minified(string name) {
        // Exercise
        string embedded = ReadEmbedded(name);

        // Verify - and it still parses, which the loading tests above exercise in full.
        Assert.DoesNotContain('\n', embedded);
        Assert.DoesNotContain("  ", embedded, StringComparison.Ordinal);
    }

}