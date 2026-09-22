#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.UnitTests;

public sealed class FileSystemThemeStoreTests : IDisposable {

    #region Fields

    private readonly TemporaryDirectory _temp = new();

    #endregion

    private string Directory => _temp.Path;

    public void Dispose() {
        _temp.Dispose();
    }

    [Fact]
    public void Creates_the_directory_on_a_first_save() {
        // Setup - a path that does not exist yet, as it would not on a fresh machine.
        string               fresh = Path.Combine(Directory, "themes");
        FileSystemThemeStore store = new(fresh);

        // Exercise
        store.Save("porno", "{}");

        // Verify
        Assert.True(File.Exists(Path.Combine(fresh, "porno.json")));
    }

    [Fact]
    public void Knows_what_it_holds_and_forgets_what_it_deletes() {
        // Setup
        FileSystemThemeStore store = new(Directory);
        store.Save("porno", "{}");

        // Exercise
        bool held = store.Contains("porno");
        store.Delete("porno");

        // Verify
        Assert.True(held);
        Assert.False(store.Contains("porno"));
    }

    [Fact]
    public void Validates_a_file_handed_to_it_by_path() {
        // Setup
        string path = _temp.WriteValidTheme("porno");

        // Exercise
        Outcome<Theme> outcome = new FileSystemThemeStore(Directory).LoadFile(path);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    [Fact]
    public void Refuses_a_path_that_leads_nowhere() {
        // Exercise
        Outcome<Theme> outcome = new FileSystemThemeStore(Directory).LoadFile(Path.Combine(Directory, "absent.json"));

        // Verify
        Assert.True(outcome.IsFailure);
    }

}