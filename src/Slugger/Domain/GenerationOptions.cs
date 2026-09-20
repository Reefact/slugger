namespace Slugger.Domain;

/// <summary>
/// The effective generation levers, once the precedence chain has already been collapsed
/// (explicit argument, theme defaults, saved config, program default - see
/// Slugger.Application.Options.OptionResolver). The domain receives the outcome of that
/// chain and never resolves it itself.
/// </summary>
public sealed record GenerationOptions
{
    /// <summary>What slugger does when nothing else says otherwise.</summary>
    public static GenerationOptions Default { get; } = new();

    /// <summary>Joins the segments, and the words of a compound value when nothing else says otherwise.</summary>
    public char Separator { get; init; } = '-';

    /// <summary>
    /// What replaces the internal spaces of a compound value - <c>"john doe"</c>, <c>"coors
    /// field"</c> - when it should not be <see cref="Separator"/>. Null means it is; an empty
    /// string glues the words together (<c>coorsfield</c>); anything else is used as written.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="Separator"/>, the boundary between a slug's segments stays
    /// readable in the text itself: <c>gorgeous-john_doe</c> says where the noun begins, where
    /// <c>gorgeous-john-doe</c> leaves it to be guessed. Casing.Camel has nowhere to put either
    /// separator, so this changes nothing there.
    /// </remarks>
    public string? WordSeparator { get; init; }

    /// <summary>
    /// Drops the diacritic from a letter that carries one - <c>é</c> becomes <c>e</c>, <c>ç</c>
    /// becomes <c>c</c> - when the slug has to survive somewhere its theme's alphabet does not.
    /// </summary>
    /// <remarks>
    /// A theme writes its words as they are written; this is the consumer's lever, not the
    /// theme's, for a use the theme cannot know about. It folds what decomposes, which is the
    /// Latin alphabet's accents: a letter with no decomposition - <c>ß</c>, <c>ø</c>, <c>œ</c>,
    /// and every non-Latin script - passes through as written. So it is a fold, never a promise
    /// that the slug came out ASCII.
    /// </remarks>
    public bool FoldAccents { get; init; }

    /// <summary>Shape of the assembled slug.</summary>
    public Casing Casing { get; init; } = Casing.Kebab;

    /// <summary>Which words sit before the noun.</summary>
    public SegmentMode SegmentMode { get; init; } = SegmentMode.Both;

    /// <summary>Length of the trailing token. Zero means no token at all.</summary>
    public int TokenLength { get; init; }

    /// <summary>Draw the token in hexadecimal rather than decimal digits.</summary>
    public bool TokenHex { get; init; }

    /// <summary>Append the token with no separator before it.</summary>
    public bool TokenGlued { get; init; }

    /// <summary>
    /// Percentage, 0 to 100, that a token is drawn at all. A value in between simulates a rare
    /// suffix - Docker's collision digit - without implementing collision detection.
    /// </summary>
    public int TokenChance { get; init; } = 100;

    /// <summary>Seed for a reproducible run (<c>--seed</c>). Null draws from a time based source.</summary>
    public int? Seed { get; init; }

    /// <summary>
    /// These options with the theme's own <c>defaults</c> laid over them, for the levers the theme
    /// states an opinion on. What it says nothing about is left exactly as it was.
    /// </summary>
    /// <remarks>
    /// This is the whole of the "single theme, defaults applied" case a library consumer needs -
    /// <c>SlugGenerator.Generate(theme, GenerationOptions.Default.WithDefaultsOf(theme))</c>
    /// reproduces the style of docker or heroku without transcribing their JSON by hand. The CLI's
    /// precedence chain (explicit argument, theme defaults, saved config, program default) is a
    /// different and larger question, and belongs to Slugger.Application.Options.OptionResolver.
    /// </remarks>
    /// <param name="theme">The theme whose style to adopt.</param>
    public GenerationOptions WithDefaultsOf(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        ThemeDefaults defaults = theme.Defaults;

        return this with
        {
            Separator = defaults.Separator ?? Separator,
            WordSeparator = defaults.WordSeparator ?? WordSeparator,
            FoldAccents = defaults.FoldAccents ?? FoldAccents,
            Casing = defaults.Casing ?? Casing,
            SegmentMode = defaults.SegmentMode ?? SegmentMode,
            TokenLength = defaults.TokenLength ?? TokenLength,
            TokenHex = defaults.TokenHex ?? TokenHex,
            TokenGlued = defaults.TokenGlued ?? TokenGlued,
            TokenChance = defaults.TokenChance ?? TokenChance,
        };
    }
}
