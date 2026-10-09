#region Usings declarations

using Slugger.Application.Abstractions;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The real theme directory, with a directory of the test's own standing in for
///     <c>~/.slugger/themes</c> wherever no <c>--theme-dir</c> was given. Without it, a theme whoever
///     runs the suite has registered reaches every test that names no directory: a new name lengthens
///     what <c>--list-themes</c> prints, and a <c>docker.json</c> of their own replaces the built-in
///     docker (measured, both).
/// </summary>
/// <param name="defaultDirectoryPath">What stands for the default location, which the test owns and removes.</param>
internal sealed class IsolatedThemeDirectory(string defaultDirectoryPath) : IThemeDirectory {

    #region Fields

    private readonly ThemeDirectory _real = new();

    #endregion

    public IThemeCatalog Embedded => _real.Embedded;

    public IThemeCatalog CatalogFor(string? directoryPath) {
        return _real.CatalogFor(directoryPath ?? defaultDirectoryPath);
    }

    public IThemeStore StoreFor(string? directoryPath) {
        return _real.StoreFor(directoryPath ?? defaultDirectoryPath);
    }

}
