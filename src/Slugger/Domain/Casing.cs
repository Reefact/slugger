namespace Slugger.Domain;

/// <summary>
///     How the words of a slug are capitalised, and what joins them when nothing else says so.
/// </summary>
/// <remarks>
///     <para>
///         Each value names a complete convention - its capitalization and its joiner, <c>snake</c> writing
///         <c>focused_turing</c> on its own. <see cref="GenerationOptions.Separator" />, when it is set,
///         replaces the joiner and keeps the capitals: snake with no separator is
///         <c>focusedturing</c>, camel joined by <c>-</c> is <c>focused-Turing</c>. The four ways of
///         capitalising crossed with any joiner reach every convention there is, Train-Case and COBOL-CASE
///         included, without a value of their own.
///     </para>
///     <para>
///         The command line's <c>--casing</c>, and the <c>casing</c> key of a theme's <c>defaults</c>.
///         See decision record DEC0030 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0030-casse-et-separateur-orthogonaux.md
///     </para>
/// </remarks>
public enum Casing {

    /// <summary>Lowercase words joined by <c>-</c>: <c>gorgeous-wandering-khorana</c>.</summary>
    Kebab,

    /// <summary>Lowercase words joined by <c>_</c>: <c>focused_turing</c>, as Docker writes it.</summary>
    Snake,

    /// <summary>
    ///     Every word but the first capitalised, and nothing between them: <c>gorgeousWanderingKhorana</c>.
    /// </summary>
    Camel,

    /// <summary>
    ///     Uppercase words joined by <c>_</c>: <c>GORGEOUS_WANDERING_KHORANA</c>, the token's hexadecimal
    ///     digits included.
    /// </summary>
    Constant,

    /// <summary>Every word capitalised, and nothing between them: <c>GorgeousWanderingKhorana</c>.</summary>
    Pascal

}
