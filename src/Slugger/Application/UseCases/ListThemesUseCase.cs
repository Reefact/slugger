using Slugger.Application.Abstractions;
using Slugger.Application.Options;

namespace Slugger.Application.UseCases;

/// <summary><c>--list-themes</c>: the theme directory plus the built-in themes, deduplicated.</summary>
internal sealed class ListThemesUseCase(IThemeDirectory directories, IConfigStore config)
{
    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore Config { get; } = config;

    /// <summary>Lists every theme in scope, custom and built-in, deduplicated.</summary>
    /// <param name="requested">What the command line asked for, which may point --theme-dir elsewhere.</param>
    internal IReadOnlyList<string> Execute(SluggerOptions requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions session = OptionResolver.Merge(requested, Config.Load());

        return Directories.CatalogFor(session.ThemeDirectory).ListNames();
    }
}
