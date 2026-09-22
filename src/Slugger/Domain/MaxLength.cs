namespace Slugger.Domain;

/// <summary>
///     What a theme promises about the length of the slugs it can produce, one figure per shape
///     (DEC0018). Docker and Heroku make the same promise by curating their vocabulary rather than
///     by declaring it - their word lists both stop at thirteen characters - and this is that
///     promise written down, so that the next word added to a theme is measured against it.
/// </summary>
/// <remarks>
///     Two figures rather than one, because the two shapes are not comparable: one word in front of
///     the noun is what Docker and Heroku produce and what their targets accept, two is slugger's
///     own richer form. A theme imitating a style makes its promise about that style's shape and
///     says nothing about the other, which is what a null means here - no promise, not no limit.
/// </remarks>
/// <param name="TwoWords">
///     The whole slug, with one word in front of the noun: segment mode "adjective", "participle"
///     or "either". Null when the theme promises nothing about that shape.
/// </param>
/// <param name="ThreeWords">The same, with two words in front of the noun: segment mode "both".</param>
/// <remarks>
///     A theme declaring "threeOrTwo" produces both shapes and so should promise on both keys; the
///     figure that applies to it is the three word one, the longest it can reach.
/// </remarks>
public sealed record MaxLength(int? TwoWords, int? ThreeWords) {

    #region Static members

    /// <summary>A theme that promises nothing, which is every theme written before DEC0018.</summary>
    public static MaxLength None { get; } = new(null, null);

    #endregion

    /// <summary>Whether the theme promises anything at all, so a caller can skip the whole check.</summary>
    public bool Declared => TwoWords is not null || ThreeWords is not null;

    /// <summary>
    ///     The figure that applies to a shape, or null where the theme said nothing about it.
    /// </summary>
    /// <param name="wordsBeforeTheNoun">One under every mode but "both", which draws two.</param>
    public int? For(int wordsBeforeTheNoun) {
        return wordsBeforeTheNoun >= 2 ? ThreeWords : TwoWords;
    }

    /// <summary>The same, read from the segment mode rather than from a count.</summary>
    /// <param name="mode">What sits in front of the noun.</param>
    public int? For(SegmentMode mode) {
        return For(mode.PutsAParticipleBesideAnAdjective() ? 2 : 1);
    }

}