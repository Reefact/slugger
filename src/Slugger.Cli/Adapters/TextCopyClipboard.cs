#region Usings declarations

using TextCopy;

using IClipboard = Slugger.Application.Abstractions.IClipboard;

#endregion

namespace Slugger.Cli.Adapters;

/// <summary>
///     The only adapter that needs a NuGet package, and the reason it sits in the CLI rather
///     than in Slugger.Infrastructure: the BCL has no cross-platform clipboard, and copying is
///     meaningless outside a command line.
/// </summary>
/// <remarks>
///     TextCopy ships an <c>IClipboard</c> of its own, so the port is aliased above. The clash
///     is the adapter's to absorb - renaming the port would let a third-party package dictate
///     vocabulary to a layer that does not even reference it.
/// </remarks>
internal sealed class TextCopyClipboard : IClipboard {

    /// <summary>The label TextCopy puts in front of what the tool it ran wrote on standard error.</summary>
    private const string ToolComplaint = "Error:";

    #region Static members

    /// <summary>
    ///     What went wrong, cut to the few words a warning line has room for. TextCopy reports a failed
    ///     copy as the whole shell command it ran followed by both of that command's streams, so what
    ///     is kept is the tool's own complaint - and the commonest one, a Linux machine without xsel,
    ///     is said in plain words.
    /// </summary>
    /// <param name="failure">What TextCopy, or the platform under it, raised.</param>
    internal static string Reason(Exception failure) {
        ArgumentNullException.ThrowIfNull(failure);

        string[] lines = failure.Message.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (Array.Exists(lines, line => line.EndsWith("xsel: command not found", StringComparison.Ordinal))) { return "xsel is not installed"; }

        string? complaint = Array.Find(lines, line => line.StartsWith(ToolComplaint, StringComparison.Ordinal)
                                                   && line.Length > ToolComplaint.Length);
        if (complaint is not null) { return complaint[ToolComplaint.Length..].Trim(); }

        return lines.FirstOrDefault() ?? failure.GetType().Name;
    }

    #endregion

    /// <inheritdoc />
    /// <remarks>
    ///     Every exception is caught, because TextCopy raises <see cref="Exception" /> itself when the
    ///     tool it runs fails: nothing narrower would catch the very case this exists for.
    /// </remarks>
    public string? Copy(string text) {
        try {
            ClipboardService.SetText(text);
        } catch (Exception failure) {
            return Reason(failure);
        }

        return null;
    }

}