using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;

namespace Slugger.Application.UseCases;

/// <summary><c>--list-themes</c>: the theme directory plus the built-in themes, deduplicated.</summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = "Scaffolding: the body still throws, so the injected Catalog is not read yet. Implementing it will.")]
public sealed class ListThemesUseCase(IThemeCatalog catalog)
{
    private IThemeCatalog Catalog { get; } = catalog;

    /// <summary>Lists every theme in scope, custom and built-in, deduplicated.</summary>
    /// <param name="options">Where --theme-dir points, if anywhere.</param>
    public IReadOnlyList<string> Execute(SluggerOptions options) => throw new NotImplementedException();
}
