#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.UnitTests;

public sealed class FileSystemThemeCatalogTests : IDisposable {

    #region Fields

    private readonly TemporaryDirectory _temp = new();

    #endregion

    private string Directory => _temp.Path;

    public void Dispose() {
        _temp.Dispose();
    }

    [Fact]
    public void Loads_a_theme_from_its_file_name() {
        // Setup
        _temp.WriteValidTheme("spices");
        FileSystemThemeCatalog catalog = new(Directory);

        // Exercise
        Outcome<ThemeDocument> outcome = catalog.Load("spices");

        // Verify - the name comes from the file, never from a field inside it.
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.Equal("spices", outcome.GetResultOrThrow().Name);
    }

    [Fact]
    public void Lists_the_json_files_it_finds() {
        // Setup
        _temp.WriteValidTheme("beta");
        _temp.WriteValidTheme("alpha");
        File.WriteAllText(Path.Combine(Directory, "notes.txt"), "ignored");

        // Exercise
        IReadOnlyList<string> names = new FileSystemThemeCatalog(Directory).ListNames();

        // Verify
        Assert.Equal(["alpha", "beta"], names);
    }

    /// <summary>
    ///     --register refuses a name --theme splits on its comma, but nothing stops a file named so
    ///     from being dropped in the folder by hand. Listed, it was a name nobody could ask for, and
    ///     "--theme '*'" drew from it all the same.
    /// </summary>
    [Fact]
    public void Leaves_out_a_file_whose_name_no_theme_option_could_select() {
        // Setup
        string selectable = Dummies.AnyWord();
        _temp.WriteValidTheme(selectable);
        _temp.WriteValidTheme($"{Dummies.AnyWord()},{Dummies.AnyWord()}");

        // Exercise
        IReadOnlyList<string> names = new FileSystemThemeCatalog(Directory).ListNames();

        // Verify
        Assert.Equal([selectable], names);
    }

    [Fact]
    public void A_directory_that_does_not_exist_simply_carries_nothing() {
        // Exercise
        IReadOnlyList<string> names = new FileSystemThemeCatalog(Path.Combine(Directory, "absent")).ListNames();

        // Verify
        Assert.Empty(names);
    }

    [Fact]
    public void Reports_a_theme_it_does_not_carry_rather_than_returning_nothing() {
        // Exercise
        Outcome<ThemeDocument> outcome = new FileSystemThemeCatalog(Directory).Load("absent");

        // Verify
        Error only = Assert.Single(outcome.Error!.InnerErrors);
        Assert.Equal(ThemeErrors.Codes.NotFound, only.Code);
    }

}