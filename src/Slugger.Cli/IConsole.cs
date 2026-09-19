namespace Slugger.Cli;

/// <summary>
/// The terminal, behind a seam. The REPL's whole behaviour is what it does with a line that
/// never comes, so a test has to be able to hand it one - and to read what it wrote back.
/// </summary>
internal interface IConsole
{
    /// <summary>
    /// Whether standard input is a pipe, a script or a CI runner rather than someone typing.
    /// True is what makes slugger generate once and quit instead of blocking on a ReadLine that
    /// will never be answered.
    /// </summary>
    bool IsInputRedirected { get; }

    /// <summary>Writes a line to standard output.</summary>
    /// <param name="line">What to write.</param>
    void WriteLine(string line);

    /// <summary>Writes a line to standard error, where a refusal belongs.</summary>
    /// <param name="line">What to write.</param>
    void WriteError(string line);

    /// <summary>Reads a line, or null once there is no more input.</summary>
    string? ReadLine();
}

/// <summary>The real terminal.</summary>
internal sealed class SystemConsole : IConsole
{
    /// <inheritdoc />
    public bool IsInputRedirected => Console.IsInputRedirected;

    /// <inheritdoc />
    public void WriteLine(string line) => Console.WriteLine(line);

    /// <inheritdoc />
    public void WriteError(string line) => Console.Error.WriteLine(line);

    /// <inheritdoc />
    public string? ReadLine() => Console.ReadLine();
}
