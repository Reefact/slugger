using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;

namespace Slugger.Application.UseCases;

/// <summary>
/// The main path: resolve the themes in scope, draw, format, optionally copy. Drives the
/// REPL and the oneshot mode alike - the loop belongs to the CLI, a batch of slugs belongs
/// here.
/// </summary>
internal sealed class GenerateSlugsUseCase(IThemeDirectory directories, IConfigStore config, IClipboard clipboard)
{
    /// <summary>The only theme in scope when <c>--theme</c> says nothing.</summary>
    internal const string DefaultThemeName = "slugger";

    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore Config { get; } = config;
    private IClipboard Clipboard { get; } = clipboard;

    /// <summary>Generates <c>--count</c> slugs in one go.</summary>
    /// <param name="requested">What the command line asked for.</param>
    internal Outcome<IReadOnlyList<string>> Execute(SluggerOptions requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions? saved = Config.Load();
        SluggerOptions session = OptionResolver.Merge(requested, saved);

        Outcome<IReadOnlyList<Theme>> loaded = LoadThemesInScope(session);
        if (loaded.Error is { } refused)
        {
            return Outcome<IReadOnlyList<string>>.Failure(refused);
        }

        IReadOnlyList<Theme> themes = loaded.GetResultOrThrow();
        WeightedThemePicker picker = new(themes);
        IRandomSource random = new DefaultRandomSource(session.Seed);

        // One source for the whole batch, so a seeded run replays every slug of it and not just
        // the first - picking the theme and drawing inside it come from the same sequence.
        List<string> slugs = [];
        for (int drawn = 0; drawn < Math.Max(1, session.Count ?? 1); drawn++)
        {
            Theme theme = picker.Pick(random);
            GenerationOptions options = OptionResolver.Resolve(requested, saved, theme, themes.Count);
            slugs.Add(SlugGenerator.Generate(theme, options, random));
        }

        if (session.Clipboard == true && slugs.Count > 0)
        {
            // The last one, which is what a REPL round leaves on screen.
            Clipboard.Copy(slugs[^1]);
        }

        return Outcome<IReadOnlyList<string>>.Success(slugs);
    }

    /// <summary>
    /// Every theme <c>--theme</c> put in scope, or the default one when it said nothing. A single
    /// refusal fails the batch: generating from the themes that did load would hide the broken one.
    /// </summary>
    private Outcome<IReadOnlyList<Theme>> LoadThemesInScope(SluggerOptions session)
    {
        IThemeCatalog catalog = Directories.CatalogFor(session.ThemeDirectory);

        string[] names = session.Themes is { Count: > 0 } requested
            ? [.. requested]
            : [DefaultThemeName];

        List<Theme> themes = [];
        foreach (string name in names)
        {
            Outcome<Theme> loaded = catalog.Load(name, session.AllowSmallTheme ?? false);
            if (loaded.Error is { } refused)
            {
                return Outcome<IReadOnlyList<Theme>>.Failure(refused);
            }

            themes.Add(loaded.GetResultOrThrow());
        }

        return Outcome<IReadOnlyList<Theme>>.Success(themes);
    }
}
