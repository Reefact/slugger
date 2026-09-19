namespace Slugger.Cli.UnitTests;

/// <summary>
/// A terminal a test drives: it hands over the lines it was given, and keeps what was written
/// so the test can read it back.
/// </summary>
internal sealed class FakeConsole(params string[] input) : IConsole
{
    private readonly Queue<string> _input = new(input);

    /// <summary>Whether the REPL should turn itself off, as it does behind a pipe.</summary>
    internal bool IsInputRedirected { get; init; }

    bool IConsole.IsInputRedirected => IsInputRedirected;

    /// <summary>Everything written to standard output, in order.</summary>
    internal List<string> Output { get; } = [];

    /// <summary>Everything written to standard error, in order.</summary>
    internal List<string> Errors { get; } = [];

    public void WriteLine(string line) => Output.Add(line);

    public void WriteError(string line) => Errors.Add(line);

    public string? ReadLine() => _input.Count > 0 ? _input.Dequeue() : null;
}
