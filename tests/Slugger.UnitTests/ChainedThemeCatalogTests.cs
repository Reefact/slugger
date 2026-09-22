#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.UnitTests;

public sealed class ChainedThemeCatalogTests : IDisposable {

    #region Fields

    private readonly TemporaryDirectory _temp = new();

    #endregion

    private string Directory => _temp.Path;

    public void Dispose() {
        _temp.Dispose();
    }

    [Fact]
    public void A_custom_file_shadows_the_built_in_theme_of_the_same_name() {
        // Setup - a custom "docker" beside the embedded one.
        _temp.WriteValidTheme("docker");
        ChainedThemeCatalog catalog = new(new FileSystemThemeCatalog(Directory), new EmbeddedThemeCatalog());

        // Exercise
        Theme docker = catalog.Load("docker").GetResultOrThrow();

        // Verify - the custom file's 120 nouns, not the built-in theme's 236.
        Assert.Equal(120, docker.Nouns.Count);
    }

    /// <summary>
    ///     The chain stops at the catalog that carries the name even when the theme is refused.
    ///     Falling through would hand back the built-in docker and leave the author's broken file
    ///     unmentioned.
    /// </summary>
    [Fact]
    public void A_broken_custom_file_is_reported_rather_than_skipped() {
        // Setup
        File.WriteAllText(Path.Combine(Directory, "docker.json"), """{ "adjectives": {}, "nouns": [] }""");
        ChainedThemeCatalog catalog = new(new FileSystemThemeCatalog(Directory), new EmbeddedThemeCatalog());

        // Exercise
        Outcome<Theme> outcome = catalog.Load("docker");

        // Verify
        Assert.True(outcome.IsFailure);
    }

    [Fact]
    public void Lists_both_origins_without_repeating_a_name() {
        // Setup
        _temp.WriteValidTheme("docker");
        _temp.WriteValidTheme("porno");
        ChainedThemeCatalog catalog = new(new FileSystemThemeCatalog(Directory), new EmbeddedThemeCatalog());

        // Exercise
        IReadOnlyList<string> names = catalog.ListNames();

        // Verify
        Assert.Equal(["docker", "heroku", "porno", "slugger"], names);
    }

}