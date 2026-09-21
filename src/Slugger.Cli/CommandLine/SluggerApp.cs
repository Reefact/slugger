using Slugger.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// Builds the application Spectre runs. Kept apart from <see cref="Program"/> so that a test can
/// build the same one over fakes and drive it through the real command line - which is the only
/// way to cover what Spectre itself decides, from an unknown flag to <c>--help</c>.
/// </summary>
internal static class SluggerApp
{
    /// <summary>
    /// Builds and runs it, reporting what Spectre itself refused in slugger's own shape - an exit
    /// code of one and the reason on standard error, rather than Spectre's exception page.
    /// </summary>
    /// <param name="runner">What does the work once the line has been understood.</param>
    /// <param name="console">Where a refusal goes.</param>
    /// <param name="terminal">Where Spectre draws what it answers itself, the help above all.</param>
    /// <param name="arguments">The command line as the runtime handed it over.</param>
    internal static int Run(
        SluggerRunner runner,
        IConsole console,
        IAnsiConsole terminal,
        IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(arguments);

        try
        {
            return Build(runner, console, terminal).Run(arguments);
        }
        catch (CommandAppException refused)
        {
            console.WriteError(ReportRenderer.Draw(
                CliErrors.Rejected([CliErrors.NotUnderstood(refused.Message)])));

            return SluggerRunner.Refused;
        }
    }

    /// <param name="runner">What does the work once the line has been understood.</param>
    /// <param name="console">Where a refusal goes.</param>
    /// <param name="terminal">Where Spectre draws what it answers itself.</param>
    internal static CommandApp<SluggerCommand> Build(SluggerRunner runner, IConsole console, IAnsiConsole terminal) =>
        Build<SluggerCommand>(new PortRegistrar().With(runner).With(console), terminal);

    /// <summary>
    /// The same application over another command, which is how a test reaches the command line
    /// itself: the configuration is shared rather than reproduced, so what a test parses is
    /// parsed under the rules the real application runs by - strict parsing above all.
    /// </summary>
    /// <typeparam name="TCommand">What runs once the line has been bound.</typeparam>
    /// <param name="ports">Everything that command's constructor names.</param>
    internal static CommandApp<TCommand> Build<TCommand>(PortRegistrar ports)
        where TCommand : class, ICommand =>
        Build<TCommand>(ports, Terminal(Console.Out, Console.IsOutputRedirected));

    /// <param name="ports">Everything the command's constructor names.</param>
    /// <param name="terminal">Where Spectre draws, which for a test is a writer it can read.</param>
    internal static CommandApp<TCommand> Build<TCommand>(PortRegistrar ports, IAnsiConsole terminal)
        where TCommand : class, ICommand
    {
        CommandApp<TCommand> app = new(ports);

        app.Configure(config =>
        {
            config.ConfigureConsole(terminal);
            config.SetApplicationName("slugger");

            // Spectre ignores an option it does not know unless told not to, so "--themme docker"
            // would silently generate from the default theme. A typo is a refusal here (measured).
            config.UseStrictParsing();
            config.UseAssemblyInformationalVersion();

            // Spectre's own exception page is for a bug in a command; slugger reports through
            // FirstClassErrors and an exit code, so what escapes is propagated rather than drawn.
            config.PropagateExceptions();

            config.AddExample("--theme", "docker", "--count", "3");
            config.AddExample("--theme", "heroku", "--sep", "=", "--casing", "camel");
            config.AddExample("--analyze", "./my-theme.json", "--max-length", "63");
            config.AddExample("--register", "./my-theme.json");
        });

        return app;
    }

    /// <summary>
    /// The terminal Spectre draws on. A width has to be given when the output is redirected:
    /// there is no window to measure then, and Spectre lays out for the width it was told, which
    /// without this is small enough to reduce the whole help to an ellipsis (measured).
    /// </summary>
    /// <param name="output">Where the drawing goes.</param>
    /// <param name="redirected">Whether that is a pipe or a file rather than a window.</param>
    internal static IAnsiConsole Terminal(TextWriter output, bool redirected)
    {
        IAnsiConsole terminal = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(output),
        });

        if (redirected)
        {
            // Wide enough for the options table, narrow enough to stay readable in a pipe or a
            // CI log, and the width every terminal has had since the VT100.
            terminal.Profile.Width = 80;
        }

        return terminal;
    }
}
