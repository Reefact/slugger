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
        if (!Files.TryGetValue(path, out ThemeDocument? theme)) { return ThemeLoader.Refuse(path, [ThemeErrors.MalformedSection("(file)", "a readable file")]); }

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

}