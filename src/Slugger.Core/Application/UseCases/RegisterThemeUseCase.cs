using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain.Validation;
using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--register</c>: validate the file exactly as a runtime load would - same rules, same
/// error messages - then copy it into the theme directory under its own file name. Refuses
/// rather than overwrite an existing custom theme; warns, but proceeds, when the name
/// shadows a built-in one.
/// </summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = "Scaffolding: the body still throws, so the injected Catalog, Store and Validator are not read yet. Implementing it will read all three.")]
public sealed class RegisterThemeUseCase(IThemeCatalog catalog, IThemeStore store, ThemeValidator validator)
{
    private IThemeCatalog Catalog { get; } = catalog;
    private IThemeStore Store { get; } = store;
    private ThemeValidator Validator { get; } = validator;

    /// <summary>Validates the file and, if it passes, copies it into the theme directory.</summary>
    /// <param name="path">The theme file to register.</param>
    /// <param name="options">Where --theme-dir points, and whether --allow-small-theme was passed.</param>
    public ThemeValidationResult Execute(string path, SluggerOptions options) => throw new NotImplementedException();
}
