using Slugger.Application.Abstractions;
using Slugger.Application.Options;

namespace Slugger.Infrastructure.Configuration;

/// <summary>
/// The defaults persisted by <c>--init</c>, in <c>~/.config/slugger/config.json</c> per the
/// XDG convention.
/// </summary>
public sealed class XdgConfigStore : IConfigStore
{
    public XdgConfigStore(string? filePath = null) => FilePath = filePath ?? DefaultFilePath;

    /// <summary>Honours <c>XDG_CONFIG_HOME</c> when it is set, and falls back to <c>~/.config</c>.</summary>
    public static string DefaultFilePath { get; } = Path.Combine(
        Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } configHome
            ? configHome
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"),
        "slugger",
        "config.json");

    public string FilePath { get; }

    public SluggerOptions? Load() => throw new NotImplementedException();

    public void Save(SluggerOptions options) => throw new NotImplementedException();
}
