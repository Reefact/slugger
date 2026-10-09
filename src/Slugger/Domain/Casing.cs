namespace Slugger.Domain;

/// <summary>How the terms of a slug are joined and capitalised.</summary>
/// <remarks>The command line's <c>--casing</c>, and the <c>casing</c> key of a theme's <c>defaults</c>.</remarks>
public enum Casing {

    /// <summary>
    ///     Lowercase terms joined by <see cref="GenerationOptions.Separator" />: <c>gorgeous-wandering-khorana</c>.
    /// </summary>
    Kebab,

    /// <summary>
    ///     Lowercase terms joined by <see cref="GenerationOptions.Separator" />, which writes the same as
    ///     <see cref="Kebab" />: set the separator to <c>_</c> as well for <c>focused_turing</c>, as Docker
    ///     writes it.
    /// </summary>
    Snake,

    /// <summary>
    ///     No separator at all, each word but the first capitalised: <c>gorgeousWanderingKhorana</c>. A
    ///     token follows the last word directly.
    /// </summary>
    Camel

}