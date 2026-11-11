namespace Slugger;

/// <summary>
///     The floors a theme must clear to be accepted, measured on what each noun actually reaches rather
///     than on the length of its lists. A theme's own <c>"allowSmall": true</c>, or <c>allowSmall</c> on a
///     load, waives them.
/// </summary>
/// <remarks>
///     <para>
///         They do not move: every <c>1.x</c> version holds a theme to these same values, so a constant
///         is the right shape for them. Changing one would take a decision record and a major version
///         (DEC0037).
///     </para>
///     <para>
///         See decision records DEC0002, DEC0003, DEC0016 and DEC0037 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0002-common-atteint-par-tout-nom.md,
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0003-validation-sur-le-pool-resolu.md,
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0016-planchers-alignes-sur-le-mode-de-segment.md and
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0037-contrat-de-compatibilite-de-la-1x.md
///     </para>
/// </remarks>
public static class ThemeFloors {

    /// <summary>Distinct nouns a theme needs before it is accepted.</summary>
    public const int MinimumNouns = 100;

    /// <summary>
    ///     Words each noun must reach for the term just before it: its adjectives under "adjective",
    ///     "both" and "threeOrTwo", its participles under "participle", the two added under "either".
    /// </summary>
    public const int MinimumPoolPerNoun = 100;

    /// <summary>
    ///     Participles each noun must reach under "both" and "threeOrTwo" - the modes where a participle
    ///     is a second term rather than the only one - for every adjective it can draw, once incompatible
    ///     pairs and a length limit have taken their share.
    /// </summary>
    /// <remarks>
    ///     Deliberately far below <see cref="MinimumPoolPerNoun" />: a participle is a second term there,
    ///     and twenty of them behind every adjective already keep the pairs varied.
    /// </remarks>
    public const int MinimumParticiplePoolPerNoun = 20;

    /// <summary>
    ///     Combinations the nouns that list a category must reach between them, so that no branch of the
    ///     theme is poor on its own. Only categories a noun lists are measured, and never <c>common</c>,
    ///     which every noun reaches (DEC0025).
    /// </summary>
    public const int MinimumCombinationsPerCategory = 40_000;

}
