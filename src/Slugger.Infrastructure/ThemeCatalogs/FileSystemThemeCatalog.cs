using Slugger.Application.Abstractions;
using Slugger.Domain;

namespace Slugger.Infrastructure.ThemeCatalogs;

/// <summary>
/// The themes sitting in the theme directory as plain <c>.json</c> files, added or edited
/// without recompiling. A theme is identified by its file name, never by a field inside it.
/// </summary>
public sealed class FileSystemThemeCatalog : IThemeCatalog
{
    public FileSystemThemeCatalog(string? directoryPath = null) => DirectoryPath = directoryPath ?? DefaultDirectoryPath;

    /// <summary><c>~/.slugger/themes</c>, unless <c>--theme-dir</c> points somewhere else.</summary>
    public static string DefaultDirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".slugger",
        "themes");

    public string DirectoryPath { get; }

    public Theme? Find(string name) => throw new NotImplementedException();

    /// <inheritdoc />
    public IReadOnlyList<string> ListNames()
    {
        if (!System.IO.Directory.Exists(DirectoryPath))
        {
            return [];
        }

        return System.IO.Directory
            .EnumerateFiles(DirectoryPath, "*.json")
            .Select(file => Path.GetFileNameWithoutExtension(file.AsSpan()).ToString())
            .Where(name => name.Length > 0)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
