#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
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
    ///     per slug: a length budget reduces the pools, and reducing them a thousand times for
    ///     <c>--count 1000</c> would be a thousand times the work for the same answer.
    /// </summary>
    /// <remarks>
    ///     This is also where a run is judged against what it actually draws. A theme reduced by
    ///     <c>--max-length</c> is a theme like any other (DEC0018): it clears the size rules or the
    ///     run is refused, naming what it no longer reaches. A <c>--segment</c> narrows nothing, but
    ///     it changes which floors apply (DEC0016) - and the load measured the theme's own mode, not
    ///     this one. Either way the theme that loaded is not the theme being drawn from, so it is
    ///     checked again; a run that changes neither was already validated when it loaded.
    /// </remarks>
    private static Outcome<IReadOnlyDictionary<Theme, Drawing>> Prepare(IReadOnlyList<Theme> themes,
                                                                        SluggerOptions       requested,
                                                                        SluggerOptions?      saved,
                                                                        SluggerOptions       session) {
        Dictionary<Theme, Drawing> drawing = [];
        foreach (Theme theme in themes) {
            GenerationOptions options  = OptionResolver.Resolve(requested, saved, theme, themes.Count);
            ThemeResolver     resolver = SlugGenerator.ResolverFor(theme, options);

            if (resolver.Narrows || ThemeValidator.DrawnMode(resolver) != ThemeValidator.DrawnMode(theme)) {
                IReadOnlyList<DomainError> refusals =
                    ThemeValidator.Validate(resolver, session.AllowSmallTheme ?? false);
                if (refusals.Count > 0) {
                    return Outcome<IReadOnlyDictionary<Theme, Drawing>>.Failure(
                        ThemeErrors.Rejected(theme.Name, refusals));
                }
            }

            drawing[theme] = new Drawing(options, resolver);
        }

        return Outcome<IReadOnlyDictionary<Theme, Drawing>>.Success(drawing);
    }

    #endregion

    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore    Config      { get; } = config;
    private IClipboard      Clipboard   { get; } = clipboard;

    /// <summary>Generates <c>--count</c> slugs in one go.</summary>
    /// <param name="requested">What the command line asked for.</param>
    internal Outcome<IReadOnlyList<string>> Execute(SluggerOptions requested) {
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions? saved   = Config.Load();
        SluggerOptions  session = OptionResolver.Merge(requested, saved);

        Outcome<IReadOnlyList<Theme>> loaded = LoadThemesInScope(session);
        if (loaded.Error is { } refused) {
            return Outcome<IReadOnlyList<string>>.Failure(refused);
        }

        IReadOnlyList<Theme>                         themes   = loaded.GetResultOrThrow();
        Outcome<IReadOnlyDictionary<Theme, Drawing>> prepared = Prepare(themes, requested, saved, session);
        if (prepared.Error is { } narrowed) {
            return Outcome<IReadOnlyList<string>>.Failure(narrowed);
        }

        IReadOnlyDictionary<Theme, Drawing> drawing = prepared.GetResultOrThrow();
        WeightedThemePicker                 picker  = new(themes);
        IRandomSource                       random  = new DefaultRandomSource(session.Seed);

        // One source for the whole batch, so a seeded run replays every slug of it and not just
        // the first - picking the theme and drawing inside it come from the same sequence.
        List<string> slugs = [];
        for (int drawn = 0; drawn < Math.Max(1, session.Count ?? 1); drawn++) {
            (GenerationOptions options, ThemeResolver resolver) = drawing[picker.Pick(random)];
            slugs.Add(SlugGenerator.Generate(resolver, options, random));
        }

        if (session.Clipboard == true && slugs.Count > 0) {
            // The last one, which is what a REPL round leaves on screen.
            Clipboard.Copy(slugs[^1]);
        }

        return Outcome<IReadOnlyList<string>>.Success(slugs);
    }

    /// <summary>
    ///     Every theme <c>--theme</c> put in scope, or the default one when it said nothing. A single
    ///     refusal fails the batch: generating from the themes that did load would hide the broken one.
    /// </summary>
    private Outcome<IReadOnlyList<Theme>> LoadThemesInScope(SluggerOptions session) {
        IThemeCatalog catalog = Directories.CatalogFor(session.ThemeDirectory);

        string[] names = session.Themes is { Count: > 0 } requested
            ? [.. requested]
            : [DefaultThemeName];

        List<Theme> themes = [];
        foreach (string name in names) {
            Outcome<Theme> loaded = catalog.Load(name, session.AllowSmallTheme ?? false);
            if (loaded.Error is { } refused) {
                return Outcome<IReadOnlyList<Theme>>.Failure(refused);
            }

            themes.Add(loaded.GetResultOrThrow());
        }

        return Outcome<IReadOnlyList<Theme>>.Success(themes);
    }

    #region Nested types

    /// <summary>What a theme draws with, settled once for the whole batch.</summary>
    /// <param name="Options">The precedence chain collapsed for this theme.</param>
    /// <param name="Resolver">Its surface, narrowed to the run's length budget where there is one.</param>
    private sealed record Drawing(GenerationOptions Options, ThemeResolver Resolver);

    #endregion

}