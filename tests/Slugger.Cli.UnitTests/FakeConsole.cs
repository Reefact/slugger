#region Usings declarations

using Slugger.Cli.Rendering;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     A terminal a test drives: it hands over the lines it was given, and keeps what was written
///     so the test can read it back.
/// </summary>
/// <remarks>
///     What was drawn is kept as the text it draws, so a test asserts on what a reader sees whether
///     the caller wrote a line or handed over drawn lines.
/// </remarks>
internal sealed class FakeConsole(params string[] input) : IConsole {

    #region Static members

    /// <summary>The text of drawn lines, one entry per line, their inks left out.</summary>
    private static IEnumerable<string> Drawn(IReadOnlyList<DrawnLine> lines) {
        return lines.Select(line => line.Text);
    }

    #endregion

    #region Fields

    private readonly Queue<string> _input = new(input);

    #endregion

    /// <summary>Whether the REPL should turn itself off, as it does behind a pipe.</summary>
    internal bool IsInputRedirected { get; init; }

    bool IConsole.IsInputRedirected => IsInputRedirected;

    /// <summary>Whether the REPL should turn itself off, as it does when its output is captured.</summary>
    internal bool IsOutputRedirected { get; init; }

    bool IConsole.IsOutputRedirected => IsOutputRedirected;

    /// <summary>Everything written to standard output, in order.</summary>
    internal List<string> Output { get; } = [];

    /// <summary>Everything written to standard error, in order.</summary>
    internal List<string> Errors { get; } = [];

    public void WriteLine(string line) {
        Output.Add(line);
    }

    public void WriteError(string line) {
        Errors.Add(line);
    }

    public void WriteError(IReadOnlyList<DrawnLine> lines) {
        Errors.AddRange(Drawn(lines));
    }

    public void Write(IReadOnlyList<DrawnLine> lines) {
        Output.AddRange(Drawn(lines));
    }

    public string? ReadLine() {
        return _input.Count > 0 ? _input.Dequeue() : null;
    }

}