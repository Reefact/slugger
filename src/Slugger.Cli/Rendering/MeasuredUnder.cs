namespace Slugger.Cli.Rendering;

/// <summary>
///     What an analysis report was measured under, so that the report can say so: one shared with
///     someone else, or reread a week later, could not tell which segment mode, limit or style it
///     measured.
/// </summary>
/// <param name="Version">The slugger that measured it.</param>
/// <param name="CommandLine">The options on the command line that change what is measured, as flags; empty when there are none.</param>
/// <param name="SavedDefaults">The saved defaults that change what is measured, as flags; empty when there are none.</param>
internal sealed record MeasuredUnder(string Version, string CommandLine, string SavedDefaults);
