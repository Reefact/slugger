namespace Slugger.Domain;

/// <summary>
///     How a slug is drawn and how it is written out: what precedes the noun, the separator, the
///     casing, accents, the token and the length. An immutable record: start from
///     <see cref="Default" /> and change what you need with a <c>with</c> expression.
/// </summary>
/// <remarks>
///     <para>
///         A theme's own style - the <c>defaults</c> block of its file, in
///         <see cref="ThemeDocument.Defaults" /> - is never applied on its own. <see cref="WithDefaultsOf" />
///         lays it over these options; without that call, every theme is drawn with the values set here.
///     </para>
///     <para>
///         The values are taken as they are. Only <see cref="MaxLength" /> and <see cref="MaxSegmentWords" />
///         refuse a value, and only when a slug is generated; every other property accepts anything its
///         type can hold, as each one says.
///     </para>
///     <para>
///         The command line builds these options from its own order of precedence - its flags, then the
///         theme's style, then saved defaults, then built-in defaults. The library takes them as given.
///     </para>
/// </remarks>
public sealed record GenerationOptions {

    #region Static members

    /// <summary>
    ///     The library's defaults: kebab case joined by <c>-</c>, an adjective and a participle before
    ///     the noun (<see cref="SegmentMode.Both" />), no token and no length limit.
    /// </summary>
    public static GenerationOptions Default { get; } = new();

    #endregion

    /// <summary>
    ///     The character written between two terms, and between the words of a compound term unless
    ///     <see cref="WordSeparator" /> says otherwise. <c>-</c> by default.
    /// </summary>
    /// <remarks>
    ///     Any character is accepted as it is, a space included. <see cref="Casing.Camel" /> writes no
    ///     separator, so this changes nothing there.
    /// </remarks>
    public char Separator { get; init; } = '-';

    /// <summary>
    ///     What replaces the space inside a compound term such as <c>"john doe"</c>, when it should differ
    ///     from <see cref="Separator" />. Null, the default, uses the separator; an empty string glues the
    ///     words together (<c>johndoe</c>); any other string is written as it is.
    /// </summary>
    /// <remarks>
    ///     A separator of its own keeps the boundary between terms readable: <c>gorgeous-john_doe</c> says
    ///     where the noun begins, where <c>gorgeous-john-doe</c> leaves it to be guessed.
    ///     <see cref="Casing.Camel" /> writes no separator at all, so this changes nothing there.
    /// </remarks>
    public string? WordSeparator { get; init; }

    /// <summary>
    ///     Drops the diacritic from a letter that has one - <c>é</c> becomes <c>e</c>, <c>ç</c>
    ///     becomes <c>c</c> - when the slug has to survive somewhere its theme's alphabet does not.
    /// </summary>
    /// <remarks>
    ///     A theme writes its words as they are written; this is your setting, not the theme's, for a use
    ///     the theme cannot know about. It folds what decomposes, which is the Latin alphabet's accents: a
    ///     letter with no decomposition - <c>ß</c>, <c>ø</c>, <c>œ</c> and every non-Latin script - is
    ///     kept as written. So it is a fold, never a promise that the slug comes out ASCII: for that,
    ///     use <see cref="Ascii" />.
    /// </remarks>
    public bool FoldAccents { get; init; }

    /// <summary>
    ///     Forces the slug into ASCII, whatever it costs the words: accents are folded, and every
    ///     letter that is still not ASCII afterwards is dropped.
    /// </summary>
    /// <remarks>
    ///     Where <see cref="FoldAccents" /> folds what it can and keeps the rest as written, this
    ///     promises the result instead of the method - so it disfigures a word rather than give up.
    ///     "straße" comes out "strae", still one word, and a term written in a script that folds to
    ///     nothing comes out empty and is left out of the slug. That is the trade, and it is only
    ///     worth taking where the destination accepts nothing else, such as a DNS label. It implies the
    ///     fold, so the two are never needed together.
    /// </remarks>
    public bool Ascii { get; init; }

    /// <summary>
    ///     How the terms are joined and capitalised. <see cref="Casing.Kebab" /> by default.
    /// </summary>
    /// <remarks>
    ///     <see cref="Casing.Kebab" /> and <see cref="Casing.Snake" /> write the same thing: the character
    ///     between terms is always <see cref="Separator" />. For snake case, set the separator to <c>_</c>
    ///     as well.
    /// </remarks>
    public Casing Casing { get; init; } = Casing.Kebab;

    /// <summary>
    ///     The most characters the finished slug may have, token included, or null - the default - for no
    ///     limit. Words that would not fit are left out of the draw: a slug is never truncated, and a word
    ///     is never cut.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Characters are counted as <see cref="string.Length" /> counts them, in UTF-16 code units,
    ///         not in bytes: <c>é</c> counts once. Set <see cref="Ascii" /> as well when the limit is in
    ///         bytes. The token is counted at its full length even when <see cref="TokenChance" /> makes it
    ///         rare.
    ///     </para>
    ///     <para>
    ///         <b>It costs milliseconds per slug, and it is not checked.</b> Every call that sets it rebuilds
    ///         the theme's pools without the words that do not fit, where a slug without it takes
    ///         microseconds. Nothing then checks that what is left still clears the floors a theme must
    ///         clear at load: a low limit leaves a thin vocabulary whose slugs repeat sooner, and one that
    ///         leaves no noun at all makes generation throw. Check a limit once, at startup: build a
    ///         <see cref="Resolution.ThemeResolver" /> with a <see cref="Generation.SlugBudget" /> and pass it
    ///         to <see cref="Validation.ThemeValidator.Validate(Resolution.ThemeResolver, bool)" />.
    ///     </para>
    ///     <para>
    ///         Zero or a negative value makes generation throw an <see cref="ArgumentOutOfRangeException" />.
    ///     </para>
    ///     <para>
    ///         The command line's <c>--max-length</c>, which does run that check before its first draw.
    ///         See decision record DEC0018 (in French):
    ///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md
    ///     </para>
    /// </remarks>
    public int? MaxLength { get; init; }

    /// <summary>
    ///     The most words any one term may have, or null - the default - for no limit. A term with more
    ///     words is left out of the draw, never shortened: under a limit of one, "speculative generality"
    ///     is not drawn at all, and never comes out as "speculative".
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It counts the words of one term, never of the slug: a noun of three words does not use up
    ///         the adjective's share, it is simply not drawn.
    ///     </para>
    ///     <para>
    ///         Like <see cref="MaxLength" />, it rebuilds the theme's pools on every call - a fraction of a
    ///         millisecond to a few milliseconds per slug - and nothing checks that the theme left still
    ///         clears the floors. The startup check described there covers it too. A theme whose style
    ///         states <c>maxSegmentWords</c> sets it through <see cref="WithDefaultsOf" />.
    ///     </para>
    ///     <para>
    ///         Zero or a negative value makes generation throw an <see cref="ArgumentOutOfRangeException" />.
    ///     </para>
    ///     <para>
    ///         The command line's <c>--max-segment-words</c>. See decision record DEC0023 (in French):
    ///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0023-plafond-de-mots-par-segment.md
    ///     </para>
    /// </remarks>
    public int? MaxSegmentWords { get; init; }

    /// <summary>
    ///     What precedes the noun. <see cref="SegmentMode.Both" /> by default: an adjective and a
    ///     participle, so three terms in all.
    /// </summary>
    /// <remarks>
    ///     Not checked against the theme: a mode the theme cannot feed degrades silently - a theme with no
    ///     participle draws an adjective instead - and the floors a theme cleared at load were measured for
    ///     its own mode, not this one. The startup check described on <see cref="MaxLength" /> measures
    ///     this mode too.
    /// </remarks>
    public SegmentMode SegmentMode { get; init; } = SegmentMode.Both;

    /// <summary>How many characters the token at the end has. Zero, the default, draws no token.</summary>
    /// <remarks>Not checked: a negative value draws no token, as zero does.</remarks>
    public int TokenLength { get; init; }

    /// <summary>Draws the token from hexadecimal digits (<c>0-9a-f</c>) rather than decimal ones.</summary>
    public bool TokenHex { get; init; }

    /// <summary>
    ///     Writes the token straight after the noun, with no separator before it, as Docker does.
    ///     <see cref="Casing.Camel" /> always does.
    /// </summary>
    public bool TokenGlued { get; init; }

    /// <summary>
    ///     Out of a hundred slugs, how many get a token when <see cref="TokenLength" /> is above zero. The
    ///     default, 100, gives every slug one. A low value imitates a rare suffix, such as the digit
    ///     Docker sometimes adds, without checking for collisions.
    /// </summary>
    /// <remarks>
    ///     Not checked against 0 to 100: a value of 100 or more always draws a token, and zero or less
    ///     never does.
    /// </remarks>
    public int TokenChance { get; init; } = 100;

    /// <summary>
    ///     The seed the two-argument <see cref="Generation.SlugGenerator.Generate(ThemeDocument, GenerationOptions)" />
    ///     uses, or null - the default - to draw from a shared, time-seeded source.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Every call reseeds.</b> That overload builds a new random source from this seed on each
    ///         call, so the same theme and the same options give the <b>same slug every time</b>. For a
    ///         reproducible sequence, create one <see cref="DefaultRandomSource" /> with the seed and pass
    ///         it to every call of
    ///         <see cref="Generation.SlugGenerator.Generate(ThemeDocument, GenerationOptions, IRandomSource)" />.
    ///         The overloads that take a random source ignore this property.
    ///     </para>
    ///     <para>
    ///         A seed reproduces a sequence for one version of the library and one version of the theme.
    ///         It is not promised across versions: a change to the theme or to the order of the draws
    ///         changes what a seed gives.
    ///     </para>
    ///     <para>
    ///         The command line's <c>--seed</c> shares one source across its run, as described above, so
    ///         <c>new DefaultRandomSource(42)</c> reproduces <c>--seed 42</c>.
    ///     </para>
    /// </remarks>
    public int? Seed { get; init; }

    /// <summary>
    ///     These options with the theme's own style laid over them - the <c>defaults</c> block of its
    ///     file, in <see cref="ThemeDocument.Defaults" />. Every option the theme states is replaced;
    ///     every option it says nothing about keeps its value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A theme's style is never applied otherwise. Call this first and change options afterwards:
    ///         <c>GenerationOptions.Default.WithDefaultsOf(theme) with { TokenLength = 4 }</c> keeps your
    ///         token length, whereas <c>(options with { TokenLength = 4 }).WithDefaultsOf(theme)</c> lets the
    ///         theme override it.
    ///     </para>
    ///     <para>
    ///         The theme's length promise, <see cref="ThemeDocument.MaxLength" />, is not an option and is
    ///         not copied into <see cref="MaxLength" />. A style that states <c>maxSegmentWords</c> sets
    ///         <see cref="MaxSegmentWords" />, with the cost described there.
    ///     </para>
    ///     <para>
    ///         The command line applies a theme's style when that theme alone is drawn. Drawing from
    ///         several themes with one set of options applies none of their styles.
    ///     </para>
    /// </remarks>
    /// <param name="theme">The theme whose style to adopt.</param>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> is null.</exception>
    public GenerationOptions WithDefaultsOf(ThemeDocument theme) {
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