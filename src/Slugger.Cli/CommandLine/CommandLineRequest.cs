using Slugger.Application.Options;

namespace Slugger.Cli.CommandLine;

/// <summary>A command line that parsed: what it asked for, and with which options.</summary>
/// <param name="Command">What to do.</param>
/// <param name="Options">Everything the line stated, with null meaning "said nothing about it".</param>
/// <param name="Argument">The path or name a command needs, null for the ones that need none.</param>
internal sealed record CommandLineRequest(CliCommand Command, SluggerOptions Options, string? Argument);
