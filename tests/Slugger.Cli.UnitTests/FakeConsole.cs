#region Usings declarations

using Spectre.Console;
using Spectre.Console.Rendering;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     A terminal a test drives: it hands over the lines it was given, and keeps what was written
///     so the test can read it back.
/// </summary>
/// <remarks>
///     What was drawn is kept as the text it draws, so a test asserts on what a reader sees whether
///     the caller wrote a line or handed over a renderable.
/// </remarks>
internal sealed class FakeConsole(params string[] input) : IConsole {

    #region Static members

    /// <summary>
    ///     The text a renderable draws, one entry per line. Colourless, so an assertion reads the
    ///     sentence rather than the escape codes around it, and far wider than a terminal, so a long
    ///     message arrives as the one line it was written as rather than wrapped into three.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The last line is dropped only where it is the empty remainder of a trailing newline. A
    ///         renderable that ends without one - a bare Markup does - would otherwise lose its last line
    ///         here and nowhere else, so a test would assert on nothing and pass (measured).
    ///     </para>
    ///     <para>
    ///         Asking the settings for no colour is not enough. Spectre enriches a profile from the
    ///         environment after building it, and its GitHub Actions enricher turns ANSI back on because
    ///         that log viewer renders it - so an assertion on a sentence met the sentence wrapped in
    ///         escape codes, on the runner and nowhere else (measured). The enrichers are off here and
    ///         the capabilities are set on the profile, which is after anything could enrich it.
    ///     </para>
    /// </remarks>
    private static List<string> Drawn(IRenderable renderable) {
        StringWriter written = new();
        IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings {
            Ansi        = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out         = new AnsiConsoleOutput(written),
            Enrichment  = new ProfileEnrichment { UseDefaultEnrichers = false }
        });
        console.Profile.Capabilities.Ansi        = false;
        console.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        console.Profile.Width                    = 240;
        console.Write(renderable);

        List<string> lines = [
            .. written.ToString()
                      .Split('\n')
                      .Select(line => line.TrimEnd('\r', ' '))
        ];

        if (lines is [.., ""]) {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    #endregion

    #region Fields

    private readonly Queue<string> _input = new(input);

    #endregion

    /// <summary>Whether the REPL should turn itself off, as it does behind a pipe.</summary>
    internal bool IsInputRedirected { get; init; }

    bool IConsole.IsInputRedirected => IsInputRedirected;

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

    public void WriteError(IRenderable renderable) {
        Errors.AddRange(Drawn(renderable));
    }

    public void Write(IRenderable renderable) {
        Output.AddRange(Drawn(renderable));
    }

    public string? ReadLine() {
        return _input.Count > 0 ? _input.Dequeue() : null;
    }

}