#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Validation;
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

    /// <summary>What --analyze asks before it owes a report: a directory is not a file, and neither is nothing.</summary>
    [Fact]
    public void Tells_a_file_from_a_directory_and_from_nothing_at_all() {
        // Setup
        FileSystemThemeStore store = new(Directory);
        string               file  = _temp.WriteValidTheme("porno");

        // Exercise
        bool fileFound      = store.FileExists(file);
        bool directoryFound = store.FileExists(Directory);
        bool nothingFound   = store.FileExists(Path.Combine(Directory, "absent.json"));

        // Verify
        Assert.True(fileFound);
        Assert.False(directoryFound);
        Assert.False(nothingFound);
    }

    [Fact]
    public void Validates_a_file_handed_to_it_by_path() {
        // Setup
        string path = _temp.WriteValidTheme("porno");

        // Exercise
        Outcome<ThemeDocument> outcome = new FileSystemThemeStore(Directory).LoadFile(path);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    [Fact]
    public void Refuses_a_path_that_leads_nowhere() {
        // Setup
        string path = Path.Combine(Directory, "absent.json");

        // Exercise
        Outcome<ThemeDocument> outcome = new FileSystemThemeStore(Directory).LoadFile(path);

        // Verify - not found, by its path, rather than a section of a file that is not there.
        Error only = Assert.Single(outcome.Error!.InnerErrors);
        Assert.Equal(ThemeErrors.Codes.NotFound, only.Code);
        Assert.Equal($"\"{path}\" does not exist.", only.DiagnosticMessage);
    }

}