#region Usings declarations

using Slugger.Domain;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.UnitTests;

public sealed class ThemeDirectoryTests : IDisposable {

    #region Fields

    private readonly TemporaryDirectory _temp = new();

    #endregion

    public void Dispose() {
        _temp.Dispose();
    }

    [Fact]
    public void Builds_a_catalog_where_a_custom_file_shadows_the_built_in_theme() {
        // Setup
        _temp.WriteValidTheme("docker");

        // Exercise
        Theme docker = new ThemeDirectory().CatalogFor(_temp.Path).Load("docker").GetResultOrThrow();

        // Verify - the custom file's 120 nouns, not the built-in theme's 236.
        Assert.Equal(120, docker.Nouns.Count);
    }

    [Fact]
    public void Its_embedded_catalog_carries_only_what_is_compiled_in() {
        // Exercise
        IReadOnlyList<string> names = new ThemeDirectory().Embedded.ListNames();

        // Verify
        Assert.Equal(["docker", "heroku", "slugger"], names);
    }

    [Fact]
    public void Builds_a_store_over_the_directory_it_was_asked_for() {
        // Exercise
        new ThemeDirectory().StoreFor(_temp.Path).Save("porno", "{}");

        // Verify
        Assert.True(File.Exists(Path.Combine(_temp.Path, "porno.json")));
    }

}