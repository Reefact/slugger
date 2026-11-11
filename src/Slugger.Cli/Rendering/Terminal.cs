#region Usings declarations

using System.Text;

#endregion

namespace Slugger.Cli.Rendering;

/// <summary>A stream lines are drawn on, in colour or not.</summary>
/// <param name="output">Where the lines go.</param>
/// <param name="colours">Whether inks are drawn, or left out.</param>
internal sealed class Terminal(TextWriter output, bool colours) {

    private const string Reset = "\u001b[0m";

    #region Static members

    /// <summary>The terminal behind a standard stream, in colour where <see cref="Allows" /> says so.</summary>
    /// <param name="output">The stream.</param>
    /// <param name="redirected">Whether it goes to a file or a pipe.</param>
    internal static Terminal For(TextWriter output, bool redirected) {
        bool colours = Allows(redirected,
                              Environment.GetEnvironmentVariable("NO_COLOR"),
                              OperatingSystem.IsWindows(),
                              Environment.GetEnvironmentVariable("WT_SESSION"));

        return new Terminal(output, colours);
    }

    /// <summary>
    ///     Colours only where a person reads the stream and nobody asked for them off: never in a
    ///     file or a pipe, where a slug must arrive as nothing but itself, and never under
    ///     <c>NO_COLOR</c> (https://no-color.org). On Windows, only in Windows Terminal, which sets
    ///     <c>WT_SESSION</c>: the older console host prints escape codes as text until asked to read
    ///     them, and is left in plain text rather than asked.
    /// </summary>
    /// <param name="redirected">Whether the stream goes to a file or a pipe.</param>
    /// <param name="noColor">The value of <c>NO_COLOR</c>, null when it is not set.</param>
    /// <param name="windows">Whether this runs on Windows.</param>
    /// <param name="windowsTerminal">The value of <c>WT_SESSION</c>, null when it is not set.</param>
    internal static bool Allows(bool redirected, string? noColor, bool windows, string? windowsTerminal) {
        if (redirected) { return false; }
        if (!string.IsNullOrEmpty(noColor)) { return false; }
        if (!windows) { return true; }

        return !string.IsNullOrEmpty(windowsTerminal);
    }

    /// <summary>
    ///     The escape code that opens an ink, null for the terminal's own text. The colours are the
    ///     sixteen a terminal's theme sets, so they follow it.
    /// </summary>
    private static string? Opening(Ink ink) {
        return ink switch {
            Ink.Faint      => "\u001b[2m",
            Ink.Refusal    => "\u001b[1;91m",
            Ink.Acceptance => "\u001b[1;32m",
            Ink.Warning    => "\u001b[1;93m",
            Ink.Heading    => "\u001b[93m",
            _              => null
        };
    }

    #endregion

    /// <summary>Writes every line, each ended by a new line.</summary>
    /// <param name="lines">What to write.</param>
    internal void Write(IEnumerable<DrawnLine> lines) {
        ArgumentNullException.ThrowIfNull(lines);

        StringBuilder written = new();
        foreach (DrawnLine line in lines) {
            Append(written, line);
            written.AppendLine();
        }

        output.Write(written.ToString());
        output.Flush();
    }

    private void Append(StringBuilder written, DrawnLine line) {
        string? opening = colours ? Opening(line.Ink) : null;
        if (opening is null || line.Text.Length == 0) {
            written.Append(line.Text);

            return;
        }

        written.Append(opening).Append(line.Text).Append(Reset);
    }

}