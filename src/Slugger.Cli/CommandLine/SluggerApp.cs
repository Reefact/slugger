#region Usings declarations

using Slugger.Cli.Rendering;
using Slugger.Domain.Errors;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     Reads a command line and hands it to the runner, or answers it alone: the help, the version,
///     and every reason the line was refused. Kept apart from <see cref="Program" /> so that a test
///     can drive the real command line over fakes.
/// </summary>
internal static class SluggerApp {

    #region Static members

    /// <summary>Reads the line and runs it, or answers it.</summary>
    /// <param name="runner">What does the work once the line has been understood.</param>
    /// <param name="console">Where the help, the version and a refusal go.</param>
    /// <param name="arguments">The command line as the runtime handed it over.</param>
    internal static int Run(SluggerRunner runner, IConsole console, IReadOnlyList<string> arguments) {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(arguments);

        ParsedLine line = CommandLineParser.Parse(arguments);
        if (line.Intent == LineIntent.Help) {
            console.Write(HelpRenderer.Lines());

            return 0;
        }

        if (line.Intent == LineIntent.Version) {
            console.WriteLine(HelpRenderer.Version());

            return 0;
        }

        Outcome<CommandLineRequest> read = CommandLineReader.Read(line.Settings, line.Leftovers);
        if (read.Error is not { } refused) { return runner.Run(read.GetResultOrThrow()); }

        console.WriteError(ReportRenderer.Draw(refused));

        return SluggerRunner.Refused;
    }

    #endregion

}