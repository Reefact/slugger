namespace Slugger.Cli.CommandLine;

/// <summary>What the parser read and could not place, each kind a complaint of its own.</summary>
/// <param name="Unattached">Words attached to no option: a bare word, and everything after <c>--</c>.</param>
/// <param name="UnknownOptions">Options slugger does not have, each named once, as typed.</param>
/// <param name="Unreadable">Why an option could not be read at all, an option missing its value above all.</param>
internal sealed record Leftovers(
    IReadOnlyList<string> Unattached,
    IReadOnlyList<string> UnknownOptions,
    IReadOnlyList<string> Unreadable) {

    #region Static members

    /// <summary>Nothing left over.</summary>
    internal static Leftovers None { get; } = new([], [], []);

    #endregion

}