#region Usings declarations

using System.Reflection;
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
///     <para>
///         Null members are left out on the way in and read back as null, which is what keeps the
///         precedence chain honest: a saved config has to be able to say nothing about an option, not
///         just say "the default", or it would override what a theme meant to decide.
///     </para>
///     <para>
///         The file is plain JSON that someone may edit by hand, so it is read leniently and never
///         silently: a key matches whatever its case, and a file that will not parse, a key that
///         means nothing and a value that cannot be read each come back as a remark naming the file.
///     </para>
/// </remarks>
internal sealed class XdgConfigStore : IConfigStore {

    #region Static members

    private static readonly JsonSerializerOptions Format = new() {
        WriteIndented               = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters                  = { new JsonStringEnumConverter() }
    };

    /// <summary>Every key the file can hold, which is every option <c>--init</c> can save.</summary>
    private static readonly string[] Keys = [
        .. typeof(SluggerOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name)
    ];

    /// <summary>
    ///     The spellings a theme's <c>defaults</c> and the command line use where they differ from the
    ///     key here by more than case, dashes or underscores - the ones a hand-edited file is likeliest
    ///     to carry. Looked up without the dashes and underscores, whatever the case.
    /// </summary>
    private static readonly Dictionary<string, string> OtherSpellings = new(StringComparer.OrdinalIgnoreCase) {
        ["sep"]        = nameof(SluggerOptions.Separator),
        ["wordSep"]    = nameof(SluggerOptions.WordSeparator),
        ["allowSmall"] = nameof(SluggerOptions.AllowSmallTheme),
        ["theme"]      = nameof(SluggerOptions.Themes),
        ["themeDir"]   = nameof(SluggerOptions.ThemeDirectory),
        ["segment"]    = nameof(SluggerOptions.SegmentMode)
    };

    /// <summary>Honours <c>XDG_CONFIG_HOME</c> when it is set, and falls back to <c>~/.config</c>.</summary>
    internal static string DefaultFilePath { get; } = Path.Combine(
        Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } configHome
            ? configHome
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"),
        "slugger",
        "config.json");

    /// <summary>The document the text holds, or null where it is not JSON at all.</summary>
    /// <param name="text">The whole file.</param>
    private static JsonElement? Parse(string text) {
        try {
            using JsonDocument document = JsonDocument.Parse(text);

            return document.RootElement.Clone();
        } catch (JsonException) {
            return null;
        }
    }

    /// <summary>The keys of the document that name no option, in the order the file writes them.</summary>
    /// <param name="root">The whole document.</param>
    private static IEnumerable<string> UnknownKeys(JsonElement root) {
        if (root.ValueKind != JsonValueKind.Object) { return []; }

        return root.EnumerateObject()
                   .Select(property => property.Name)
                   .Where(key => !Keys.Contains(key, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>The key this one most likely stands for, or null where nothing obvious does.</summary>
    /// <param name="key">A key that names no option.</param>
    private static string? Meant(string key) {
        string bare = key.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal);
        if (OtherSpellings.TryGetValue(bare, out string? spelled)) { return spelled; }

        return Array.Find(Keys, known => known.Equals(bare, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The key a reading stopped on, as the file writes it, or null where it stopped on the document itself.</summary>
    /// <param name="path">Where the serializer says it stopped, such as <c>$.Casing</c>.</param>
    private static string? KeyAt(string? path) {
        if (path is null) { return null; }
        if (!path.StartsWith("$.", StringComparison.Ordinal)) { return null; }

        return path[2..];
    }

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
        return Read().Options;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     A config that cannot be read is treated as no config at all rather than as a fatal error: a
    ///     broken file in the home directory must not make the tool unusable, and the fix - running
    ///     --init again - is one command away. It is never ignored without saying so, though, or a
    ///     user who edited the file would never learn why it changed nothing.
    /// </remarks>
    public SavedConfig Read() {
        if (!File.Exists(FilePath)) { return new SavedConfig(null, []); }

        JsonElement? parsed = Parse(File.ReadAllText(FilePath));
        if (parsed is not { } root) { return new SavedConfig(null, [$"{FilePath} is not valid JSON and was ignored"]); }

        List<string> remarks = [.. UnknownKeys(root).Select(UnknownKey)];
        try {
            return new SavedConfig(root.Deserialize<SluggerOptions>(Format), remarks);
        } catch (JsonException unreadable) {
            remarks.Add(KeyAt(unreadable.Path) is { } key
                            ? $"{FilePath}: the value of \"{key}\" cannot be read, so the file was ignored"
                            : $"{FilePath} does not hold slugger's defaults and was ignored");

            return new SavedConfig(null, remarks);
        }
    }

    /// <inheritdoc />
    public void Save(SluggerOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(options, Format));
    }

    /// <summary>The remark about a key that names no option, with the one it stands for when that is obvious.</summary>
    /// <param name="key">The key, as the file writes it.</param>
    private string UnknownKey(string key) {
        return Meant(key) is { } meant
            ? $"{FilePath}: unknown key \"{key}\"; did you mean \"{meant}\"?"
            : $"{FilePath}: unknown key \"{key}\"";
    }

}
