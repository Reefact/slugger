#region Usings declarations

using System.Globalization;

using Slugger.Cli.Rendering;

using Spectre.Console;
using Spectre.Console.Cli;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     Builds the application Spectre runs. Kept apart from <see cref="Program" /> so that a test can
///     build the same one over fakes and drive it through the real command line - which is the only
///     way to cover what Spectre itself decides, from an unknown flag to <c>--help</c>.
/// </summary>
internal static class SluggerApp {

    /// <summary>The token that ends the options: whatever follows it is an argument, left as typed.</summary>
    private const string EndOfOptions = "--";

    /// <summary>What a separator option is given to mean a dash.</summary>
    private const string LoneDash = "-";

    private const string Separator     = "--sep";
    private const string WordSeparator = "--word-sep";

    #region Static members

    /// <summary>
    ///     Builds and runs it, reporting what Spectre itself refused in slugger's own shape - an exit
    ///     code of one and the reason on standard error, rather than Spectre's exception page.
    /// </summary>
    /// <param name="runner">What does the work once the line has been understood.</param>
    /// <param name="console">Where a refusal goes.</param>
    /// <param name="terminal">Where Spectre draws what it answers itself, the help above all.</param>
    /// <param name="arguments">The command line as the runtime handed it over.</param>
    internal static int Run(SluggerRunner         runner,
                            IConsole              console,
                            IAnsiConsole          terminal,
                            IReadOnlyList<string> arguments) {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(arguments);

        try {
            return Build(runner, console, terminal).Run(Spelled(arguments));
        } catch (CommandAppException refused) {
            console.WriteError(ReportRenderer.Draw(
                                   CliErrors.Rejected([CliErrors.NotUnderstood(refused.Message)])));

            return SluggerRunner.Refused;
        }
    }

    /// <summary>
    ///     The two spellings Spectre's tokenizer cannot read, rewritten into ones it can before it sees
    ///     them. A lone <c>-</c> after <c>--sep</c> or <c>--word-sep</c> is read as an option with no
    ///     name, though a dash is the separator people reach for first, so it is attached the way
    ///     <c>--sep=-</c> already was. And <c>--word-sep=</c>, with nothing after the sign, is refused
    ///     for a missing value, though nothing is the very value meant: it becomes the empty value
    ///     <c>--word-sep ""</c> already gave.
    /// </summary>
    /// <remarks>
    ///     Kept to those two options, the only ones whose value is a character: every other one takes
    ///     a name, a path, a number or a word, and none of those is a lone dash. Nothing after a bare
    ///     <c>--</c> is touched, since that is where the options stop.
    /// </remarks>
    /// <param name="arguments">The command line as the runtime handed it over.</param>
    internal static IReadOnlyList<string> Spelled(IReadOnlyList<string> arguments) {
        ArgumentNullException.ThrowIfNull(arguments);

        List<string> spelled = [];
        int          read    = 0;
        while (read < arguments.Count && arguments[read] != EndOfOptions) {
            string argument = arguments[read];
            read++;

            if (argument == $"{WordSeparator}=") {
                spelled.AddRange([WordSeparator, string.Empty]);
                continue;
            }

            if (IsGivenALoneDash(argument, arguments, read)) {
                spelled.Add($"{argument}={LoneDash}");
                read++;
                continue;
            }

            spelled.Add(argument);
        }

        spelled.AddRange(arguments.Skip(read));

        return spelled;
    }

    /// <param name="runner">What does the work once the line has been understood.</param>
    /// <param name="console">Where a refusal goes.</param>
    /// <param name="terminal">Where Spectre draws what it answers itself.</param>
    internal static CommandApp<SluggerCommand> Build(SluggerRunner runner, IConsole console, IAnsiConsole terminal) {
        return Build<SluggerCommand>(new PortRegistrar().With(runner).With(console), terminal);
    }

    /// <summary>
    ///     The same application over another command, which is how a test reaches the command line
    ///     itself: the configuration is shared rather than reproduced, so what a test parses is
    ///     parsed under the rules the real application runs by, down to how it tokenizes.
    /// </summary>
    /// <typeparam name="TCommand">What runs once the line has been bound.</typeparam>
    /// <param name="ports">Everything that command's constructor names.</param>
    internal static CommandApp<TCommand> Build<TCommand>(PortRegistrar ports)
        where TCommand : class, ICommand {
        return Build<TCommand>(ports, Terminal(Console.Out, Console.IsOutputRedirected));
    }

    /// <param name="ports">Everything the command's constructor names.</param>
    /// <param name="terminal">Where Spectre draws, which for a test is a writer it can read.</param>
    internal static CommandApp<TCommand> Build<TCommand>(PortRegistrar ports, IAnsiConsole terminal)
        where TCommand : class, ICommand {
        CommandApp<TCommand> app = new(ports);

        app.Configure(config => {
            config.ConfigureConsole(terminal);
            config.SetApplicationName("slugger");

            // Spectre translates the frame of the help - "USAGE", "EXAMPLES" - to the current
            // culture, and every word inside it is slugger's, which is written in English only.
            // Left alone, a French machine prints "UTILISATION" over English descriptions
            // (measured). One language throughout, and it is the one the descriptions are in.
            config.Settings.Culture = CultureInfo.InvariantCulture;

            // Deliberately no UseStrictParsing() here: strict throws on the first token it
            // cannot place, and the rest of the line is then never read. Lenient binds all of it
            // and leaves what it could not place in the remaining arguments, which
            // CommandLineReader refuses alongside every other complaint - which is what DEC0006
            // asks for. Ignoring them is what must not happen, and the reader is what stops it.

            config.UseAssemblyInformationalVersion();

            // Spectre's own exception page is for a bug in a command; slugger reports through
            // FirstClassErrors and an exit code, so what escapes is propagated rather than drawn.
            config.PropagateExceptions();

            config.AddExample("--theme", "docker", "--count", "3");
            config.AddExample("--ascii", "--max-length", "63", "--oneshot");
            config.AddExample("--analyze", "./my-theme.json", "--max-length", "63");
            config.AddExample("--register", "./my-theme.json");
        });

        return app;
    }

    /// <summary>
    ///     The terminal Spectre draws on. A width has to be given when the output is redirected:
    ///     there is no window to measure then, and Spectre lays out for the width it was told, which
    ///     without this is small enough to reduce the whole help to an ellipsis (measured).
    /// </summary>
    /// <param name="output">Where the drawing goes.</param>
    /// <param name="redirected">Whether that is a pipe or a file rather than a window.</param>
    internal static IAnsiConsole Terminal(TextWriter output, bool redirected) {
        IAnsiConsole terminal = Drawing(output);

        if (redirected) {
            // Wide enough for the options table, narrow enough to stay readable in a pipe or a
            // CI log, and the width every terminal has had since the VT100.
            terminal.Profile.Width = 80;
        }

        return terminal;
    }

    /// <summary>
    ///     The terminal refusals and warnings are drawn on. Redirected - a CI log, <c>2&gt;err.txt</c> -
    ///     it lays out for a width no line reaches, so nothing is wrapped: whatever shows the log
    ///     wraps a long line itself, and a break written into the text cuts a sentence in two where
    ///     grep no longer finds it. The help keeps its eighty columns; it is drawn on the other one.
    /// </summary>
    /// <param name="error">Where the drawing goes.</param>
    /// <param name="redirected">Whether that is a pipe or a file rather than a window.</param>
    internal static IAnsiConsole ErrorTerminal(TextWriter error, bool redirected) {
        IAnsiConsole terminal = Drawing(error);

        if (redirected) {
            terminal.Profile.Width = int.MaxValue;
        }

        return terminal;
    }

    /// <summary>Whether a separator option is followed by a lone dash, which is then its value.</summary>
    /// <param name="option">The token just read.</param>
    /// <param name="arguments">The whole command line.</param>
    /// <param name="next">Where the token after it sits.</param>
    private static bool IsGivenALoneDash(string option, IReadOnlyList<string> arguments, int next) {
        if (option is not (Separator or WordSeparator)) { return false; }
        if (next >= arguments.Count) { return false; }

        return arguments[next] == LoneDash;
    }

    private static IAnsiConsole Drawing(TextWriter output) {
        return AnsiConsole.Create(new AnsiConsoleSettings {
            Out = new AnsiConsoleOutput(output)
        });
    }

    #endregion

}