using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Validation;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--theme-info</c>: hands back a theme's own "meta" block, by name rather than by file path -
/// unlike <c>--analyze</c>, which measures a file that may not even be registered yet.
/// </summary>
internal sealed class ThemeInfoUseCase(IThemeDirectory directories, IConfigStore config)
{
    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore Config { get; } = config;

    /// <summary>Loads the theme, size rules waived - this reads what the author wrote, not whether the theme passes.</summary>
    /// <param name="name">The theme to describe.</param>
    /// <param name="requested">What the command line asked for, which may point --theme-dir elsewhere.</param>
    internal Outcome<Theme> Execute(string name, SluggerOptions requested)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions session = OptionResolver.Merge(requested, Config.Load());
        IThemeCatalog catalog = Directories.CatalogFor(session.ThemeDirectory);

        return catalog.Contains(name)
            ? catalog.Load(name, allowSmall: true)
            : Outcome<Theme>.Failure(ThemeErrors.NotFound(name, catalog.ListNames()));
    }
}
