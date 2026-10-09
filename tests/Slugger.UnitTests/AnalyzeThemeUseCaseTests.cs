#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Domain.Analysis;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     <c>--analyze</c> measures a theme precisely when it does not pass - so a refusal is something
///     to report on, and only a path with nothing behind it is something to refuse.
/// </summary>
public sealed class AnalyzeThemeUseCaseTests {

    #region Static members

    private static string AnyThemePath() {
        return $"/tmp/{Dummies.AnyThemeNameOtherThanTheBuiltInOnes()}.json";
    }

    #endregion

    [Fact]
    public void Measures_a_theme_that_loads() {
        // Setup
        string         path  = AnyThemePath();
        FakeThemeStore store = new();
        store.Files[path] = GenerateSlugsUseCaseTests.ThemeNamed(Path.GetFileNameWithoutExtension(path));
        AnalyzeThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        Outcome<ThemeAnalysis> outcome = useCase.Execute(path, SluggerOptions.Empty);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.NotNull(outcome.GetResultOrThrow().Measurements);
    }

    /// <summary>A refused theme is analysed all the same: its refusals are what the report is for.</summary>
    [Fact]
    public void Reports_on_a_file_that_is_there_even_when_it_would_be_refused() {
        // Setup - a null entry is a file the fake refuses to load.
        string         path  = AnyThemePath();
        FakeThemeStore store = new();
        store.Files[path] = null;
        AnalyzeThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        Outcome<ThemeAnalysis> outcome = useCase.Execute(path, SluggerOptions.Empty);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.NotEmpty(outcome.GetResultOrThrow().Refusals);
    }

    /// <summary>
    ///     A path with no file behind it leaves nothing to analyse, so there is no analysis to hand
    ///     back - and no report for a caller to write beside a file that does not exist.
    /// </summary>
    [Fact]
    public void Refuses_a_path_with_no_file_behind_it_rather_than_report_on_it() {
        // Setup
        AnalyzeThemeUseCase useCase = new(new FakeThemeDirectory(store: new FakeThemeStore()), new FakeConfigStore());

        // Exercise
        Outcome<ThemeAnalysis> outcome = useCase.Execute(AnyThemePath(), SluggerOptions.Empty);

        // Verify
        Assert.True(outcome.IsFailure);
    }

}
