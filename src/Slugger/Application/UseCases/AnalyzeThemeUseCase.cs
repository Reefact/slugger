using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Analysis;
using Slugger.Domain.Generation;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--analyze</c>: measures a theme file and hands back the numbers, without writing a word
/// about them. Rendering belongs to the caller.
/// </summary>
/// <remarks>
/// It loads the file waiving the size rules on purpose. A theme is analysed precisely when it
/// does not pass, and a report that refused to measure what does not load would be useless at
/// the one moment it is wanted - knowing a noun reaches 8 participles rather than 19 is what
/// tells its author what to fix. The refusals come back all the same, from the validator run
/// against the real floors.
/// </remarks>
internal sealed class AnalyzeThemeUseCase(IThemeDirectory directories, IConfigStore config)
{
    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore Config { get; } = config;

    /// <summary>Measures the file at that path.</summary>
    /// <param name="path">The theme file to analyse.</param>
    /// <param name="requested">What the command line asked for, which may point --theme-dir elsewhere.</param>
    internal ThemeAnalysis Execute(string path, SluggerOptions requested)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions? saved = Config.Load();
        SluggerOptions session = OptionResolver.Merge(requested, saved);
        IThemeStore store = Directories.StoreFor(session.ThemeDirectory);
        string name = Path.GetFileNameWithoutExtension(path.AsSpan()).ToString();

        Outcome<Theme> loaded = store.LoadFile(path, allowSmall: true);

        // Nothing survives a document that will not parse, or one holding no noun at all: there
        // is no theme to measure, only the reasons there is none.
        if (loaded.Error is not { } unreadable)
        {
            // One theme in scope, so its own defaults speak - and --max-length narrows the surface
            // exactly as it would for a run, which is what makes the report answer for that run.
            Theme theme = loaded.GetResultOrThrow();
            GenerationOptions style = OptionResolver.Resolve(requested, saved, theme, themesInScope: 1);

            return ThemeAnalyzer.Analyze(SlugGenerator.ResolverFor(theme, style), style);
        }

        // A load refusal carries its reasons inside; a lone one carries itself.
        IReadOnlyList<Error> reasons = unreadable.InnerErrors.Count > 0 ? unreadable.InnerErrors : [unreadable];

        return ThemeAnalyzer.Unreadable(name, reasons);
    }
}
