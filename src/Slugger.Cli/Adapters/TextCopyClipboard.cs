using TextCopy;
using IClipboard = Slugger.Application.Abstractions.IClipboard;

namespace Slugger.Cli.Adapters;

/// <summary>
/// The only adapter that needs a NuGet package, and the reason it sits in the CLI rather
/// than in Slugger.Infrastructure: the BCL has no cross-platform clipboard, and copying is
/// meaningless outside a command line.
/// </summary>
/// <remarks>
/// TextCopy ships an <c>IClipboard</c> of its own, so the port is aliased above. The clash
/// is the adapter's to absorb - renaming the port would let a third-party package dictate
/// vocabulary to a layer that does not even reference it.
/// </remarks>
internal sealed class TextCopyClipboard : IClipboard
{
    /// <inheritdoc />
    public void Copy(string text) => ClipboardService.SetText(text);
}
