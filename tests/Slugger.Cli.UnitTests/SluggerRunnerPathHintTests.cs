#region Usings declarations

using FirstClassErrors;

using Slugger.Domain.Validation;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     When a refused run earns the line saying where a folder goes: one theme not found, under a
///     name that reads as a path. SluggerRunnerTests shows the line through a real run; these hold
///     each condition apart, including those a run never reaches with another answer.
/// </summary>
/// <remarks>Literal on purpose: "./spices.json" is a path, which is the whole case.</remarks>
public sealed class SluggerRunnerPathHintTests {

    #region Static members

    private const string AskedByItsPath = "./spices.json";

    private static DomainError NotFound(string name) {
        return ThemeErrors.NotFound(name, ["docker", "heroku", "slugger"]);
    }

    #endregion

    [Fact]
    public void Points_to_the_folder_for_one_theme_not_found_under_a_path() {
        // Exercise
        bool hinted = SluggerRunner.AsksForAThemeByItsPath(ThemeErrors.Rejected(AskedByItsPath, [NotFound(AskedByItsPath)]));

        // Verify
        Assert.True(hinted);
    }

    /// <summary>Two reasons are two answers, and a line about one of them would read as about both.</summary>
    [Fact]
    public void Says_nothing_of_folders_when_the_run_was_refused_for_several_reasons() {
        // Exercise
        bool hinted = SluggerRunner.AsksForAThemeByItsPath(
            ThemeErrors.Rejected(AskedByItsPath, [NotFound(AskedByItsPath), NotFound("./rivers.json")]));

        // Verify
        Assert.False(hinted);
    }

    [Fact]
    public void Says_nothing_of_folders_when_the_one_reason_is_not_a_missing_theme() {
        // Exercise
        bool hinted = SluggerRunner.AsksForAThemeByItsPath(ThemeErrors.Rejected(AskedByItsPath, [ThemeErrors.TooFewNouns(3, 100)]));

        // Verify
        Assert.False(hinted);
    }

    /// <summary>A missing file is reported by its path and names no theme: there is no name to read as one.</summary>
    [Fact]
    public void Says_nothing_of_folders_when_the_missing_thing_carries_no_theme_name() {
        // Exercise
        bool hinted = SluggerRunner.AsksForAThemeByItsPath(ThemeErrors.Rejected(AskedByItsPath, [ThemeErrors.NoSuchFile(AskedByItsPath)]));

        // Verify
        Assert.False(hinted);
    }

    /// <summary>
    ///     FirstClassErrors keeps no null in a context, which is what lets the runner read a found name
    ///     as present; this goes red, rather than quietly wrong, the day that stops being true.
    /// </summary>
    [Fact]
    public void Says_nothing_of_folders_when_the_theme_name_was_recorded_empty() {
        // Setup
        DomainError nameless = DomainError.Create(ThemeErrors.Codes.NotFound, "not found", context => context.Add(ThemeErrors.ThemeName, null!))
                                          .WithPublicMessage("not found");

        // Exercise
        bool hinted = SluggerRunner.AsksForAThemeByItsPath(ThemeErrors.Rejected(AskedByItsPath, [nameless]));

        // Verify
        Assert.False(hinted);
    }

}
