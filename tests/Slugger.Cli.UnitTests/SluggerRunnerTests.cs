#region Usings declarations

using Slugger.Application.Abstractions;
using Slugger.Application.UseCases;
using Slugger.Cli.CommandLine;
using Slugger.Infrastructure.Configuration;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The runner over the real stack - real catalogs, a real config file in a directory of its own -
///     with only the terminal and the clipboard doubled. What is worth testing here is what the CLI
///     decides, and it decides it against the engine rather than against a mock of it.
/// </summary>
public sealed class SluggerRunnerTests : IDisposable {

    #region Static members

    private static string ValidTheme() {
        return $$"""{ "adjectives": { "common": [{{Words()}}] }, "nouns": [{{Nouns()}}] }""";
    }

    private static string Words() {
        return string.Join(", ", Enumerable.Range(0, 120).Select(index => $"\"adj{index}\""));
    }

    private static string Nouns() {
        return string.Join(", ", Enumerable.Range(0, 120).Select(index => $"{{ \"value\": \"noun{index}\" }}"));
    }

    #endregion

    #region Fields

    private readonly string        _directory = Path.Combine(Path.GetTempPath(), $"slugger-cli-{Guid.NewGuid():N}");
    private readonly FakeClipboard _clipboard = new();

    #endregion

    #region Constructors & Destructor

    public SluggerRunnerTests() {
        Directory.CreateDirectory(_directory);
    }

    #endregion

    public void Dispose() {
        if (Directory.Exists(_directory)) { Directory.Delete(_directory, true); }
    }

    [Fact]
    public void Draws_once_and_stops_when_standard_input_is_not_a_terminal() {
        // Setup - a pipe, a script or a CI runner: a ReadLine nobody will answer is a hang.
        FakeConsole console = new() { IsInputRedirected = true };

        // Exercise
        int exit = Run(console, "--theme", "docker");

        // Verify
        Assert.Equal(0, exit);
        Assert.Single(console.Output);
    }

    [Fact]
    public void Draws_once_and_stops_when_oneshot_was_asked_for() {
        // Setup - input is waiting, and --oneshot says not to read it.
        FakeConsole console = new("", "", "");

        // Exercise
        Run(console, "--theme", "docker", "--oneshot");

        // Verify
        Assert.Single(console.Output);
    }

    /// <summary>A round on start, then another on every Enter, until the input runs out.</summary>
    [Fact]
    public void Draws_again_on_every_line_until_there_are_no_more() {
        // Setup - two Enters after the opening round.
        FakeConsole console = new("", "");

        // Exercise
        Run(console, "--theme", "docker");

        // Verify
        Assert.Equal(3, console.Output.Count);
    }

    [Fact]
    public void Draws_as_many_slugs_a_round_as_count_asks_for() {
        // Setup
        FakeConsole console = new() { IsInputRedirected = true };

        // Exercise
        Run(console, "--theme", "docker", "--count", "4");

        // Verify
        Assert.Equal(4, console.Output.Count);
    }

    [Fact]
    public void Copies_the_last_slug_of_a_round_when_the_clipboard_was_asked_for() {
        // Setup
        FakeConsole console = new() { IsInputRedirected = true };

        // Exercise
        Run(console, "--theme", "docker", "--count", "3", "--clipboard");

        // Verify
        Assert.Equal(console.Output[^1], _clipboard.LastCopied);
    }

    [Fact]
    public void Lists_the_themes_in_scope() {
        // Setup
        FakeConsole console = new();

        // Exercise
        Run(console, "--list-themes");

        // Verify
        Assert.Equal(["docker", "heroku", "slugger"], console.Output);
    }

    /// <summary>
    ///     DEC0006 on the command line: the options that bound are all converted before anything is
    ///     refused, so two typos are two complaints in one run rather than two runs.
    /// </summary>
    [Fact]
    public void Refuses_every_option_it_could_not_make_sense_of_at_once() {
        // Setup
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--casing", "SHOUT", "--count", "0");

        // Verify - nothing on standard output, so a pipe downstream gets no rubbish.
        Assert.Equal(SluggerRunner.Refused, exit);
        Assert.Empty(console.Output);
        Assert.Contains(
            console.Errors,
            line => line.Contains("The command line was refused for 2 reasons:", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Worth a test of its own, because Spectre's own answer is to ignore it: an option it does
    ///     not know goes into the remaining arguments and nothing else happens, so without someone
    ///     reading those, "--themme docker" draws from the default theme and says nothing at all
    ///     (measured, DEC0019).
    /// </summary>
    [Fact]
    public void Refuses_an_option_it_does_not_know_rather_than_ignoring_it() {
        // Setup
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--themme", "docker");

        // Verify
        Assert.Equal(SluggerRunner.Refused, exit);
        Assert.Empty(console.Output);
        Assert.Contains(console.Errors, line => line.Contains("themme", StringComparison.Ordinal));
    }

    /// <summary>
    ///     The reason for taking Spectre at all. The text itself is generated from the options'
    ///     declaration and is Spectre's to lay out; what is slugger's, and what would break without
    ///     anyone noticing, is that asking for it succeeds and complains about nothing.
    /// </summary>
    [Fact]
    public void Answers_the_help_rather_than_refusing_it() {
        // Setup
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--help");

        // Verify
        Assert.Equal(0, exit);
        Assert.Empty(console.Errors);
    }

    [Fact]
    public void Refuses_a_theme_nobody_carries() {
        // Setup
        FakeConsole console = new() { IsInputRedirected = true };

        // Exercise
        int exit = Run(console, "--theme", "nonexistent");

        // Verify
        Assert.Equal(SluggerRunner.Refused, exit);
        Assert.Contains(console.Errors, line => line.Contains("nonexistent", StringComparison.Ordinal));
    }

    [Fact]
    public void Registers_a_theme_file_into_the_theme_directory() {
        // Setup
        string path = Path.Combine(_directory, "porno.json");
        File.WriteAllText(path, ValidTheme());
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--register", path, "--theme-dir", Path.Combine(_directory, "themes"));

        // Verify
        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(_directory, "themes", "porno.json")));
    }

    /// <summary>Allowed, because a custom file is meant to be able to shadow a built-in theme - but never silent.</summary>
    [Fact]
    public void Warns_when_a_registered_theme_shadows_a_built_in_one() {
        // Setup
        string path = Path.Combine(_directory, "docker.json");
        File.WriteAllText(path, ValidTheme());
        FakeConsole console = new();

        // Exercise
        Run(console, "--register", path, "--theme-dir", Path.Combine(_directory, "themes"));

        // Verify
        Assert.Contains(console.Errors, line => line.Contains("shadows", StringComparison.Ordinal));
    }

    [Fact]
    public void Refuses_to_unregister_a_theme_that_is_built_in() {
        // Setup
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--unregister", "docker", "--theme-dir", _directory);

        // Verify
        Assert.Equal(SluggerRunner.Refused, exit);
        Assert.Contains(console.Errors, line => line.Contains("embedded", StringComparison.Ordinal));
    }

    [Fact]
    public void Theme_info_prints_the_declared_fields_of_a_registered_theme() {
        // Setup
        string path = Path.Combine(_directory, "cuisine.json");
        File.WriteAllText(
            path,
            $$"""
              {
                "adjectives": { "common": [{{Words()}}] }, "nouns": [{{Nouns()}}],
                "meta": { "title": "Cuisine", "author": "Sylvain" }
              }
              """);
        string themeDirectory = Path.Combine(_directory, "themes");
        Run(new FakeConsole(), "--register", path, "--theme-dir", themeDirectory);
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--theme-info", "cuisine", "--theme-dir", themeDirectory);

        // Verify
        Assert.Equal(0, exit);
        Assert.Contains(console.Output, line => line.Contains("cuisine", StringComparison.Ordinal));
        Assert.Contains(console.Output, line => line.Contains("title: Cuisine", StringComparison.Ordinal));
        Assert.Contains(console.Output, line => line.Contains("author: Sylvain", StringComparison.Ordinal));
        Assert.DoesNotContain(console.Output, line => line.Contains("version:", StringComparison.Ordinal));
    }

    [Fact]
    public void Theme_info_says_plainly_when_a_theme_declares_no_metadata() {
        // Setup - the built-in themes now carry their own meta, so this one declares none on purpose.
        string path = Path.Combine(_directory, "porno.json");
        File.WriteAllText(path, ValidTheme());
        string themeDirectory = Path.Combine(_directory, "themes");
        Run(new FakeConsole(), "--register", path, "--theme-dir", themeDirectory);
        FakeConsole console = new();

        // Exercise
        Run(console, "--theme-info", "porno", "--theme-dir", themeDirectory);

        // Verify
        Assert.Contains(console.Output, line => line.Contains("no metadata declared", StringComparison.Ordinal));
    }

    /// <summary>
    ///     The reason the command reads the shape only: a theme refused for its pools still has a
    ///     "meta" block worth reading, and --theme-info is not the command that judges the rest.
    /// </summary>
    [Fact]
    public void Theme_info_shows_meta_even_when_the_theme_would_be_refused_on_its_pools() {
        // Setup - one noun and one adjective, far under every floor.
        string path = Path.Combine(_directory, "maigre.json");
        File.WriteAllText(
            path,
            """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }], "meta": { "title": "Maigre" } }""");
        string themeDirectory = Path.Combine(_directory, "themes");
        Directory.CreateDirectory(themeDirectory);
        File.Copy(path, Path.Combine(themeDirectory, "maigre.json"));
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--theme-info", "maigre", "--theme-dir", themeDirectory);

        // Verify
        Assert.Equal(0, exit);
        Assert.Contains(console.Output, line => line.Contains("title: Maigre", StringComparison.Ordinal));
    }

    [Fact]
    public void Theme_info_refuses_a_theme_nobody_carries() {
        // Exercise
        FakeConsole console = new();
        int         exit    = Run(console, "--theme-info", "nonexistent");

        // Verify
        Assert.Equal(SluggerRunner.Refused, exit);
        Assert.Contains(console.Errors, line => line.Contains("nonexistent", StringComparison.Ordinal));
    }

    /// <summary>
    ///     The whole point of --init: what it saves has to steer a later run that says nothing.
    /// </summary>
    [Fact]
    public void Saves_defaults_that_a_later_run_picks_up() {
        // Setup
        FakeConsole saving = new();
        Run(saving, "--init", "--theme", "docker", "--count", "3");

        // Exercise
        FakeConsole drawing = new() { IsInputRedirected = true };
        Run(drawing, "--oneshot");

        // Verify - three slugs from docker, neither of which this command line mentioned.
        Assert.Equal(3, drawing.Output.Count);
        Assert.All(drawing.Output, slug => Assert.Contains('_', slug));
    }

    /// <summary>
    ///     The report lands beside the theme it measured, not in the theme directory: the file
    ///     analysed may never be registered at all.
    /// </summary>
    [Fact]
    public void Analyze_writes_the_report_next_to_the_theme_it_measured() {
        // Setup
        string theme = Path.Combine(_directory, "cuisine.json");
        File.WriteAllText(theme, """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }] }""");
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--analyze", theme);

        // Verify - exit zero: the analysis succeeded, whatever it found.
        Assert.Equal(0, exit);
        string report = Path.Combine(_directory, "cuisine-analysis.md");
        Assert.True(File.Exists(report), $"expected a report at {report}");
        Assert.Contains(console.Output, line => line.Contains("cuisine-analysis.md", StringComparison.Ordinal));
    }

    /// <summary>
    ///     A theme is analysed because something about it is in doubt, and the answer should not
    ///     cost opening a document: the verdict is on the terminal, the measurements are in the file.
    /// </summary>
    [Fact]
    public void Analyze_says_on_the_terminal_that_the_theme_would_be_refused() {
        // Setup - one noun and one adjective, far under every floor.
        string theme = Path.Combine(_directory, "maigre.json");
        File.WriteAllText(theme, """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }] }""");
        FakeConsole console = new();

        // Exercise
        Run(console, "--analyze", theme);

        // Verify
        Assert.Contains(console.Output, line => line.Contains("would be refused", StringComparison.Ordinal));
        Assert.Contains(console.Output, line => line.Contains("at least 100", StringComparison.Ordinal));
    }

    /// <summary>
    ///     The reason the command exists. A theme is analysed precisely when it does not pass, so a
    ///     report that measured only what loads would be useless at the one moment it is wanted.
    /// </summary>
    [Fact]
    public void Analyze_measures_a_theme_that_would_be_refused() {
        // Setup - one noun, far under every floor.
        string theme = Path.Combine(_directory, "maigre.json");
        File.WriteAllText(theme, """{ "adjectives": { "common": ["keen"] }, "nouns": [{ "value": "moon" }] }""");

        // Exercise
        Run(new FakeConsole(), "--analyze", theme);

        // Verify - the refusals and the numbers, in the same file.
        string report = File.ReadAllText(Path.Combine(_directory, "maigre-analysis.md"));
        Assert.Contains("**Refused**", report, StringComparison.Ordinal);
        Assert.Contains("## Margins", report, StringComparison.Ordinal);
        Assert.Contains("`moon`", report, StringComparison.Ordinal);
    }

    private int Run(FakeConsole console, params string[] arguments) {
        IConfigStore    config      = new XdgConfigStore(Path.Combine(_directory, "config.json"));
        IThemeDirectory directories = new ThemeDirectory();

        SluggerRunner runner = new(
            console,
            config,
            new GenerateSlugsUseCase(directories, config, _clipboard),
            new ListThemesUseCase(directories, config),
            new RegisterThemeUseCase(directories, config),
            new UnregisterThemeUseCase(directories, config),
            new SaveDefaultsUseCase(config),
            new AnalyzeThemeUseCase(directories, config),
            new ThemeInfoUseCase(directories, config),
            directories);

        // Spectre draws its own answers - the help above all - and a test wants the exit code
        // rather than the page, so they go nowhere.
        return SluggerApp.Run(runner, console, SluggerApp.Terminal(TextWriter.Null, true), arguments);
    }

}