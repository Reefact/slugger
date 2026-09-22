#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Abstractions;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Serialization;

#endregion

namespace Slugger.Infrastructure.ThemeCatalogs;

/// <summary>The write side of the theme directory, behind <c>--register</c> and <c>--unregister</c>.</summary>
internal sealed class FileSystemThemeStore : IThemeStore {

    #region Constructors & Destructor

    /// <param name="directoryPath">Where <c>--theme-dir</c> points, or null for the default location.</param>
    internal FileSystemThemeStore(string? directoryPath = null) {
        DirectoryPath = directoryPath ?? FileSystemThemeCatalog.DefaultDirectoryPath;
    }

    #endregion

    /// <summary>The directory this store writes to.</summary>
    internal string DirectoryPath { get; }

    /// <inheritdoc />
    public bool Contains(string name) {
        return File.Exists(PathFor(name));
    }

    /// <inheritdoc />
    public Outcome<Theme> LoadFile(string path, bool allowSmall = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string name = Path.GetFileNameWithoutExtension(path.AsSpan()).ToString();
        if (name.Length == 0) {
            name = "(unnamed)";
        }

        return File.Exists(path)
            ? ThemeLoader.Load(name, File.ReadAllText(path), allowSmall)
            : ThemeLoader.Refuse(name, [ThemeErrors.MalformedSection("(file)", $"a readable file; \"{path}\" does not exist")]);
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public void WriteFileText(string path, string content) {
        File.WriteAllText(path, content);
    }

    /// <inheritdoc />
    public string ReadFileText(string path) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return File.ReadAllText(path);
    }

    /// <inheritdoc />
    public void Save(string name, string json) {
        ArgumentNullException.ThrowIfNull(json);

        // The directory is created on demand: a first --register should not need the user to
        // have made ~/.slugger/themes by hand.
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(PathFor(name), json);
    }

    /// <inheritdoc />
    public void Delete(string name) {
        File.Delete(PathFor(name));
    }

    /// <summary>Where a theme of that name lives, whether or not the file exists.</summary>
    /// <param name="name">The theme to locate.</param>
    internal string PathFor(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Path.Combine(DirectoryPath, $"{name}.json");
    }

}