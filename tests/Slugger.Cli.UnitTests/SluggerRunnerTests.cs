using Slugger.Application.Abstractions;
using Slugger.Application.UseCases;
using Slugger.Infrastructure.Configuration;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger.Cli.UnitTests;

/// <summary>
/// The runner over the real stack - real catalogs, a real config file in a directory of its own -
/// with only the terminal and the clipboard doubled. What is worth testing here is what the CLI
/// decides, and it decides it against the engine rather than against a mock of it.
/// </summary>
public sealed class SluggerRunnerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"slugger-cli-{Guid.NewGuid():N}");
    private readonly FakeClipboard _clipboard = new();

    public SluggerRunnerTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory)) { Directory.Delete(_directory, recursive: true); }
    }

    [Fact]
    public void Draws_once_and_stops_when_standard_input_is_not_a_terminal()
    {
        // Setup - a pipe, a script or a CI runner: a ReadLine nobody will answer is a hang.
        FakeConsole console = new() { IsInputRedirected = true };

        // Exercise
        int exit = Run(console, "--theme", "docker");

        // Verify
        Assert.Equal(0, exit);
        Assert.Single(console.Output);
    }

    [Fact]
    public void Draws_once_and_stops_when_oneshot_was_asked_for()
    {
        // Setup - input is waiting, and --oneshot says not to read it.
        FakeConsole console = new("", "", "");

        // Exercise
        Run(console, "--theme", "docker", "--oneshot");

        // Verify
        Assert.Single(console.Output);
    }

    /// <summary>A round on start, then another on every Enter, until the input runs out.</summary>
    [Fact]
    public void Draws_again_on_every_line_until_there_are_no_more()
    {
        // Setup - two Enters after the opening round.
        FakeConsole console = new("", "");

        // Exercise
        Run(console, "--theme", "docker");

        // Verify
        Assert.Equal(3, console.Output.Count);
    }

    [Fact]
    public void Draws_as_many_slugs_a_round_as_count_asks_for()
    {
        // Setup
        FakeConsole console = new() { IsInputRedirected = true };

        // Exercise
        Run(console, "--theme", "docker", "--count", "4");

        // Verify
        Assert.Equal(4, console.Output.Count);
    }

    [Fact]
    public void Copies_the_last_slug_of_a_round_when_the_clipboard_was_asked_for()
    {
        // Setup
        FakeConsole console = new() { IsInputRedirected = true };

        // Exercise
        Run(console, "--theme", "docker", "--count", "3", "--clipboard");

        // Verify
        Assert.Equal(console.Output[^1], _clipboard.LastCopied);
    }

    [Fact]
    public void Lists_the_themes_in_scope()
    {
        // Setup
        FakeConsole console = new();

        // Exercise
        Run(console, "--list-themes");

        // Verify
        Assert.Equal(["docker", "heroku", "slugger"], console.Output);
    }

    [Fact]
    public void Refuses_a_command_line_it_cannot_read_and_says_why_on_standard_error()
    {
        // Setup
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--casing", "SHOUT", "--nope");

        // Verify - nothing on standard output, so a pipe downstream gets no rubbish.
        Assert.Equal(SluggerRunner.Refused, exit);
        Assert.Empty(console.Output);
        Assert.Contains(console.Errors, line => line.Contains("2 reasons", StringComparison.Ordinal));
    }

    [Fact]
    public void Refuses_a_theme_nobody_carries()
    {
        // Setup
        FakeConsole console = new() { IsInputRedirected = true };

        // Exercise
        int exit = Run(console, "--theme", "nonexistent");

        // Verify
        Assert.Equal(SluggerRunner.Refused, exit);
        Assert.Contains(console.Errors, line => line.Contains("nonexistent", StringComparison.Ordinal));
    }

    [Fact]
    public void Registers_a_theme_file_into_the_theme_directory()
    {
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
    public void Warns_when_a_registered_theme_shadows_a_built_in_one()
    {
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
    public void Refuses_to_unregister_a_theme_that_is_built_in()
    {
        // Setup
        FakeConsole console = new();

        // Exercise
        int exit = Run(console, "--unregister", "docker", "--theme-dir", _directory);

        // Verify
        Assert.Equal(SluggerRunner.Refused, exit);
        Assert.Contains(console.Errors, line => line.Contains("embedded", StringComparison.Ordinal));
    }

    /// <summary>
    /// The whole point of --init: what it saves has to steer a later run that says nothing.
    /// </summary>
    [Fact]
    public void Saves_defaults_that_a_later_run_picks_up()
    {
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

    private int Run(FakeConsole console, params string[] arguments)
    {
        IConfigStore config = new XdgConfigStore(Path.Combine(_directory, "config.json"));
        IThemeDirectory directories = new ThemeDirectory();

        SluggerRunner runner = new(
            console,
            config,
            new GenerateSlugsUseCase(directories, config, _clipboard),
            new ListThemesUseCase(directories, config),
            new RegisterThemeUseCase(directories, config),
            new UnregisterThemeUseCase(directories, config),
            new SaveDefaultsUseCase(config));

        return runner.Run(arguments);
    }

    private static string ValidTheme()
    {
        string words = string.Join(", ", Enumerable.Range(0, 120).Select(index => $"\"adj{index}\""));
        string nouns = string.Join(", ", Enumerable.Range(0, 120).Select(index => $"{{ \"value\": \"noun{index}\" }}"));

        return $$"""{ "adjectives": { "common": [{{words}}] }, "nouns": [{{nouns}}] }""";
    }
}

/// <summary>A clipboard that remembers the last thing copied to it.</summary>
internal sealed class FakeClipboard : IClipboard
{
    internal string? LastCopied { get; private set; }

    public void Copy(string text) => LastCopied = text;
}
