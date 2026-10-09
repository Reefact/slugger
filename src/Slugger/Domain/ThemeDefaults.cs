namespace Slugger.Domain;

/// <summary>
///     A theme's own style: the <c>defaults</c> block of its file. Every member is optional, and
///     <c>null</c> always means "this theme says nothing about it" - never a value.
/// </summary>
/// <remarks>
///     <para>
///         Generation never applies it on its own:
///         <see cref="GenerationOptions.WithDefaultsOf" /> lays it over a set of options, replacing only
///         what the theme states.
///     </para>
///     <para>
///         Only what makes a style recognisable belongs here. How many slugs to draw, the seed or the
///         themes to draw from belong to the caller, and are deliberately absent.
///     </para>
/// </remarks>
public sealed record ThemeDefaults {

    #region Static members

    /// <summary>A theme that states no preference at all.</summary>
    public static ThemeDefaults Empty { get; } = new();

    #endregion

    /// <summary>
    ///     The separator between terms - <c>sep</c> in the file. Docker writes <c>_</c>, Heroku <c>-</c>.
    ///     See <see cref="GenerationOptions.Separator" />.
    /// </summary>
    public char? Separator { get; init; }

    /// <summary>
    ///     What joins the words of a compound term, when the style wants something other than
    ///     <see cref="Separator" /> - <c>wordSep</c> in the file. An empty string glues them. See
    ///     <see cref="GenerationOptions.WordSeparator" />.
    /// </summary>
    public string? WordSeparator { get; init; }

    /// <summary>Whether accents are folded; rarely a theme's business. See <see cref="GenerationOptions.FoldAccents" />.</summary>
    public bool? FoldAccents { get; init; }

    /// <summary>Whether the slug is forced into ASCII; rarely a theme's business. See <see cref="GenerationOptions.Ascii" />.</summary>
    public bool? Ascii { get; init; }

    /// <summary>
    ///     How the terms are joined and capitalised, kept consistent with the style's separator. See
    ///     <see cref="GenerationOptions.Casing" />.
    /// </summary>
    public Casing? Casing { get; init; }

    /// <summary>
    ///     The length of the token at the end: Heroku ends on four digits, Docker on one. See
    ///     <see cref="GenerationOptions.TokenLength" />.
    /// </summary>
    /// <remarks>Not checked at load: a negative value is accepted and draws no token.</remarks>
    public int? TokenLength { get; init; }

    /// <summary>Whether that token is hexadecimal rather than decimal.</summary>
    public bool? TokenHex { get; init; }

    /// <summary>
    ///     Out of a hundred slugs, how many get a token when <see cref="TokenLength" /> is above zero. See
    ///     <see cref="GenerationOptions.TokenChance" />.
    /// </summary>
    /// <remarks>Not checked at load: a value above 100 is accepted and always draws a token.</remarks>
    public int? TokenChance { get; init; }

    /// <summary>
    ///     Whether the token is written straight after the noun, with no separator, as Docker does. See
    ///     <see cref="GenerationOptions.TokenGlued" />.
    /// </summary>
    public bool? TokenGlued { get; init; }

    /// <summary>
    ///     What precedes the noun: how many terms, and whether adjectives or participles. See
    ///     <see cref="GenerationOptions.SegmentMode" />.
    /// </summary>
    public SegmentMode? SegmentMode { get; init; }

    /// <summary>
    ///     The most words a single term may have, where the style is built on short terms. See
    ///     <see cref="GenerationOptions.MaxSegmentWords" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A style setting rather than a caller's one: how wide a term runs is as much part of a
    ///         theme's look as its separator. Setting <see cref="GenerationOptions.MaxSegmentWords" />
    ///         after <see cref="GenerationOptions.WithDefaultsOf" /> overrides it.
    ///     </para>
    ///     <para>
    ///         Unlike the other members, a theme's own value also applies when the theme is loaded: its
    ///         floors are measured on the terms short enough for it. A file holding zero or a negative
    ///         value is refused at load, with the other reasons.
    ///     </para>
    ///     <para>
    ///         See decision record DEC0023 (in French):
    ///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0023-plafond-de-mots-par-segment.md
    ///     </para>
    /// </remarks>
    public int? MaxSegmentWords { get; init; }

}