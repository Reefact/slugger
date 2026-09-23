#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     The themes the repository carries without compiling them in - <c>themes/</c>. They are handed
///     to a reader as working files, so a validation rule that refuses one has to be a red build here
///     rather than a discovery by whoever downloads it. The floors are a ratchet: raising one means
///     growing these too, and this is what says so.
/// </summary>
/// <remarks>
///     The real directory is read rather than a copy staged into the test output - see
///     <see cref="RepositoryDirectory" /> for where it is found and why that matters.
/// </remarks>
public sealed class RepositoryThemeTests {

    #region Static members

    /// <summary>The file name alone, so a failure names the theme rather than someone's disk.</summary>
    public static TheoryData<string> EveryTheme() {
        TheoryData<string> themes = [];
        foreach (string path in Files()) {
            themes.Add(Path.GetFileName(path));
        }

        return themes;
    }

    private static string[] Files() {
        return RepositoryDirectory.ThemeFiles();
    }

    /// <summary>Every reason, not the first: fixing the theme should be one pass (DEC0006).</summary>
    private static string Refusals(Outcome<Theme> outcome) {
        return string.Join(
            Environment.NewLine,
            (outcome.Error?.InnerErrors ?? []).Select(reason => reason.DiagnosticMessage));
    }

    #endregion

    /// <summary>
    ///     A theory over an empty directory runs nothing and reports nothing, so the guard would
    ///     pass by having found no theme to guard. This is what fails when the directory moves.
    /// </summary>
    [Fact]
    public void The_theme_directory_holds_at_least_one_theme() {
        Assert.NotEmpty(Files());
    }

    [Theory]
    [MemberData(nameof(EveryTheme))]
    public void Loads_with_no_refusal(string file) {
        // Exercise - no allowSmall: a theme offered to a reader clears the floors like any other.
        Outcome<Theme> outcome = Themes.LoadFromFileResult(Path.Combine(RepositoryDirectory.Themes, file));

        // Verify
        Assert.True(outcome.IsSuccess, Refusals(outcome));
    }

}