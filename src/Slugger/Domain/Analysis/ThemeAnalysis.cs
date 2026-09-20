using FirstClassErrors;

namespace Slugger.Domain.Analysis;

/// <summary>
/// Everything measured about a theme, and nothing written about it. The prose belongs to
/// whoever renders this - the CLI writes markdown from it, exactly as it writes a refusal's
/// sentence from the facts an error carries (DEC0006).
/// </summary>
/// <param name="Name">The theme the analysis is about.</param>
/// <param name="Refusals">Every reason a load would refuse it, empty when it would not.</param>
/// <param name="Remarks">What it may do and probably did not mean to; never a refusal.</param>
/// <param name="Measurements">The numbers, or null when the document could not be read at all.</param>
internal sealed record ThemeAnalysis(
    string Name,
    IReadOnlyList<Error> Refusals,
    IReadOnlyList<string> Remarks,
    ThemeMeasurements? Measurements);

/// <summary>
/// How close a per-noun floor is, and which noun is closest to it. Named rather than averaged:
/// an average hides the one noun that fails, which is the whole point of DEC0003.
/// </summary>
/// <param name="Smallest">What the poorest noun actually reaches.</param>
/// <param name="Noun">That noun, so the author knows where to look.</param>
/// <param name="Floor">
/// The threshold it had to clear, or null where the theme's segment mode asks nothing of this
/// pool - which is a measurement worth reporting all the same, next to the one that does.
/// </param>
internal sealed record PoolFloor(int Smallest, string Noun, int? Floor);

/// <summary>
/// The same, for the participles a noun keeps once one adjective has refused what it refuses.
/// Two names rather than one, because the pair is what the author has to go and look at.
/// </summary>
/// <param name="Smallest">What the poorest couple still reaches.</param>
/// <param name="Noun">The noun of that couple.</param>
/// <param name="Adjective">The adjective that leaves it fewest.</param>
/// <param name="Floor">The threshold it had to clear.</param>
internal sealed record CoupleFloor(int Smallest, string Noun, string Adjective, int Floor);

/// <summary>The same, for the per-category combination floor.</summary>
/// <param name="Smallest">What the poorest category totals.</param>
/// <param name="Category">That category.</param>
/// <param name="Floor">The threshold it had to clear.</param>
internal sealed record CategoryFloor(long Smallest, string Category, long Floor);

/// <summary>How many nouns can draw one word - the spread between a common word and a rare one.</summary>
/// <param name="Word">The word.</param>
/// <param name="Nouns">How many nouns reach it.</param>
internal sealed record Exposure(string Word, int Nouns);

/// <summary>
/// The measurements themselves. Every one of them is a fact about the theme as loaded, not a
/// judgement: a wide spread of exposure or a long slug is worth knowing and may be exactly what
/// the author wanted.
/// </summary>
/// <param name="Nouns">Entries in "nouns", duplicates included - this is what the draw sees.</param>
/// <param name="DistinctNouns">Distinct values after normalization - this is what validation counts.</param>
/// <param name="Drawn">What the theme puts in front of a noun left alone, which chose the floors.</param>
/// <param name="Adjectives">The per-noun adjective count and its worst case, floored where the mode draws it.</param>
/// <param name="Participles">The same for participles, or null when the theme declares none.</param>
/// <param name="WordsBeforeTheNoun">
/// The two pools added, floored - only under "either", the one mode drawing them as one.
/// </param>
/// <param name="ParticiplesBesideAnAdjective">
/// The worst an incompatibility leaves a noun - only under "both", and only where the theme
/// declares pairs at all.
/// </param>
/// <param name="Combinations">The per-category floor and its worst case, or null with no category in use.</param>
/// <param name="TotalCombinations">Distinct slugs the theme can produce, participle included.</param>
/// <param name="CombinationsDrawn">
/// How many it produces under its own segment mode - other slugs rather than fewer of the same,
/// since one word in front of the noun makes a different slug from two.
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
    int Nouns,
    int DistinctNouns,
    SegmentMode Drawn,
    PoolFloor Adjectives,
    PoolFloor? Participles,
    PoolFloor? WordsBeforeTheNoun,
    CoupleFloor? ParticiplesBesideAnAdjective,
    CategoryFloor? Combinations,
    long TotalCombinations,
    long CombinationsDrawn,
    IReadOnlyList<string> DuplicatedNouns,
    IReadOnlyList<string> UnreachableCategories,
    Exposure LeastExposed,
    Exposure MostExposed,
    int TwoWordAdjectives,
    int TotalAdjectives,
    int TwoWordNouns,
    int LongestSlugSegments);
