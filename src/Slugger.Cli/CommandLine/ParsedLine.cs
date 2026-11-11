namespace Slugger.Cli.CommandLine;

/// <summary>A command line, bound onto the settings it declares.</summary>
/// <param name="Intent">What it asks for.</param>
/// <param name="Settings">The text bound to each option it gave.</param>
/// <param name="Leftovers">What could not be placed.</param>
internal sealed record ParsedLine(LineIntent Intent, SluggerSettings Settings, Leftovers Leftovers);