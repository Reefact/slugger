using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--theme-info</c>: hands back a theme's own "meta" block, by name rather than by file path -
/// unlike <c>--analyze</c>, which measures a file that may not even be registered yet.
/// </summary>
/// <remarks>
/// Reads the file's shape only, never the domain's validation rules - a theme refused for its
/// pools, its exclusions or its length promise still has a "meta" block worth reading, and this
/// command is not the one that judges the rest.
/// </remarks>
internal sealed class ThemeInfoUseCase(IThemeDirectory directories, IConfigStore config)
{
    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore Config { get; } = config;

    /// <summary>Reads the theme's declared "meta" block.</summary>
    /// <param name="name">The theme to describe.</param>
    /// <param name="requested">What the command line asked for, which may point --theme-dir elsewhere.</param>
    internal Outcome<Theme> Execute(string name, SluggerOptions requested)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions session = OptionResolver.Merge(requested, Config.Load());

        return Directories.CatalogFor(session.ThemeDirectory).Parse(name);
    }
}
