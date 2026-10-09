#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Domain.Analysis;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     <c>--analyze</c> measures a theme precisely when it does not pass - so a refusal is something
///     to report on, and only a path with nothing behind it is something to refuse.
/// </summary>
public sealed class AnalyzeThemeUseCaseTests : IDisposable {

    #region Static members

    private static string AnyThemePath() {
        return $"/tmp/{Dummies.AnyThemeNameOtherThanTheBuiltInOnes()}.json";
    }

    /// <summary>A theme that clears every rule but one: its first noun lists a category nothing declares.</summary>
    private static string NamingACategoryItDoesNotDeclare(string category) {
        return ThemeFiles.Valid()
                         .Replace("""{ "value": "noun0" }""", $$"""{ "value": "noun0", "categories": ["{{category}}"] }""", StringComparison.Ordinal);
    }

    #endregion

    #region Fields

    private readonly TemporaryDirectory _temp = new();

    #endregion

    public void Dispose() {
        _temp.Dispose();
    }

    /// <summary>The real store and the real parser: what is under test is how a real file is read.</summary>
    private ThemeAnalysis AnalyseFile(string json) {
        string path = Path.Combine(_temp.Path, $"{Dummies.AnyThemeNameOtherThanTheBuiltInOnes()}.json");
        File.WriteAllText(path, json);
        AnalyzeThemeUseCase useCase = new(new ThemeDirectory(), new FakeConfigStore());

        return useCase.Execute(path, SluggerOptions.Empty).GetResultOrThrow();
    }

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

    /// <summary>
    ///     A theme refused for one of its rules is whole, and is measured: it used to get the one
    ///     coherence error and "the document could not be read", where --register gave two reasons
    ///     for the same file - the category is too poor as well, which only a measurement finds.
    /// </summary>
    [Fact]
    public void Measures_a_theme_refused_for_a_category_it_does_not_declare() {
        // Setup
        string category = Dummies.AnyCategoryOtherThanCommon();

        // Exercise
        ThemeAnalysis analysis = AnalyseFile(NamingACategoryItDoesNotDeclare(category));

        // Verify
        Assert.NotNull(analysis.Measurements);
        Assert.Contains(analysis.Refusals, reason => reason.Code == ThemeErrors.Codes.UnknownCategory);
        Assert.Contains(analysis.Refusals, reason => reason.Code == ThemeErrors.Codes.CategoryTooPoor);
        Assert.Equal(category, analysis.Measurements.Combinations!.Category);
    }

    /// <summary>The same for a word an exclusion names and the theme declares nowhere.</summary>
    [Fact]
    public void Measures_a_theme_refused_for_an_exclusion_matching_nothing() {
        // Setup
        string json = ThemeFiles.Valid()
                                .Replace("""{ "value": "noun0" }""", """{ "value": "noun0", "except": ["nowhere"] }""", StringComparison.Ordinal);

        // Exercise
        ThemeAnalysis analysis = AnalyseFile(json);

        // Verify
        Assert.NotNull(analysis.Measurements);
        Assert.Equal(ThemeErrors.Codes.ExclusionMatchesNothing, Assert.Single(analysis.Refusals).Code);
    }

    /// <summary>
    ///     A section of the wrong shape leaves a document rebuilt around a gap, which is not measured -
    ///     but the file was read, and the analysis says so rather than calling it unreadable.
    /// </summary>
    [Fact]
    public void Reports_a_section_of_the_wrong_shape_as_read_with_nothing_measured() {
        // Setup
        string json = ThemeFiles.Valid().Replace("\"adjectives\":", "\"allowSmall\": \"yes\", \"adjectives\":", StringComparison.Ordinal);

        // Exercise
        ThemeAnalysis analysis = AnalyseFile(json);

        // Verify
        Assert.Null(analysis.Measurements);
        Assert.True(analysis.Read);
        Assert.Equal(ThemeErrors.Codes.MalformedSection, Assert.Single(analysis.Refusals).Code);
    }

    [Fact]
    public void Reports_a_theme_holding_no_noun_as_read_with_nothing_measured() {
        // Exercise
        ThemeAnalysis analysis = AnalyseFile("""{ "adjectives": { "common": ["keen"] }, "nouns": [] }""");

        // Verify
        Assert.Null(analysis.Measurements);
        Assert.True(analysis.Read);
    }

    /// <summary>Only a file that is not JSON was never read at all.</summary>
    [Fact]
    public void Reports_a_file_that_is_not_json_as_never_read() {
        // Exercise
        ThemeAnalysis analysis = AnalyseFile("""{ "adjectives": """);

        // Verify
        Assert.Null(analysis.Measurements);
        Assert.False(analysis.Read);
    }

}
