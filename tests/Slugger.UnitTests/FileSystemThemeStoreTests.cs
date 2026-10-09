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
        store.Save("spices", "{}");

        // Verify
        Assert.True(File.Exists(Path.Combine(fresh, "spices.json")));
    }

    [Fact]
    public void Knows_what_it_holds_and_forgets_what_it_deletes() {
        // Setup
        FileSystemThemeStore store = new(Directory);
        store.Save("spices", "{}");

        // Exercise
        bool held = store.Contains("spices");
        store.Delete("spices");

        // Verify
        Assert.True(held);
        Assert.False(store.Contains("spices"));
    }

    /// <summary>What --analyze asks before it owes a report: a directory is not a file, and neither is nothing.</summary>
    [Fact]
    public void Tells_a_file_from_a_directory_and_from_nothing_at_all() {
        // Setup
        FileSystemThemeStore store = new(Directory);
        string               file  = _temp.WriteValidTheme("spices");

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
        string path = _temp.WriteValidTheme("spices");

        // Exercise
        Outcome<ThemeDocument> outcome = new FileSystemThemeStore(Directory).LoadFile(path);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     The catalog leaves such a file out of every listing, so whoever dropped it there is told
    ///     why, by its path, with the rule its name breaks and what to do about it.
    /// </summary>
    [Fact]
    public void Names_a_file_whose_name_no_theme_option_could_select() {
        // Setup
        _temp.WriteValidTheme(Dummies.AnyWord());
        string unselectable = _temp.WriteValidTheme($"{Dummies.AnyWord()},{Dummies.AnyWord()}");

        // Exercise
        IReadOnlyList<string> remarks = new FileSystemThemeStore(Directory).Unselectable();

        // Verify
        Assert.Equal(
            [$"{unselectable} is ignored: --theme splits its value on commas, so no --theme could ever select it. Rename the file."],
            remarks);
    }

    [Fact]
    public void Names_nothing_in_a_directory_that_does_not_exist() {
        // Exercise
        IReadOnlyList<string> remarks = new FileSystemThemeStore(Path.Combine(Directory, "absent")).Unselectable();

        // Verify
        Assert.Empty(remarks);
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