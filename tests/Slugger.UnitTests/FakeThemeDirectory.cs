#region Usings declarations

using Slugger.Application.Abstractions;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     A theme directory that hands back whatever the test set up, remembering which path it was
///     asked for - which is how a test shows that --theme-dir actually reached it.
/// </summary>
internal sealed class FakeThemeDirectory(IThemeCatalog? catalog = null, IThemeStore? store = null) : IThemeDirectory {

    #region Fields

    private readonly IThemeCatalog _catalog = catalog ?? new FakeThemeCatalog();
    private readonly IThemeStore   _store   = store   ?? new FakeThemeStore();

    #endregion

    /// <summary>Every directory path a use case asked for, in order.</summary>
    internal List<string?> Asked { get; } = [];

    public IThemeCatalog Embedded { get; init; } = new FakeThemeCatalog();

    public IThemeCatalog CatalogFor(string? directoryPath) {
        Asked.Add(directoryPath);

        return _catalog;
    }

    public IThemeStore StoreFor(string? directoryPath) {
        Asked.Add(directoryPath);

        return _store;
    }

}