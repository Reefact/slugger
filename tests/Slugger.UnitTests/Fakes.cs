using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Serialization;

namespace Slugger.UnitTests;

/// <summary>A catalog holding themes given to it, so a use case can be exercised without a disk.</summary>
internal sealed class FakeThemeCatalog(params Theme[] themes) : IThemeCatalog
{
    private readonly Dictionary<string, Theme> _themes = themes.ToDictionary(theme => theme.Name, StringComparer.Ordinal);

    /// <summary>Names this catalog should claim to carry but refuse to load, standing in for a broken file.</summary>
    internal HashSet<string> Broken { get; } = new(StringComparer.Ordinal);

    public bool Contains(string name) => _themes.ContainsKey(name) || Broken.Contains(name);

    public Outcome<Theme> Load(string name, bool allowSmall = false)
    {
        if (Broken.Contains(name))
        {
            return ThemeLoader.Refuse(name, [ThemeErrors.TooFewNouns(1, 100)]);
        }

        return _themes.TryGetValue(name, out Theme? theme)
            ? Outcome<Theme>.Success(theme)
            : ThemeLoader.Refuse(name, [ThemeErrors.NotFound(name, ListNames())]);
    }

    /// <summary>A fake holds ready-built themes, so there is no validation step to skip - same as <see cref="Load"/>.</summary>
    public Outcome<Theme> Parse(string name) => Load(name);

    public IReadOnlyList<string> ListNames() => [.. _themes.Keys.Concat(Broken).Order(StringComparer.Ordinal)];
}

/// <summary>
/// A theme directory that hands back whatever the test set up, remembering which path it was
/// asked for - which is how a test shows that --theme-dir actually reached it.
/// </summary>
internal sealed class FakeThemeDirectory(IThemeCatalog? catalog = null, IThemeStore? store = null) : IThemeDirectory
{
    private readonly IThemeCatalog _catalog = catalog ?? new FakeThemeCatalog();
    private readonly IThemeStore _store = store ?? new FakeThemeStore();

    /// <summary>Every directory path a use case asked for, in order.</summary>
    internal List<string?> Asked { get; } = [];

    public IThemeCatalog Embedded { get; init; } = new FakeThemeCatalog();

    public IThemeCatalog CatalogFor(string? directoryPath)
    {
        Asked.Add(directoryPath);

        return _catalog;
    }

    public IThemeStore StoreFor(string? directoryPath)
    {
        Asked.Add(directoryPath);

        return _store;
    }
}

/// <summary>A theme store backed by a dictionary rather than a directory.</summary>
internal sealed class FakeThemeStore : IThemeStore
{
    private readonly Dictionary<string, string> _saved = new(StringComparer.Ordinal);

    /// <summary>Paths this store will serve, and whether the theme at each one is valid.</summary>
    internal Dictionary<string, Theme?> Files { get; } = new(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, string> Saved => _saved;

    public bool Contains(string name) => _saved.ContainsKey(name);

    public Outcome<Theme> LoadFile(string path, bool allowSmall = false)
    {
        if (!Files.TryGetValue(path, out Theme? theme))
        {
            return ThemeLoader.Refuse(path, [ThemeErrors.MalformedSection("(file)", "a readable file")]);
        }

        return theme is null
            ? ThemeLoader.Refuse(Path.GetFileNameWithoutExtension(path), [ThemeErrors.TooFewNouns(1, 100)])
            : Outcome<Theme>.Success(theme);
    }

    public string ReadFileText(string path) => $"{{ \"from\": \"{path}\" }}";

    public void WriteFileText(string path, string content) => Written[path] = content;

    /// <summary>What the fake was asked to write, so a test can read it back.</summary>
    public Dictionary<string, string> Written { get; } = new(StringComparer.Ordinal);

    public void Save(string name, string json) => _saved[name] = json;

    public void Delete(string name) => _saved.Remove(name);
}

/// <summary>A config store that keeps what it was given in memory.</summary>
internal sealed class FakeConfigStore(SluggerOptions? initial = null) : IConfigStore
{
    internal SluggerOptions? Stored { get; private set; } = initial;

    public SluggerOptions? Load() => Stored;

    public void Save(SluggerOptions options) => Stored = options;
}

/// <summary>A clipboard that remembers the last thing copied to it.</summary>
internal sealed class FakeClipboard : IClipboard
{
    internal string? LastCopied { get; private set; }

    internal int Copies { get; private set; }

    public void Copy(string text)
    {
        LastCopied = text;
        Copies++;
    }
}
