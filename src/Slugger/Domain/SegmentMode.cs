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

/// <summary>What a mode does, asked where several of them answer the same way.</summary>
internal static class SegmentModes {

    #region Static members

    /// <summary>
    ///     Whether this mode can put a participle beside an adjective, which is what decides the
    ///     per-noun floors (DEC0016), the pairs of DEC0017 and the shape the report measures.
    /// </summary>
    /// <remarks>
    ///     Distinct from <see cref="AlwaysDrawsTwoWords" />, which asks whether a mode draws two
    ///     words <i>always</i>: "threeOrTwo" may draw one, and that difference is what lets a
    ///     ceiling keep an adjective it would otherwise have refused.
    /// </remarks>
    /// <param name="mode">The mode to ask about.</param>
    internal static bool PutsAParticipleBesideAnAdjective(this SegmentMode mode) {
        return mode is SegmentMode.Both or SegmentMode.ThreeOrTwo;
    }

    /// <summary>
    ///     Whether this mode draws two words rather than one, <b>always</b>, which is what decides
    ///     whether a ceiling has to keep room in front of the noun for a second word (DEC0018).
    /// </summary>
    /// <remarks>
    ///     "threeOrTwo" is deliberately not one of them, and the omission is the point rather than an
    ///     oversight (DEC0020). It may draw one word, so no room is reserved in front of the noun: a
    ///     long adjective stays in the pool, and when nothing fits behind it the narrowed participle
    ///     pool comes back empty and the absence is all that is left to draw. The ceiling holds
    ///     either way, and the theme keeps adjectives that "both" has to throw away.
    /// </remarks>
    /// <param name="mode">The mode to ask about.</param>
    internal static bool AlwaysDrawsTwoWords(this SegmentMode mode) {
        return mode == SegmentMode.Both;
    }

    #endregion

}