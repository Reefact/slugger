using Slugger.Application.Abstractions;
using Slugger.Application.Options;

namespace Slugger.Application.UseCases;

/// <summary><c>--list-themes</c>: the theme directory plus the built-in themes, deduplicated.</summary>
public sealed class ListThemesUseCase(IThemeCatalog catalog)
{
    private IThemeCatalog Catalog { get; } = catalog;

    public IReadOnlyList<string> Execute(SluggerOptions options) => throw new NotImplementedException();
}
