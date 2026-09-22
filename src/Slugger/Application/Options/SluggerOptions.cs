#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.Application.Options;

/// <summary>
///     One layer of the precedence chain, as requested rather than as resolved: the explicit
///     command line, or the config saved by <c>--init</c>. Null always means "this layer says
///     nothing about it", which is what lets a lower layer speak.
/// </summary>
internal sealed record SluggerOptions {

    #region Static members

    /// <summary>A layer that asks for nothing.</summary>
    public static SluggerOptions Empty { get; } = new();

    #endregion

    /// <summary>The themes in scope (<c>--theme</c>). More than one arms the weighted draw.</summary>
    public IReadOnlyList<string>? Themes { get; init; }

    /// <summary>Where to look for theme files (<c>--theme-dir</c>).</summary>
    public string? ThemeDirectory { get; init; }

    /// <summary>The separator (<c>--sep</c>).</summary>
    public char? Separator { get; init; }

    /// <summary>What joins the words of a compound value (<c>--word-sep</c>), empty to glue them.</summary>
    public string? WordSeparator { get; init; }

    /// <summary>Folds a letter's diacritic away (<c>--fold-accents</c>).</summary>
    public bool? FoldAccents { get; init; }

    /// <summary>Forces an ASCII slug, whatever it costs (<c>--ascii</c>).</summary>
    public bool? Ascii { get; init; }

    /// <summary>The output shape (<c>--casing</c>).</summary>
    public Casing? Casing { get; init; }

    /// <summary>Which words sit before the noun (<c>--segment</c>).</summary>
    public SegmentMode? SegmentMode { get; init; }

    /// <summary>Length of the trailing token (<c>--token-length</c>).</summary>
    public int? TokenLength { get; init; }

    /// <summary>Whether the token is hexadecimal (<c>--token-hex</c>).</summary>
    public bool? TokenHex { get; init; }

    /// <summary>How often the token appears, as a percentage (<c>--token-chance</c>).</summary>
    public int? TokenChance { get; init; }

    /// <summary>Whether the token is glued to the previous segment (<c>--token-glued</c>).</summary>
    public bool? TokenGlued { get; init; }

    /// <summary>
    ///     The most characters a slug may carry (<c>--max-length</c>). The consumer's ceiling, which
    ///     narrows the surface a run draws from rather than trimming what it produced (DEC0018).
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    ///     The most words a single segment may carry (<c>--max-segment-words</c>). The same shape of
    ///     lever as <see cref="MaxLength" /> on a different unit: it narrows the surface a run draws
    ///     from rather than shortening a value it drew (DEC0023). Nullable twice over rather than a
    ///     bare <c>int?</c>: the outer null is this layer's ordinary silence, and
    ///     <see cref="SegmentWordsCap.None" /> is this layer explicitly asking for no cap at all,
    ///     which a theme's own <c>defaults</c> cannot be told apart from by omission alone (DEC0024).
    /// </summary>
    public SegmentWordsCap? MaxSegmentWords { get; init; }

    /// <summary>How many slugs one round generates (<c>--count</c>).</summary>
    public int? Count { get; init; }

    /// <summary>Seed for a reproducible run (<c>--seed</c>).</summary>
    public int? Seed { get; init; }

    /// <summary>Generate once and quit rather than staying in the REPL (<c>--oneshot</c>).</summary>
    public bool? Oneshot { get; init; }

    /// <summary>Copy each generated slug to the clipboard (<c>--clipboard</c>).</summary>
    public bool? Clipboard { get; init; }

    /// <summary>Whether the drawn theme's own defaults apply (<c>--mimic-style</c>).</summary>
    public MimicStyle? MimicStyle { get; init; }

    /// <summary>Accept a theme below the minimum size rules for this run (<c>--allow-small-theme</c>).</summary>
    public bool? AllowSmallTheme { get; init; }

}