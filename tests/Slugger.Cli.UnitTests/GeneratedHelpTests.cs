#region Usings declarations

using System.Globalization;
using System.Reflection;

using Slugger.Cli.CommandLine;

using Spectre.Console;
using Spectre.Console.Cli;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The half of DEC0019 that is a promise rather than a mechanism: the help is drawn from the
///     declaration, so it cannot describe an option that was removed nor miss one that was added.
///     Spectre lays the text out; what these pin is that every option reaches it, and that it
///     survives a pipe.
/// </summary>
public sealed class GeneratedHelpTests {

    #region Static members

    /// <summary>What the application prints for <c>--help</c>, drawn into a writer rather than a window.</summary>
    private static string Help() {
        return Answer("--help");
    }

    /// <summary>
    ///     What the real application draws for a flag it answers itself, over a command that never
    ///     runs. Nothing is asserted about the drawing here beyond its succeeding: what it says is
    ///     each case's business.
    /// </summary>
    /// <remarks>
    ///     The terminal is the real one, and then its colour is taken away. Spectre enriches a
    ///     profile from the environment, and on a GitHub runner it turns ANSI back on because that
    ///     log viewer renders it - so a word matched here would be the word wrapped in escape codes,
    ///     on the runner and nowhere else. What these cases are about is what the help says, not how
    ///     it is painted.
    /// </remarks>
    /// <param name="flag">The flag Spectre answers.</param>
    private static string Answer(string flag) {
        StringWriter output   = new();
        IAnsiConsole terminal = SluggerApp.Terminal(output, true);
        terminal.Profile.Capabilities.Ansi        = false;
        terminal.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;

        Assert.Equal(0, SluggerApp.Build<Nothing>(new PortRegistrar(), terminal).Run([flag]));

        return output.ToString();
    }

    /// <summary>The options section alone, which is the list an option has to appear in.</summary>
    /// <param name="help">The whole page.</param>
    private static string Options(string help) {
        int heading = help.IndexOf("OPTIONS", StringComparison.Ordinal);
        Assert.True(heading >= 0, "the help carries no options section at all");

        return help[heading..];
    }

    private static HashSet<string> Words(string text) {
        return [.. text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(word => word.TrimEnd(','))];
    }

    #endregion

    /// <summary>
    ///     The regression that arrived silently and would again: with no window to measure, Spectre
    ///     lays out for the width it was told, and what it assumes reduced the whole help to an
    ///     ellipsis - three bytes, exit code 0, nothing to suggest anything went wrong (measured).
    /// </summary>
    [Fact]
    public void Draws_the_whole_help_rather_than_an_ellipsis_when_the_output_is_redirected() {
        // Exercise
        string help = Help();

        // Verify
        Assert.Contains("USAGE", help, StringComparison.Ordinal);
        Assert.Contains("EXAMPLES", help, StringComparison.Ordinal);
        Assert.Contains("OPTIONS", help, StringComparison.Ordinal);
    }

    /// <summary>
    ///     All four, not the first one: an example is the shortest answer to "how do I use this",
    ///     and three of them could be blanked without a case noticing (measured, KillMutants).
    /// </summary>
    [Fact]
    public void Shows_every_example_it_was_given() {
        // Setup
        string[] examples = [
            "--theme docker --count 3",
            "--theme heroku --sep = --casing camel",
            "--analyze ./my-theme.json --max-length 63",
            "--register ./my-theme.json"
        ];

        // Exercise
        string help = Help();

        // Verify
        Assert.All(examples, example => Assert.Contains(example, help, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Read from the declaration rather than written down here, because a list written down here
    ///     is the second declaration DEC0019 removed.
    /// </summary>
    /// <remarks>
    ///     Matched against whole words of the options section, not against the whole page: "--theme"
    ///     occurs inside "--theme-dir" and again in every example, so a substring of the page would
    ///     stay green with the option itself missing from the list.
    /// </remarks>
    [Fact]
    public void Names_every_option_the_settings_declare() {
        // Setup
        IEnumerable<string> declared = typeof(SluggerSettings)
                                      .GetProperties()
                                      .Select(property => property.GetCustomAttribute<CommandOptionAttribute>())
                                      .OfType<CommandOptionAttribute>()
                                      .SelectMany(option => option.LongNames)
                                      .Select(name => "--" + name);

        // Exercise
        HashSet<string> listed = Words(Options(Help()));

        // Verify
        Assert.All(declared, option => Assert.Contains(option, listed));
    }

    /// <summary>
    ///     Spectre translates the frame of the help to the current culture, and everything inside it
    ///     is slugger's own prose, which is English. A machine set to French printed "UTILISATION"
    ///     over English descriptions until the culture was pinned (measured), and this suite would
    ///     have gone red on that machine and nowhere else.
    /// </summary>
    /// <remarks>
    ///     On a thread of its own, because the culture is process-global and this suite runs in
    ///     parallel. Setting it inside a test and restoring it in a finally restores it on whichever
    ///     thread the continuation happened to resume on, and leaves any other one it touched
    ///     speaking French - which showed up as a case elsewhere failing now and then, under a host
    ///     that schedules collections differently from "dotnet test". A thread's culture dies with
    ///     the thread, so nothing outside this one ever sees it.
    /// </remarks>
    [Fact]
    public void Prints_one_language_whatever_the_machine_is_set_to() {
        // Setup
        string help = string.Empty;
        Thread french = new(() => {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            help                         = Help();
        });

        // Exercise
        french.Start();
        french.Join();

        // Verify
        Assert.Contains("USAGE", help, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The informational version rather than the assembly one, which is what carries the
    ///     prerelease tag and the commit a package was built from.
    /// </summary>
    [Fact]
    public void Answers_the_version_it_was_built_from() {
        // Setup
        string built = typeof(SluggerApp).Assembly
                                         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                                         .InformationalVersion;

        // Exercise
        string printed = Answer("--version");

        // Verify
        Assert.Contains(built, printed, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Wide enough for the options table and the width a terminal has when nothing says
    ///     otherwise, so a pipe and a CI log read like the screen does.
    /// </summary>
    [Fact]
    public void Lays_out_for_eighty_columns_when_there_is_no_window_to_measure() {
        // Setup
        StringWriter output = new();

        // Verify
        Assert.Equal(80, SluggerApp.Terminal(output, true).Profile.Width);
    }

    #region Nested types

    /// <summary>Never runs: asking for the help is answered before any command is reached.</summary>
    private sealed class Nothing : Command<SluggerSettings> {

        protected override int Execute(CommandContext context, SluggerSettings settings, CancellationToken cancellationToken) {
            return 0;
        }

    }

    #endregion

}