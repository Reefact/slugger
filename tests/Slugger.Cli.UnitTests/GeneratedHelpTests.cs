using System.Reflection;
using Slugger.Cli.CommandLine;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Slugger.Cli.UnitTests;

/// <summary>
/// The half of DEC0019 that is a promise rather than a mechanism: the help is drawn from the
/// declaration, so it cannot describe an option that was removed nor miss one that was added.
/// Spectre lays the text out; what these pin is that every option reaches it, and that it
/// survives a pipe.
/// </summary>
public sealed class GeneratedHelpTests
{
    /// <summary>
    /// The regression that arrived silently and would again: with no window to measure, Spectre
    /// lays out for the width it was told, and what it assumes reduced the whole help to an
    /// ellipsis - three bytes, exit code 0, nothing to suggest anything went wrong (measured).
    /// </summary>
    [Fact]
    public void Draws_the_whole_help_rather_than_an_ellipsis_when_the_output_is_redirected()
    {
        // Exercise
        string help = Help();

        // Verify
        Assert.Contains("USAGE", help, StringComparison.Ordinal);
        Assert.Contains("EXAMPLES", help, StringComparison.Ordinal);
        Assert.Contains("--theme docker --count 3", help, StringComparison.Ordinal);
    }

    /// <summary>
    /// Read from the declaration rather than written down here, because a list written down here
    /// is the second declaration DEC0019 removed.
    /// </summary>
    [Fact]
    public void Names_every_option_the_settings_declare()
    {
        // Setup
        string help = Help();

        // Exercise
        IEnumerable<string> declared = typeof(SluggerSettings)
            .GetProperties()
            .Select(property => property.GetCustomAttribute<CommandOptionAttribute>())
            .OfType<CommandOptionAttribute>()
            .SelectMany(option => option.LongNames)
            .Select(name => "--" + name);

        // Verify
        Assert.All(declared, option => Assert.Contains(option, help, StringComparison.Ordinal));
    }

    /// <summary>
    /// Wide enough for the options table and the width a terminal has when nothing says
    /// otherwise, so a pipe and a CI log read like the screen does.
    /// </summary>
    [Fact]
    public void Lays_out_for_eighty_columns_when_there_is_no_window_to_measure()
    {
        // Setup
        StringWriter output = new();

        // Verify
        Assert.Equal(80, SluggerApp.Terminal(output, redirected: true).Profile.Width);
    }

    /// <summary>What the application prints for <c>--help</c>, drawn into a writer rather than a window.</summary>
    private static string Help()
    {
        StringWriter output = new();
        CommandApp<Nothing> app = SluggerApp.Build<Nothing>(
            new PortRegistrar(), SluggerApp.Terminal(output, redirected: true));

        Assert.Equal(0, app.Run(["--help"]));

        return output.ToString();
    }

    /// <summary>Never runs: asking for the help is answered before any command is reached.</summary>
    private sealed class Nothing : Command<SluggerSettings>
    {
        protected override int Execute(CommandContext context, SluggerSettings settings, CancellationToken cancellationToken) => 0;
    }
}
