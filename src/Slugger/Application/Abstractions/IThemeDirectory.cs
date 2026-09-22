namespace Slugger.Application.Abstractions;

/// <summary>
///     Builds the catalogs and the store for a given theme directory.
/// </summary>
/// <remarks>
///     A use case cannot be handed a ready-made catalog, because which directory to read is itself
///     part of the precedence chain: <c>--theme-dir</c> may come from the command line or from the
///     config saved by <c>--init</c>, and the config is only loaded once the use case runs. Baking
///     the directory in at composition time would make a saved <c>--theme-dir</c> impossible to
///     honour.
/// </remarks>
internal interface IThemeDirectory {

    /// <summary>The built-in themes alone, for telling apart what is a file and what is compiled in.</summary>
    IThemeCatalog Embedded { get; }

    /// <summary>A custom file in this directory shadowing the built-in theme of the same name.</summary>
    /// <param name="directoryPath">Where <c>--theme-dir</c> points, or null for the default location.</param>
    IThemeCatalog CatalogFor(string? directoryPath);

    /// <summary>The write side of that same directory.</summary>
    /// <param name="directoryPath">Where <c>--theme-dir</c> points, or null for the default location.</param>
    IThemeStore StoreFor(string? directoryPath);

}