#region Usings declarations

using Slugger.Cli.Rendering;

#endregion

namespace Slugger.Cli;

/// <summary>The real terminal.</summary>
/// <remarks>
///     Two consoles rather than one, because they write to two different streams and are redirected
///     independently: a refusal drawn on the one measuring standard output would land in the pipe
///     that was only ever meant to carry slugs.
/// </remarks>
/// <param name="output">Where what went right is drawn.</param>
/// <param name="error">Where what did not is drawn.</param>
internal sealed class SystemConsole(Terminal output, Terminal error) : IConsole {

    /// <inheritdoc />
    public bool IsInputRedirected => Console.IsInputRedirected;

    /// <inheritdoc />
    public bool IsOutputRedirected => Console.IsOutputRedirected;

    /// <inheritdoc />
    public void WriteLine(string line) {
        Console.WriteLine(line);
    }

    /// <inheritdoc />
    public void WriteError(string line) {
        Console.Error.WriteLine(line);
    }

    /// <inheritdoc />
    public void WriteError(IReadOnlyList<DrawnLine> lines) {
        error.Write(lines);
    }

    /// <inheritdoc />
    public void Write(IReadOnlyList<DrawnLine> lines) {
        output.Write(lines);
    }

    /// <inheritdoc />
    public string? ReadLine() {
        return Console.ReadLine();
    }

}