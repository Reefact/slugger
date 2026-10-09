namespace Slugger.Domain;

/// <summary>What precedes the noun in a slug: which epithet, of how many terms.</summary>
/// <remarks>
///     <para>
///         A mode asks; the theme answers with what each noun reaches. A noun that reaches no participle
///         gets an adjective instead, whatever the mode: under <see cref="Both" />, its slug is then one
///         term shorter.
///     </para>
///     <para>
///         The command line's <c>--segment</c>, and the <c>segmentMode</c> key of a theme's
///         <c>defaults</c>. The name says "segment" for historical reasons: a mode counts terms, and a
///         compound term is written as several segments.
///     </para>
/// </remarks>
public enum SegmentMode {

    /// <summary>An adjective only. Participles are never drawn, even when the theme declares some.</summary>
    Adjective,

    /// <summary>A participle only, from those the noun reaches.</summary>
    Participle,

    /// <summary>
    ///     One term, drawn from the adjectives and participles the noun reaches taken together, so that
    ///     every word has the same chance whichever list declares it. The single word before the noun
    ///     of Docker and Heroku names.
    /// </summary>
    Either,

    /// <summary>An adjective, then a participle: three terms in all. The library's default.</summary>
    Both,

    /// <summary>
    ///     An adjective, then a participle or nothing. The participle is drawn among the noun's
    ///     participles plus one more candidate, the absence of a participle: a noun reaching 25
    ///     participles draws among 26, and the extra one gives a slug of two terms.
    /// </summary>
    /// <remarks>
    ///     See decision record DEC0020 (in French):
    ///     https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0020-absence-de-participe-tiree-comme-un-participe-de-plus.md
    /// </remarks>
    ThreeOrTwo

}