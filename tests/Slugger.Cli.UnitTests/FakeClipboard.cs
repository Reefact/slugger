#region Usings declarations

using Slugger.Application.Abstractions;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     A clipboard that remembers the last thing copied to it - or, given a reason, one that cannot
///     be reached and answers every copy with it, as a Linux machine without xsel does.
/// </summary>
/// <param name="unreachable">Why every copy fails, or null for a clipboard that takes them.</param>
internal sealed class FakeClipboard(string? unreachable = null) : IClipboard {

    internal string? LastCopied { get; private set; }

    public string? Copy(string text) {
        if (unreachable is not null) { return unreachable; }

        LastCopied = text;

        return null;
    }

}