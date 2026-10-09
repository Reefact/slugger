namespace Slugger.Domain;

/// <summary>
///     The longest slug a theme promises to produce, one figure per shape: the <c>maxLength</c> block
///     of a theme file, read into <see cref="ThemeDocument.MaxLength" />. A theme whose words break its
///     promise is refused at load, so the next word added to it is measured against the promise.
/// </summary>
/// <remarks>
///     <para>
///         Two figures rather than one, because the two shapes are not comparable: one term before the
///         noun is what Docker and Heroku produce and what their destinations accept, two is slugger's
///         own longer form. A theme imitating a style makes its promise about that style's shape and
///         says nothing about the other, which is what a null means here - no promise, not no limit.
///     </para>
///     <para>
///         A theme drawing "threeOrTwo" produces both shapes and so should promise on both keys; the
///         figure that applies to it is the three-term one, the longest it can reach.
///     </para>
///     <para>
///         It describes the theme, not your slugs: to limit their length, set
///         <see cref="GenerationOptions.MaxLength" />.
///     </para>
///     <para>
///         See decision record DEC0018 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md
///     </para>
/// </remarks>
/// <param name="TwoWords">
///     The whole slug, with one term before the noun - segment mode "adjective", "participle" or
///     "either" - or null when the theme promises nothing about that shape. <c>twoWords</c> in the file.
/// </param>
/// <param name="ThreeWords">
///     The same, with two terms before the noun - segment mode "both" or "threeOrTwo".
///     <c>threeWords</c> in the file.
/// </param>
public sealed record MaxLength(int? TwoWords, int? ThreeWords) {

    #region Static members

    /// <summary>A theme that promises nothing.</summary>
    public static MaxLength None { get; } = new(null, null);

    #endregion

    /// <summary>Whether the theme promises anything at all.</summary>
    public bool Declared => TwoWords is not null || ThreeWords is not null;

    /// <summary>
    ///     The figure for a slug with that many terms before the noun, or null where the theme said
    ///     nothing about that shape.
    /// </summary>
    /// <param name="wordsBeforeTheNoun">How many terms precede the noun: one, or two and more.</param>
    public int? For(int wordsBeforeTheNoun) {
        return wordsBeforeTheNoun >= 2 ? ThreeWords : TwoWords;
    }

    /// <summary>
    ///     The figure for the longest shape a segment mode produces, or null where the theme said nothing
    ///     about that shape.
    /// </summary>
    /// <param name="mode">What precedes the noun.</param>
    public int? For(SegmentMode mode) {
        return For(mode.PutsAParticipleBesideAnAdjective() ? 2 : 1);
    }

}