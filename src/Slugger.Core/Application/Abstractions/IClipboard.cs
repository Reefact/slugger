namespace Slugger.Application.Abstractions;

/// <summary>
/// The <c>--clipboard</c> side effect. The port is declared here, but the only adapter lives
/// in Slugger.Cli: TextCopy is the CLI's single external dependency, and copying has no
/// meaning outside a command line.
/// </summary>
public interface IClipboard
{
    /// <summary>Replaces the clipboard contents.</summary>
    /// <param name="text">The slug to copy.</param>
    void Copy(string text);
}
