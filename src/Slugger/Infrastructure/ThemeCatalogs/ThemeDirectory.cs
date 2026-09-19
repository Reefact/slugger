using Slugger.Application.Abstractions;
using Slugger.Infrastructure.Serialization;

namespace Slugger.Infrastructure.ThemeCatalogs;

/// <summary>
/// The real theme directory: a chain of the file system over the embedded themes, built per
/// call so that a directory resolved from the config is honoured as readily as one passed on
/// the command line.
/// </summary>
internal sealed class ThemeDirectory : IThemeDirectory
{
    private readonly StringInternPool? _pool;

    /// <param name="pool">The run's shared intern pool, so every theme loaded interns against the same one.</param>
    internal ThemeDirectory(StringInternPool? pool = null)
    {
        _pool = pool;
        Embedded = new EmbeddedThemeCatalog(pool);
    }

    /// <inheritdoc />
    public IThemeCatalog Embedded { get; }

    /// <inheritdoc />
    public IThemeCatalog CatalogFor(string? directoryPath) =>
        new ChainedThemeCatalog(new FileSystemThemeCatalog(directoryPath, _pool), Embedded);

    /// <inheritdoc />
    public IThemeStore StoreFor(string? directoryPath) => new FileSystemThemeStore(directoryPath);
}
