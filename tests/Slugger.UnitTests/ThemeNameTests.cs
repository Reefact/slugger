#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What <c>docs/ubiquitous-language.md</c> says a theme is asked for by: a name, which is also
///     the stem of the file a catalog serves it from. These are written from that reading rather
///     than from the type, so that a rule nobody implemented comes out red.
/// </summary>
public sealed class ThemeNameTests : IDisposable {

    #region Fields

    private readonly TemporaryDirectory _temp = new();

    #endregion

    public void Dispose() {
        _temp.Dispose();
    }

    [Fact]
    public void Reads_a_plain_name_as_the_theme_it_names() {
        // Setup
        string name = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();

        // Exercise
        Outcome<ThemeName> outcome = ThemeName.From(name);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     Eleven of the twelve shipped themes are spelled with a hyphen, so a name is more than
    ///     letters and the rule has to say which characters it is actually about.
    /// </summary>
    [Fact]
    public void Reads_a_hyphenated_name_because_the_shipped_themes_are_spelled_that_way() {
        // Exercise
        Outcome<ThemeName> outcome = ThemeName.From("french-gastronomy");

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    [Fact]
    public void Trims_the_whitespace_around_a_name_because_it_names_the_same_theme() {
        // Setup
        string name = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();

        // Verify
        Assert.Equal(ThemeName.FromOrThrow(name), ThemeName.FromOrThrow($"  {name}  "));
    }

    [Fact]
    public void Refuses_a_value_of_whitespace_alone_as_the_no_name_it_is() {
        // Exercise
        Outcome<ThemeName> outcome = ThemeName.From("   ");

        // Verify
        Assert.Equal(ThemeNameError.Codes.Empty, outcome.Error?.Code);
    }

    /// <summary>
    ///     Why the rule exists, measured rather than asserted about: a catalog locates a theme by
    ///     combining the name with the directory it serves, and a rooted name replaces that
    ///     directory outright. The two halves are one test on purpose - the refusal is only worth
    ///     anything while the combination still behaves this way.
    /// </summary>
    [Fact]
    public void Refuses_a_rooted_name_which_would_otherwise_replace_the_theme_directory() {
        // Setup
        FileSystemThemeCatalog catalog = new(_temp.Path);

        // Exercise
        string located = catalog.PathFor("/etc/passwd");

        // Verify
        Assert.DoesNotContain(_temp.Path, located, StringComparison.Ordinal);
        Assert.Equal(ThemeNameError.Codes.CarriesAPathSeparator, ThemeName.From("/etc/passwd").Error?.Code);
    }

    /// <summary>
    ///     The other half of the same measurement: a relative name stays under the directory as a
    ///     string and walks out of it as a path, which is what opens the file.
    /// </summary>
    [Fact]
    public void Refuses_a_traversing_name_which_would_otherwise_walk_out_of_the_theme_directory() {
        // Setup
        FileSystemThemeCatalog catalog = new(_temp.Path);

        // Exercise
        string located = catalog.PathFor("../escaped");

        // Verify
        Assert.NotEqual(_temp.Path, Path.GetDirectoryName(Path.GetFullPath(located)));
        Assert.Equal(ThemeNameError.Codes.CarriesAPathSeparator, ThemeName.From("../escaped").Error?.Code);
    }

    /// <summary>
    ///     A backslash is a separator on Windows and an ordinary character on Linux. It is refused
    ///     on both, so that a theme named on one machine is a theme on the next rather than a file
    ///     nothing can open.
    /// </summary>
    [Fact]
    public void Refuses_a_backslash_wherever_it_runs_because_a_name_outlives_the_machine_that_wrote_it() {
        // Exercise
        Outcome<ThemeName> outcome = ThemeName.From(@"themes\docker");

        // Verify
        Assert.Equal(ThemeNameError.Codes.CarriesAPathSeparator, outcome.Error?.Code);
    }

    [Fact]
    public void Refuses_a_volume_separator_for_the_same_reason_as_a_backslash() {
        // Exercise
        Outcome<ThemeName> outcome = ThemeName.From("c:docker");

        // Verify
        Assert.Equal(ThemeNameError.Codes.CarriesAPathSeparator, outcome.Error?.Code);
    }

    /// <summary>
    ///     "." and ".." carry no separator, so the rule about paths lets them through and this one
    ///     has to catch them: ".." is the half of a traversal that does the walking.
    /// </summary>
    [Fact]
    public void Refuses_the_parent_directory_which_carries_no_separator_to_be_caught_by() {
        // Exercise
        Outcome<ThemeName> outcome = ThemeName.From("..");

        // Verify
        Assert.Equal(ThemeNameError.Codes.NamesADirectory, outcome.Error?.Code);
    }

    [Fact]
    public void Refuses_the_current_directory_for_the_same_reason() {
        // Exercise
        Outcome<ThemeName> outcome = ThemeName.From(".");

        // Verify
        Assert.Equal(ThemeNameError.Codes.NamesADirectory, outcome.Error?.Code);
    }

    /// <summary>
    ///     A dot inside a name is nothing but a character - only the whole value being "." or ".."
    ///     names a directory, and refusing the character would refuse a theme called "v1.2".
    /// </summary>
    [Fact]
    public void Reads_a_dotted_name_because_only_the_whole_value_can_name_a_directory() {
        // Exercise
        Outcome<ThemeName> outcome = ThemeName.From("retro.computing");

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     The case is the file system's business: a directory on Linux tells "Docker" from
    ///     "docker" and one on Windows does not, so folding it here would make the domain answer
    ///     for a file it cannot open. Unlike <c>Category</c>, which nothing but the domain matches.
    /// </summary>
    [Fact]
    public void Keeps_the_case_because_whatever_holds_the_themes_is_what_matches_a_name() {
        // Setup
        string name = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();

        // Verify
        Assert.NotEqual(ThemeName.FromOrThrow(name), ThemeName.FromOrThrow(name.ToUpperInvariant()));
    }

    [Fact]
    public void Throws_its_own_exception_for_a_caller_with_no_report_to_fill() {
        // Verify
        Assert.Throws<ThemeNameException>(() => ThemeName.FromOrThrow("../escaped"));
    }

}
