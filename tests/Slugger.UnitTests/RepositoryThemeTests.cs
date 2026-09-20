using FirstClassErrors;
using Slugger.Domain;

namespace Slugger.UnitTests;

/// <summary>
/// The themes the repository carries without compiling them in - <c>themes/</c>. They are handed
/// to a reader as working files, so a validation rule that refuses one has to be a red build here
/// rather than a discovery by whoever downloads it. The floors are a ratchet: raising one means
/// growing these too, and this is what says so.
/// </summary>
/// <remarks>
/// The real directory is read rather than a copy staged into the test output, which would answer
/// for the state of the last build where the point of the guard is the state of the repository.
/// </remarks>
public sealed class RepositoryThemeTests
{
    private static readonly string Directory = FindTheThemeDirectory();

    /// <summary>The file name alone, so a failure names the theme rather than someone's disk.</summary>
    public static TheoryData<string> EveryTheme()
    {
        TheoryData<string> themes = [];
        foreach (string path in Files())
        {
            themes.Add(Path.GetFileName(path));
        }

        return themes;
    }

    /// <summary>
    /// A theory over an empty directory runs nothing and reports nothing, so the guard would
    /// pass by having found no theme to guard. This is what fails when the directory moves.
    /// </summary>
    [Fact]
    public void The_theme_directory_holds_at_least_one_theme() => Assert.NotEmpty(Files());

    [Theory]
    [MemberData(nameof(EveryTheme))]
    public void Loads_with_no_refusal(string file)
    {
        // Exercise - no allowSmall: a theme offered to a reader clears the floors like any other.
        Outcome<Theme> outcome = Themes.LoadFromFileResult(Path.Combine(Directory, file));

        // Verify
        Assert.True(outcome.IsSuccess, Refusals(outcome));
    }

    private static string[] Files() =>
        [.. System.IO.Directory.EnumerateFiles(Directory, "*.json").Order(StringComparer.Ordinal)];

    /// <summary>Every reason, not the first: fixing the theme should be one pass (DEC0006).</summary>
    private static string Refusals(Outcome<Theme> outcome) => string.Join(
        Environment.NewLine,
        (outcome.Error?.InnerErrors ?? []).Select(reason => reason.DiagnosticMessage));

    private static string FindTheThemeDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "slugger.slnx")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new InvalidOperationException(
                $"No slugger.slnx above {AppContext.BaseDirectory}, so the repository's themes cannot be found.")
            : Path.Combine(directory.FullName, "themes");
    }
}
