#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Abstractions;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Serialization;

#endregion

namespace Slugger.UnitTests;

/// <summary>A theme store backed by a dictionary rather than a directory.</summary>
internal sealed class FakeThemeStore : IThemeStore {

    #region Fields

    private readonly Dictionary<string, string> _saved = new(StringComparer.Ordinal);

    #endregion

    /// <summary>Paths this store will serve, and whether the theme at each one is valid.</summary>
    internal Dictionary<string, ThemeDocument?> Files { get; } = new(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, string> Saved => _saved;

    /// <summary>What the fake was asked to write, so a test can read it back.</summary>
    public Dictionary<string, string> Written { get; } = new(StringComparer.Ordinal);

    public bool Contains(string name) {
        return _saved.ContainsKey(name);
    }

    public Outcome<ThemeDocument> LoadFile(string path, bool allowSmall = false) {
        if (!Files.TryGetValue(path, out ThemeDocument? theme)) { return ThemeLoader.Refuse(path, [ThemeErrors.NoSuchFile(path)]); }

        return theme is null
            ? ThemeLoader.Refuse(Path.GetFileNameWithoutExtension(path), [ThemeErrors.TooFewNouns(1, 100)])
            : Outcome<ThemeDocument>.Success(theme);
    }

    public string ReadFileText(string path) {
        return $"{{ \"from\": \"{path}\" }}";
    }

    public void WriteFileText(string path, string content) {
        Written[path] = content;
    }

    public void Save(string name, string json) {
        _saved[name] = json;
    }

    public void Delete(string name) {
        _saved.Remove(name);
    }

    public bool FileExists(string path) {
        return Files.ContainsKey(path);
    }

    public IReadOnlyList<string> Unselectable() {
        return [];
    }

    /// <summary>A file the fake refuses is one whose shape it refuses too.</summary>
    public Outcome<ThemeDocument> ReadWellFormed(string path) {
        if (Files.GetValueOrDefault(path) is not { } theme) { return ThemeLoader.Refuse(path, [ThemeErrors.MalformedSection("nouns", "an array")]); }

        return Outcome<ThemeDocument>.Success(theme);
    }

}