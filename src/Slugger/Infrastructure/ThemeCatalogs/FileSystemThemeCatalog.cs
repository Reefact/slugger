#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Abstractions;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Serialization;

#endregion

namespace Slugger.Infrastructure.ThemeCatalogs;

/// <summary>
///     The themes sitting in the theme directory as plain <c>.json</c> files, added or edited
///     without recompiling. A theme is identified by its file name, never by a field inside it.
/// </summary>
internal sealed class FileSystemThemeCatalog : IThemeCatalog {

    #region Static members

    /// <summary><c>~/.slugger/themes</c>, unless <c>--theme-dir</c> points somewhere else.</summary>
    internal static string DefaultDirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".slugger",
        "themes");

    #endregion

    #region Fields

    private readonly StringInternPool? _pool;

    #endregion

    #region Constructors & Destructor

    /// <param name="directoryPath">Where <c>--theme-dir</c> points, or null for the default location.</param>
    /// <param name="pool">The run's shared intern pool, when there is one.</param>
    internal FileSystemThemeCatalog(string? directoryPath = null, StringInternPool? pool = null) {
        DirectoryPath = directoryPath ?? DefaultDirectoryPath;
        _pool         = pool;
    }

    #endregion

    /// <summary>The directory this catalog reads.</summary>
    internal string DirectoryPath { get; }

    /// <inheritdoc />
    public bool Contains(string name) {
        return File.Exists(PathFor(name));
    }

    /// <inheritdoc />
    public Outcome<Theme> Load(string name, bool allowSmall = false) {
        string path = PathFor(name);

        return File.Exists(path)
            ? ThemeLoader.Load(name, File.ReadAllText(path), allowSmall, _pool)
            : ThemeLoader.Refuse(name, [ThemeErrors.NotFound(name, ListNames())]);
    }

    /// <inheritdoc />
    public Outcome<Theme> Parse(string name) {
        string path = PathFor(name);

        return File.Exists(path)
            ? ThemeLoader.Parse(name, File.ReadAllText(path), _pool)
            : ThemeLoader.Refuse(name, [ThemeErrors.NotFound(name, ListNames())]);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListNames() {
        if (!Directory.Exists(DirectoryPath)) { return []; }

        return Directory
              .EnumerateFiles(DirectoryPath, "*.json")
              .Select(file => Path.GetFileNameWithoutExtension(file.AsSpan()).ToString())
              .Where(name => name.Length > 0)
              .Order(StringComparer.Ordinal)
              .ToArray();
    }

    /// <summary>Where a theme of that name would live, whether or not the file exists.</summary>
    /// <param name="name">The theme to locate.</param>
    internal string PathFor(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Path.Combine(DirectoryPath, $"{name}.json");
    }

}