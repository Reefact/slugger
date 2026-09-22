#region Usings declarations

using Spectre.Console;
using Spectre.Console.Rendering;

#endregion

namespace Slugger.Cli;

/// <summary>The real terminal.</summary>
/// <remarks>
///     Two consoles rather than one, because they write to two different streams and are redirected
///     independently: a refusal drawn on the one measuring standard output would land in the pipe
///     that was only ever meant to carry slugs.
/// </remarks>
/// <param name="output">Where Spectre draws what went right.</param>
/// <param name="error">Where Spectre draws what did not.</param>
internal sealed class SystemConsole(IAnsiConsole output, IAnsiConsole error) : IConsole {

    /// <inheritdoc />
    public bool IsInputRedirected => Console.IsInputRedirected;

    /// <inheritdoc />
    public void WriteLine(string line) {
        Console.WriteLine(line);
    }

    /// <inheritdoc />
    public void WriteError(string line) {
        Console.Error.WriteLine(line);
    }

    /// <inheritdoc />
    public void WriteError(IRenderable renderable) {
        error.Write(renderable);
    }

    /// <inheritdoc />
    public void Write(IRenderable renderable) {
        output.Write(renderable);
    }

    /// <inheritdoc />
    public string? ReadLine() {
        return Console.ReadLine();
    }

}