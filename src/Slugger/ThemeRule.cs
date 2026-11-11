namespace Slugger;

/// <summary>A rule a theme is measured against, one row of <see cref="ThemeMeasurements.Margins" />.</summary>
public enum ThemeRule {

    /// <summary>Distinct nouns, against <see cref="ThemeFloors.MinimumNouns" />.</summary>
    DistinctNouns,

    /// <summary>
    ///     Words before the noun, adjectives and participles together - only under "either", the one mode
    ///     drawing them as one - against <see cref="ThemeFloors.MinimumPoolPerNoun" />.
    /// </summary>
    WordsBeforeTheNoun,

    /// <summary>Adjectives each noun reaches, against <see cref="ThemeFloors.MinimumPoolPerNoun" />.</summary>
    AdjectivesPerNoun,

    /// <summary>Participles each noun reaches, against the floor its mode holds it to.</summary>
    ParticiplesPerNoun,

    /// <summary>
    ///     Participles left behind the adjective that leaves the fewest - only where the theme declares
    ///     incompatible pairs - against <see cref="ThemeFloors.MinimumParticiplePoolPerNoun" />.
    /// </summary>
    ParticiplesBesideAnAdjective,

    /// <summary>
    ///     Characters of the longest slug, against the theme's own <c>maxLength</c> promise: a ceiling,
    ///     not a floor.
    /// </summary>
    Characters,

    /// <summary>Combinations of the poorest category, against <see cref="ThemeFloors.MinimumCombinationsPerCategory" />.</summary>
    CombinationsPerCategory

}
