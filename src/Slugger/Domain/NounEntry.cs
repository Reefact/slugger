namespace Slugger.Domain;

/// <summary>
///     One entry of a theme's noun list, as the file declares it: the noun, the categories it belongs
///     to, and the words it refuses.
/// </summary>
/// <remarks>
///     A noun reaches the adjectives and participles of its own categories and those of <c>common</c>.
///     With no category, it reaches <c>common</c> alone: there is no implicit "anything goes".
/// </remarks>
/// <param name="Value">
///     The noun, as a loaded theme holds it: lowercase, its words separated by single spaces.
///     <c>"rock crystal"</c> is one noun of two words.
/// </param>
/// <param name="Categories">
///     The categories the noun belongs to, which decide the adjectives and participles it reaches. May
///     be empty.
/// </param>
public sealed record NounEntry(string Value, IReadOnlyList<string> Categories) {

    /// <summary>
    ///     Words this noun refuses, whatever its categories would otherwise reach - the <c>except</c> list of
    ///     the file, for a pair that is unfortunate rather than implausible.
    /// </summary>
    /// <remarks>
    ///     Categories already keep an adjective away from a noun it cannot describe. They cannot keep
    ///     one away from a noun it describes perfectly well and insults anyway: Docker ships a
    ///     hard-coded refusal of "boring_wozniak" for exactly that. This is where a theme says it
    ///     itself, per noun, rather than in the engine. A word listed here is refused as an adjective
    ///     and as a participle alike: the grammatical slot is not what makes a word unwelcome.
    /// </remarks>
    public IReadOnlyList<string> Exclusions { get; init; } = [];

}