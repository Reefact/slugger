using FirstClassErrors;
using Slugger.Cli.Rendering;
using Spectre.Console.Cli;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// The one command. slugger's shape is flat - twenty-one options and no verbs - so Spectre is
/// used for what it gives here and not for what it is usually reached for: binding, and a
/// <c>--help</c> generated from the same declaration (DEC0019).
/// </summary>
/// <param name="runner">What does the work once the line has been understood.</param>
/// <param name="console">Where a refusal goes.</param>
internal sealed class SluggerCommand(SluggerRunner runner, IConsole console) : Command<SluggerSettings>
{
    /// <inheritdoc />
    protected override int Execute(CommandContext context, SluggerSettings settings, CancellationToken cancellationToken)
    {
        Outcome<CommandLineRequest> read = CommandLineReader.Read(settings);
        if (read.Error is not { } refused)
        {
            return runner.Run(read.GetResultOrThrow());
        }

        foreach (string line in ReportRenderer.Render(refused))
        {
            console.WriteError(line);
        }

        return SluggerRunner.Refused;
    }
}
