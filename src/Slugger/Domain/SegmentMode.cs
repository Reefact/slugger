namespace Slugger.Domain;

/// <summary>
///     What sits between the optional prefix and the noun (<c>--segment</c>, or the theme's
///     <c>defaults.segmentMode</c>). Five values rather than a visibility boolean.
/// </summary>
public enum SegmentMode {

    /// <summary>An adjective only. Participles are never used, even when declared.</summary>
    Adjective,

    /// <summary>A participle only, drawn from partPool(noun).</summary>
    Participle,

    /// <summary>One or the other, decided per generation. Reproduces the single word prefix of Heroku and Docker.</summary>
    Either,

    /// <summary>Both, the three segment format slugger uses by default.</summary>
    Both,

    /// <summary>
    ///     An adjective, and a participle drawn from a pool holding one more candidate than the
    ///     theme declares: the absence of a participle (DEC0020). A noun reaching 25 participles
    ///     draws over 26, and the extra one writes a two segment slug.
    /// </summary>
    ThreeOrTwo

}