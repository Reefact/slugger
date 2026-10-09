namespace Slugger.Application.Abstractions;

/// <summary>
///     The <c>--clipboard</c> side effect. The port is declared here, but the only adapter lives
///     in Slugger.Cli: TextCopy is the CLI's single external dependency, and copying has no
///     meaning outside a command line.
/// </summary>
internal interface IClipboard {

    /// <summary>Replaces the clipboard contents, when the machine has a clipboard to replace.</summary>
    /// <param name="text">The slug to copy.</param>
    /// <returns>
    ///     Null once the text is on the clipboard; otherwise why it is not, short enough for one line.
    ///     Answered rather than thrown, because a clipboard that cannot be reached is never worth the
    ///     slugs a throw would take down with it.
    /// </returns>
    string? Copy(string text);

}