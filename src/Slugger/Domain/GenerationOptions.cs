namespace Slugger.Domain;

/// <summary>
///     The effective generation levers, once the precedence chain has already been collapsed
///     (explicit argument, theme defaults, saved config, program default - see
///     Slugger.Application.Options.OptionResolver). The domain receives the outcome of that
///     chain and never resolves it itself.
/// </summary>
public sealed record GenerationOptions {

    #region Static members

    /// <summary>What slugger does when nothing else says otherwise.</summary>
    public static GenerationOptions Default { get; } = new();

    #endregion

    /// <summary>Joins the terms, and the words of a compound one when nothing else says otherwise.</summary>
    public char Separator { get; init; } = '-';

    /// <summary>
    ///     What replaces the internal spaces of a compound value - <c>"john doe"</c>,
    ///     <c>
    ///         "coors
    ///         field"
    ///     </c>
    ///     - when it should not be <see cref="Separator" />. Null means it is; an empty
    ///     string glues the words together (<c>coorsfield</c>); anything else is used as written.
    /// </summary>
    /// <remarks>
    ///     Distinct from <see cref="Separator" />, the boundary between a slug's terms stays
    ///     readable in the text itself: <c>gorgeous-john_doe</c> says where the noun begins, where
    ///     <c>gorgeous-john-doe</c> leaves it to be guessed. Casing.Camel has nowhere to put either
    ///     separator, so this changes nothing there.
    /// </remarks>
    public string? WordSeparator { get; init; }

    /// <summary>
    ///     Drops the diacritic from a letter that carries one - <c>é</c> becomes <c>e</c>, <c>ç</c>
    ///     becomes <c>c</c> - when the slug has to survive somewhere its theme's alphabet does not.
    /// </summary>
    /// <remarks>
    ///     A theme writes its words as they are written; this is the consumer's lever, not the
    ///     theme's, for a use the theme cannot know about. It folds what decomposes, which is the
    ///     Latin alphabet's accents: a letter with no decomposition - <c>ß</c>, <c>ø</c>, <c>œ</c>,
    ///     and every non-Latin script - passes through as written. So it is a fold, never a promise
    ///     that the slug came out ASCII.
    /// </remarks>
    public bool FoldAccents { get; init; }

    /// <summary>
    ///     Forces the slug into ASCII, whatever it costs the words: accents are folded, and every
    ///     letter that is still not ASCII afterwards is dropped.
    /// </summary>
    /// <remarks>
    ///     Where <see cref="FoldAccents" /> folds what it can and leaves the rest as written, this
    ///     promises the result instead of the mechanism - so it disfigures rather than give up.
    ///     "straße" comes out "strae", still one word, and a value written in a script that folds to
    ///     nothing comes out empty and is dropped from the slug. That is the trade, and it is only
    ///     worth taking where the destination cannot accept anything else. It implies the fold, so
    ///     the two are never needed together.
    /// </remarks>
    public bool Ascii { get; init; }

    /// <summary>Shape of the assembled slug.</summary>
    public Casing Casing { get; init; } = Casing.Kebab;

    /// <summary>
    ///     The most characters the finished slug may carry, or null for no ceiling at all (DEC0018).
    /// </summary>
    /// <remarks>
    ///     The consumer's lever, not the theme's: a theme states what it promises and is refused at
    ///     load when it cannot keep it, while this reduces what a run draws from so that the promise
    ///     is kept whatever the theme says. It only ever removes words - a slug is never truncated,
    ///     and a word is never cut in the middle.
    /// </remarks>
    public int? MaxLength { get; init; }

    /// <summary>
    ///     The most words any one drawn value may carry, or null for no cap at all (DEC0023).
    /// </summary>
    /// <remarks>
    ///     The same lever as <see cref="MaxLength" /> aimed at a different unit, and it works the same
    ///     way: it removes a value from the pool rather than cut it down, so "speculative generality"
    ///     leaves the draw under a cap of one and never comes out as "speculative". A cap counts the
    ///     words of one segment, never of the slug - a noun of three words does not spend the
    ///     adjective's budget, it is simply not drawn.
    /// </remarks>
    public int? MaxSegmentWords { get; init; }

    /// <summary>Which words sit before the noun.</summary>
    public SegmentMode SegmentMode { get; init; } = SegmentMode.Both;

    /// <summary>Length of the trailing token. Zero means no token at all.</summary>
    public int TokenLength { get; init; }

    /// <summary>Draw the token in hexadecimal rather than decimal digits.</summary>
    public bool TokenHex { get; init; }

    /// <summary>Append the token with no separator before it.</summary>
    public bool TokenGlued { get; init; }

    /// <summary>
    ///     Percentage, 0 to 100, that a token is drawn at all. A value in between simulates a rare
    ///     suffix - Docker's collision digit - without implementing collision detection.
    /// </summary>
    public int TokenChance { get; init; } = 100;

    /// <summary>Seed for a reproducible run (<c>--seed</c>). Null draws from a time based source.</summary>
    public int? Seed { get; init; }

    /// <summary>
    ///     These options with the theme's own <c>defaults</c> laid over them, for the levers the theme
    ///     states an opinion on. What it says nothing about is left exactly as it was.
    /// </summary>
    /// <remarks>
    ///     This is the whole of the "single theme, defaults applied" case a library consumer needs -
    ///     <c>SlugGenerator.Generate(theme, GenerationOptions.Default.WithDefaultsOf(theme))</c>
    ///     reproduces the style of docker or heroku without transcribing their JSON by hand. The CLI's
    ///     precedence chain (explicit argument, theme defaults, saved config, program default) is a
    ///     different and larger question, and belongs to Slugger.Application.Options.OptionResolver.
    /// </remarks>
    /// <param name="theme">The theme whose style to adopt.</param>
    public GenerationOptions WithDefaultsOf(Theme theme) {
        ArgumentNullException.ThrowIfNull(theme);

        ThemeDefaults defaults = theme.Defaults;

        return this with {
            Separator = defaults.Separator             ?? Separator,
            WordSeparator = defaults.WordSeparator     ?? WordSeparator,
            FoldAccents = defaults.FoldAccents         ?? FoldAccents,
            Ascii = defaults.Ascii                     ?? Ascii,
            Casing = defaults.Casing                   ?? Casing,
            SegmentMode = defaults.SegmentMode         ?? SegmentMode,
            MaxSegmentWords = defaults.MaxSegmentWords ?? MaxSegmentWords,
            TokenLength = defaults.TokenLength         ?? TokenLength,
            TokenHex = defaults.TokenHex               ?? TokenHex,
            TokenGlued = defaults.TokenGlued           ?? TokenGlued,
            TokenChance = defaults.TokenChance         ?? TokenChance
        };
    }

}