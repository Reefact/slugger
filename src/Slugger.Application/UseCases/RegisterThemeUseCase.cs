using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain.Validation;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--register</c>: validate the file exactly as a runtime load would - same rules, same
/// error messages - then copy it into the theme directory under its own file name. Refuses
/// rather than overwrite an existing custom theme; warns, but proceeds, when the name
/// shadows a built-in one.
/// </summary>
public sealed class RegisterThemeUseCase(IThemeCatalog catalog, IThemeStore store, ThemeValidator validator)
{
    private IThemeCatalog Catalog { get; } = catalog;
    private IThemeStore Store { get; } = store;
    private ThemeValidator Validator { get; } = validator;

    public ThemeValidationResult Execute(string path, SluggerOptions options) => throw new NotImplementedException();
}
