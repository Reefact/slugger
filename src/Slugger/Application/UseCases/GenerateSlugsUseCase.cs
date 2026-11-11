#region Usings declarations

using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Errors;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Application.UseCases;

/// <summary>
///     The main path: resolve the themes in scope, draw, format, optionally copy. Drives the
///     REPL and the oneshot mode alike - the loop belongs to the CLI, a batch of slugs belongs
///     here.
/// </summary>
internal sealed class GenerateSlugsUseCase(IThemeDirectory directories, IConfigStore config, IClipboard clipboard) {

    /// <summary>The only theme in scope when <c>--theme</c> says nothing.</summary>
    internal const string DefaultThemeName = "slugger";

    #region Static members

    /// <summary>
    ///     Settles the options and the surface of every theme before the first draw, rather than once
    ///     per slug - which is what <see cref="SlugGenerator" /> is for, the library's own way and this
    ///     command's alike: a length budget reduces the pools, and reducing them a thousand times for
    ///     <c>--count 1000</c> would be a thousand times the work for the same answer.
    /// </summary>
    /// <remarks>
    ///     This is also where a run is judged against what it actually draws (DEC0016, DEC0018): the
    ///     source validates a theme its options reduce or draw in another mode, and refuses the run.
    ///     The refusal is the command's own, so that a theme refused among others can say how to keep
    ///     its style.
    /// </remarks>
    private static Outcome<SlugGenerator> Prepare(IReadOnlyList<ThemeDocument> themes,
                                                  SluggerOptions               requested,
                                                  SluggerOptions?              saved,
                                                  SluggerOptions               session) {
        SluggerTheme[] handedOut = [.. themes.Select(theme => new SluggerTheme(theme))];

        return SlugGenerator.Prepare(
            handedOut,
            theme => OptionResolver.Resolve(requested, saved, theme.Content, themes.Count),
            session.AllowSmallTheme ?? false,
            (theme, resolver, refusals) => Rejection(theme, resolver, refusals, themes.Count, requested));
    }

    /// <summary>
    ///     A theme refused in a draw among others, under a mode the command line did not ask for, was
    ///     refused for a mode it does not draw on its own: the styles of a mixed draw are switched off
    ///     (DEC0004). The refusal then carries both modes, so that it can say how to keep the theme's.
    /// </summary>
    private static SluggerThemeError Rejection(ThemeDocument              theme,
                                         ThemeResolver              resolver,
                                         IReadOnlyList<SluggerThemeError> refusals,
                                         int                        themesInScope,
                                         SluggerOptions             requested) {
        if (themesInScope < 2) { return SluggerThemeError.Rejected(theme.Name, refusals); }
        if (requested.SegmentMode is not null) { return SluggerThemeError.Rejected(theme.Name, refusals); }

        SegmentMode own   = ThemeValidator.DrawnMode(theme);
        SegmentMode drawn = ThemeValidator.DrawnMode(resolver);
        if (own == drawn) { return SluggerThemeError.Rejected(theme.Name, refusals); }

        return SluggerThemeError.RejectedAmongOthers(theme.Name, own, drawn, refusals);
    }

    /// <summary>
    ///     Puts every name the catalog serves where the wildcard stands, and names each theme once.
    /// </summary>
    /// <remarks>
    ///     Naming a theme once matters because <see cref="WeightedThemePicker" /> weighs a theme by its
    ///     size: one listed twice would be drawn twice as often as its vocabulary earns, which is
    ///     exactly what "*" beside a name it already covers would ask for.
    /// </remarks>
    /// <param name="requested">The names <c>--theme</c> put in scope, wildcard included.</param>
    /// <param name="catalog">The catalog whose names the wildcard stands for.</param>
    internal static string[] Expand(IReadOnlyList<string> requested, IThemeCatalog catalog) {
        if (!requested.Contains(ThemeSelection.EveryTheme, StringComparer.Ordinal)) { return [.. requested]; }

        // Distinct keeps the first of each, so the order asked for survives the expansion.
        return
        [
            .. requested
                .SelectMany(asked => asked == ThemeSelection.EveryTheme ? catalog.ListNames() : (IReadOnlyList<string>)[asked])
                .Distinct(StringComparer.Ordinal),
        ];
    }

    #endregion

    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore    Config      { get; } = config;
    private IClipboard      Clipboard   { get; } = clipboard;

    /// <summary>Generates <c>--count</c> slugs in one go.</summary>
    /// <param name="requested">What the command line asked for.</param>
    /// <param name="random">
    ///     Where the draws come from, for a caller that draws several batches - the interactive loop -
    ///     and keeps one source across them, so that a seeded session carries on with its sequence
    ///     rather than replaying its first batch on every round. Left out, the batch draws from a
    ///     source of its own, seeded from <c>--seed</c> when there is one.
    /// </param>
    internal Outcome<GeneratedSlugs> Execute(SluggerOptions requested, IRandomSource? random = null) {
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions? saved   = Config.Load();
        SluggerOptions  session = OptionResolver.Merge(requested, saved);

        Outcome<IReadOnlyList<ThemeDocument>> loaded = LoadThemesInScope(session);
        if (loaded.Error is { } refused) { return Outcome<GeneratedSlugs>.Failure(refused); }

        IReadOnlyList<ThemeDocument> themes   = loaded.GetResultOrThrow();
        Outcome<SlugGenerator>       prepared = Prepare(themes, requested, saved, session);
        if (prepared.Error is { } narrowed) { return Outcome<GeneratedSlugs>.Failure(narrowed); }

        SlugGenerator source = prepared.GetResultOrThrow();
        IRandomSource draws  = random ?? new DefaultRandomSource(session.Seed);

        // One source for the whole batch, so a seeded run replays every slug of it and not just
        // the first - picking the theme and drawing inside it come from the same sequence.
        List<string> slugs = [];
        for (int drawn = 0; drawn < Math.Max(1, session.Count ?? 1); drawn++) {
            slugs.Add(source.Next(draws));
        }

        string? notCopied = null;
        if (session.Clipboard == true && slugs.Count > 0) {
            // The last one, which is what a REPL round leaves on screen.
            notCopied = Clipboard.Copy(slugs[^1]);
        }

        return Outcome<GeneratedSlugs>.Success(new GeneratedSlugs(slugs, notCopied));
    }

    /// <summary>
    ///     Every theme <c>--theme</c> put in scope, or the default one when it said nothing. A single
    ///     refusal fails the batch: generating from the themes that did load would hide the broken one.
    /// </summary>
    private Outcome<IReadOnlyList<ThemeDocument>> LoadThemesInScope(SluggerOptions session) {
        IThemeCatalog catalog = Directories.CatalogFor(session.ThemeDirectory);

        string[] names = session.Themes is { Count: > 0 } requested
            ? Expand(requested, catalog)
            : [DefaultThemeName];

        List<ThemeDocument> themes = [];
        foreach (string name in names) {
            Outcome<ThemeDocument> loaded = catalog.Load(name, session.AllowSmallTheme ?? false);
            if (loaded.Error is { } refused) { return Outcome<IReadOnlyList<ThemeDocument>>.Failure(refused); }

            themes.Add(loaded.GetResultOrThrow());
        }

        return Outcome<IReadOnlyList<ThemeDocument>>.Success(themes);
    }

}