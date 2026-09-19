using Slugger.Application.Abstractions;
using Slugger.Application.Options;

namespace Slugger.Application.UseCases;

/// <summary><c>--list-themes</c>: the theme directory plus the built-in themes, deduplicated.</summary>
internal sealed class ListThemesUseCase(IThemeCatalog catalog)
{
    private IThemeCatalog Catalog { get; } = catalog;

    /// <summary>Lists every theme in scope, custom and built-in, deduplicated.</summary>
    /// <param name="options">Unused for now; the theme directory is already baked into the catalog.</param>
    internal IReadOnlyList<string> Execute(SluggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Catalog.ListNames();
    }
}
