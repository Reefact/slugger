namespace Slugger.Cli.CommandLine;

/// <summary>What a command line asks of slugger before any of its options is read.</summary>
internal enum LineIntent {

    /// <summary>Run with the options bound.</summary>
    Run,

    /// <summary><c>-h</c> or <c>--help</c>, anywhere on the line: the help, whatever else is there.</summary>
    Help,

    /// <summary><c>-v</c> or <c>--version</c>, anywhere on the line.</summary>
    Version

}