namespace Slugger.Domain.Analysis;

/// <summary>
///     The measurements themselves. Every one of them is a fact about the theme as loaded, not a
///     judgement: a wide spread of exposure or a long slug is worth knowing and may be exactly what
///     the author wanted.
/// </summary>
/// <param name="Nouns">Entries in "nouns", duplicates included - this is what the draw sees.</param>
/// <param name="DistinctNouns">Distinct values after normalization - this is what validation counts.</param>
/// <param name="Drawn">What the theme puts in front of a noun left alone, which chose the floors.</param>
/// <param name="Adjectives">The per-noun adjective count and its worst case, floored where the mode draws it.</param>
/// <param name="Participles">The same for participles, or null when the theme declares none.</param>
/// <param name="WordsBeforeTheNoun">
///     The two pools added, floored - only under "either", the one mode drawing them as one.
/// </param>
/// <param name="ParticiplesBesideAnAdjective">
///     The worst an incompatibility leaves a noun - only under "both", and only where the theme
///     declares pairs at all.
/// </param>
/// <param name="Combinations">The per-category floor and its worst case, or null with no category in use.</param>
/// <param name="TotalCombinations">Distinct slugs the theme can produce, participle included.</param>
/// <param name="LongestSlug">The longest slug the measured surface can produce, formatted as the run would.</param>
/// <param name="CharacterCeiling">What it had to stay under, or null where nothing said.</param>
/// <param name="CombinationsDrawn">
///     How many it produces under its own segment mode - other slugs rather than fewer of the same,
///     since one word in front of the noun makes a different slug from two.
/// </param>
/// <param name="DuplicatedNouns">Values appearing more than once, which the draw favours accordingly.</param>
/// <param name="UnreachableCategories">Declared categories no noun carries, whose words never draw.</param>
/// <param name="LeastExposed">The adjective the fewest nouns can reach.</param>
/// <param name="MostExposed">The adjective the most nouns can reach.</param>
/// <param name="TwoWordAdjectives">Adjectives written in more than one word, which lengthen a slug.</param>
/// <param name="TotalAdjectives">Adjectives declared, all categories together.</param>
/// <param name="TwoWordNouns">Nouns written in more than one word.</param>
/// <param name="LongestSlugSegments">Segments the longest possible slug would carry, token aside.</param>
internal sealed record ThemeMeasurements(
    int                   Nouns,
    int                   DistinctNouns,
    SegmentMode           Drawn,
    PoolFloor             Adjectives,
    PoolFloor?            Participles,
    PoolFloor?            WordsBeforeTheNoun,
    CoupleFloor?          ParticiplesBesideAnAdjective,
    CategoryFloor?        Combinations,
    long                  TotalCombinations,
    long                  CombinationsDrawn,
    string                LongestSlug,
    int?                  CharacterCeiling,
    IReadOnlyList<string> DuplicatedNouns,
    IReadOnlyList<string> UnreachableCategories,
    Exposure              LeastExposed,
    Exposure              MostExposed,
    int                   TwoWordAdjectives,
    int                   TotalAdjectives,
    int                   TwoWordNouns,
    int                   LongestSlugSegments);