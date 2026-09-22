#region Usings declarations

using System.Text.Json;
using System.Text.Json.Serialization;

using Slugger.Application.Abstractions;
using Slugger.Application.Options;

#endregion

namespace Slugger.Infrastructure.Configuration;

/// <summary>
///     The defaults persisted by <c>--init</c>, in <c>~/.config/slugger/config.json</c> per the
///     XDG convention.
/// </summary>
/// <remarks>
///     Null members are left out on the way in and read back as null, which is what keeps the
///     precedence chain honest: a saved config has to be able to say nothing about an option, not
///     just say "the default", or it would override what a theme meant to decide.
/// </remarks>
internal sealed class XdgConfigStore : IConfigStore {

    #region Static members

    private static readonly JsonSerializerOptions Format = new() {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters             = { new JsonStringEnumConverter() }
    };

    /// <summary>Honours <c>XDG_CONFIG_HOME</c> when it is set, and falls back to <c>~/.config</c>.</summary>
    internal static string DefaultFilePath { get; } = Path.Combine(
        Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } configHome
            ? configHome
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"),
        "slugger",
        "config.json");

    #endregion

    #region Constructors & Destructor

    /// <param name="filePath">Where the config lives, or null for the XDG location.</param>
    internal XdgConfigStore(string? filePath = null) {
        FilePath = filePath ?? DefaultFilePath;
    }

    #endregion

    /// <summary>The config file this store reads and writes.</summary>
    internal string FilePath { get; }

    /// <inheritdoc />
    public SluggerOptions? Load() {
        if (!File.Exists(FilePath)) {
            return null;
        }

        // A config that will not parse is treated as no config at all rather than as a fatal
        // error: a broken file in the home directory must not make the tool unusable, and the
        // fix - running --init again - is one command away.
        try {
            return JsonSerializer.Deserialize<SluggerOptions>(File.ReadAllText(FilePath), Format);
        } catch (JsonException) {
            return null;
        }
    }

    /// <inheritdoc />
    public void Save(SluggerOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(options, Format));
    }

}