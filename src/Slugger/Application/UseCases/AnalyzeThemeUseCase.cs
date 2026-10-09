#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Analysis;
using Slugger.Domain.Generation;

#endregion

namespace Slugger.Application.UseCases;

/// <summary>
///     <c>--analyze</c>: measures a theme file and hands back the numbers, without writing a word
///     about them. Rendering belongs to the caller.
/// </summary>
/// <remarks>
///     It loads the file waiving the size rules on purpose. A theme is analysed precisely when it
///     does not pass, and a report that refused to measure what does not load would be useless at
///     the one moment it is wanted - knowing a noun reaches 8 participles rather than 19 is what
///     tells its author what to fix. The refusals come back all the same, from the validator run
///     against the real floors.
/// </remarks>
internal sealed class AnalyzeThemeUseCase(IThemeDirectory directories, IConfigStore config) {

    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore    Config      { get; } = config;

    /// <summary>Measures the file at that path.</summary>
    /// <param name="path">The theme file to analyse.</param>
    /// <param name="requested">What the command line asked for, which may point --theme-dir elsewhere.</param>
    /// <returns>
    ///     The analysis, refusals and all - or a failure when there is no file at that path, which
    ///     leaves nothing to analyse and no report to owe.
    /// </returns>
    internal Outcome<ThemeAnalysis> Execute(string path, SluggerOptions requested) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions? saved   = Config.Load();
        SluggerOptions  session = OptionResolver.Merge(requested, saved);
        IThemeStore     store   = Directories.StoreFor(session.ThemeDirectory);
        string          name    = Path.GetFileNameWithoutExtension(path.AsSpan()).ToString();

        Outcome<ThemeDocument> loaded = store.LoadFile(path, true);
        if (loaded.Error is not { } refused) { return Outcome<ThemeAnalysis>.Success(Measured(loaded.GetResultOrThrow(), requested, saved)); }
        if (!store.FileExists(path)) { return Outcome<ThemeAnalysis>.Failure(refused); }

        // Nothing survives a document that will not parse, or one holding no noun at all: there
        // is no theme to measure, only the reasons there is none. A load refusal carries its
        // reasons inside; a lone one carries itself.
        IReadOnlyList<Error> reasons = refused.InnerErrors.Count > 0 ? refused.InnerErrors : [refused];

        return Outcome<ThemeAnalysis>.Success(ThemeAnalyzer.Unreadable(name, reasons));
    }

    /// <summary>
    ///     One theme in scope, so its own defaults speak - and --max-length narrows the surface
    ///     exactly as it would for a run, which is what makes the report answer for that run.
    /// </summary>
    private static ThemeAnalysis Measured(ThemeDocument theme, SluggerOptions requested, SluggerOptions? saved) {
        GenerationOptions style = OptionResolver.Resolve(requested, saved, theme, 1);

        return ThemeAnalyzer.Analyze(SlugGenerator.ResolverFor(theme, style), style);
    }

}