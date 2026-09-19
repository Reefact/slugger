using Slugger.Application.Abstractions;

namespace Slugger.Infrastructure.ThemeCatalogs;

/// <summary>The write side of the theme directory, behind <c>--register</c> and <c>--unregister</c>.</summary>
public sealed class FileSystemThemeStore : IThemeStore
{
    public FileSystemThemeStore(string? directoryPath = null) => DirectoryPath = directoryPath ?? FileSystemThemeCatalog.DefaultDirectoryPath;

    public string DirectoryPath { get; }

    /// <inheritdoc />
    public bool Contains(string name) => File.Exists(PathFor(name));

    public void Save(string name, string json) => throw new NotImplementedException();

    public void Delete(string name) => throw new NotImplementedException();

    /// <summary>Where a theme of that name lives, whether or not the file exists.</summary>
    public string PathFor(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Path.Combine(DirectoryPath, $"{name}.json");
    }
}
