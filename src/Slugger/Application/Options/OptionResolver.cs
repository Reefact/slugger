using Slugger.Domain;

namespace Slugger.Application.Options;

/// <summary>
/// Collapses the precedence chain into the <see cref="GenerationOptions"/> the domain
/// consumes:
/// <code>
/// explicit argument  &gt;  config saved by --init  &gt;  theme defaults  &gt;  program default
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The theme's defaults only join the chain when the choice of theme is unambiguous - a single
/// theme in scope - or when <see cref="MimicStyle.Force"/> puts them back in for a multi-theme
/// run. What arms them is the number of active themes, not the flag: <c>slugger --theme heroku</c>
/// alone already reproduces heroku's style, since a config that says nothing lets the theme
/// through untouched.
/// </para>
/// <para>
/// The saved config sits <b>above</b> the theme, where an earlier reading of the spec put it
/// below. Measured, on the themes actually shipped: each of the three declares five to seven of
/// the seven format levers, so a theme below the config was a theme that voided it - and --init
/// then had no effect on formatting in any single-theme run, which is every ordinary run. The
/// theme now fills in what neither the command line nor the user ever stated, which is what a
/// default is.
/// </para>
/// </remarks>
internal static class OptionResolver
{
    /// <param name="commandLine">What this invocation asked for explicitly.</param>
    /// <param name="saved">What --init persisted, or null.</param>
    /// <param name="drawnTheme">The theme actually drawn, whose defaults may apply.</param>
    /// <param name="themesInScope">How many themes are active, which is what arms the automatic behaviour.</param>
    internal static GenerationOptions Resolve(
        SluggerOptions commandLine,
        SluggerOptions? saved,
        Theme drawnTheme,
        int themesInScope)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(drawnTheme);

        // Laid on in reverse order of precedence, so each layer overwrites the weaker one below.
        GenerationOptions options = GenerationOptions.Default;

        // --mimic-style is an option like any other, so --init can save it: the flag decides
        // whether the theme speaks, and the command line only outranks the config in saying so.
        if (AppliesTheStyleOf(commandLine.MimicStyle ?? saved?.MimicStyle, themesInScope))
        {
            options = options.WithDefaultsOf(drawnTheme);
        }

        return LayOver(LayOver(options, saved), commandLine);
    }

    /// <summary>
    /// Merges the command line over the saved config, for the options that are not per theme -
    /// which themes are in scope, how many slugs, the seed, the REPL, the clipboard.
    /// </summary>
    /// <param name="commandLine">What this invocation asked for explicitly.</param>
    /// <param name="saved">What --init persisted, or null.</param>
    internal static SluggerOptions Merge(SluggerOptions commandLine, SluggerOptions? saved)
    {
        ArgumentNullException.ThrowIfNull(commandLine);

        if (saved is null)
        {
            return commandLine;
        }

        return new SluggerOptions
        {
            Themes = commandLine.Themes ?? saved.Themes,
            ThemeDirectory = commandLine.ThemeDirectory ?? saved.ThemeDirectory,
            Separator = commandLine.Separator ?? saved.Separator,
            Casing = commandLine.Casing ?? saved.Casing,
            SegmentMode = commandLine.SegmentMode ?? saved.SegmentMode,
            TokenLength = commandLine.TokenLength ?? saved.TokenLength,
            TokenHex = commandLine.TokenHex ?? saved.TokenHex,
            TokenChance = commandLine.TokenChance ?? saved.TokenChance,
            TokenGlued = commandLine.TokenGlued ?? saved.TokenGlued,
            Count = commandLine.Count ?? saved.Count,
            Seed = commandLine.Seed ?? saved.Seed,
            Oneshot = commandLine.Oneshot ?? saved.Oneshot,
            Clipboard = commandLine.Clipboard ?? saved.Clipboard,
            MimicStyle = commandLine.MimicStyle ?? saved.MimicStyle,
            AllowSmallTheme = commandLine.AllowSmallTheme ?? saved.AllowSmallTheme,
        };
    }

    /// <summary>
    /// Whether the drawn theme's own defaults apply. Absent, the flag defers to how many themes
    /// are active; present, it decides outright in either direction.
    /// </summary>
    /// <param name="mimicStyle">The three-state flag, or null when it was not passed.</param>
    /// <param name="themesInScope">How many themes are active.</param>
    internal static bool AppliesTheStyleOf(MimicStyle? mimicStyle, int themesInScope) => mimicStyle switch
    {
        MimicStyle.Force => true,
        MimicStyle.Off => false,
        _ => themesInScope == 1,
    };

    private static GenerationOptions LayOver(GenerationOptions options, SluggerOptions? layer)
    {
        if (layer is null)
        {
            return options;
        }

        return options with
        {
            Separator = layer.Separator ?? options.Separator,
            Casing = layer.Casing ?? options.Casing,
            SegmentMode = layer.SegmentMode ?? options.SegmentMode,
            TokenLength = layer.TokenLength ?? options.TokenLength,
            TokenHex = layer.TokenHex ?? options.TokenHex,
            TokenGlued = layer.TokenGlued ?? options.TokenGlued,
            TokenChance = layer.TokenChance ?? options.TokenChance,
            Seed = layer.Seed ?? options.Seed,
        };
    }
}
