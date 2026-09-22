#region Usings declarations

using Spectre.Console.Rendering;

#endregion

namespace Slugger.Cli;

/// <summary>
///     The terminal, behind a seam. The REPL's whole behaviour is what it does with a line that
///     never comes, so a test has to be able to hand it one - and to read what it wrote back.
/// </summary>
/// <remarks>
///     A slug goes out as a line and nothing else: it is what the next command in the pipe reads,
///     and a colour code in it would be rubbish. Everything written for a person to look at goes out
///     as something Spectre lays out (DEC0019), which a test reads back as the text it draws.
/// </remarks>
internal interface IConsole {

    /// <summary>
    ///     Whether standard input is a pipe, a script or a CI runner rather than someone typing.
    ///     True is what makes slugger generate once and quit instead of blocking on a ReadLine that
    ///     will never be answered.
    /// </summary>
    bool IsInputRedirected { get; }

    /// <summary>Writes a line to standard output.</summary>
    /// <param name="line">What to write.</param>
    void WriteLine(string line);

    /// <summary>Writes a line to standard error, where a refusal belongs.</summary>
    /// <param name="line">What to write.</param>
    void WriteError(string line);

    /// <summary>Draws something on standard error, where a refusal belongs.</summary>
    /// <param name="renderable">What Spectre is to lay out.</param>
    void WriteError(IRenderable renderable);

    /// <summary>Draws something on standard output.</summary>
    /// <param name="renderable">What Spectre is to lay out.</param>
    void Write(IRenderable renderable);

    /// <summary>Reads a line, or null once there is no more input.</summary>
    string? ReadLine();

}