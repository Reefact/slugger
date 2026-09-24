namespace Slugger.Domain;

/// <summary>
///     An entry of a theme's noun list: a noun and the categories it belongs to. Not
///     <see cref="Noun" />, which is the role a term plays in a slug - this one is what a theme
///     file declares, and it keeps a suffix until it is given a name of its own. Zero categories means an empty adjective pool,
///     not access to everything: there is no implicit "anything goes" branch.
/// </summary>
public sealed record NounOld(string Value, IReadOnlyList<string> Categories) {

    /// <summary>
    ///     Words this noun refuses, whatever its categories would otherwise reach - the escape hatch
    ///     for a pair that is unfortunate rather than implausible.
    /// </summary>
    /// <remarks>
    ///     Categories already keep an adjective away from a noun it cannot describe. They cannot keep
    ///     one away from a noun it describes perfectly well and insults anyway: Docker ships a
    ///     hardcoded refusal of "boring_wozniak" for exactly that. This is where a theme says it
    ///     itself, per noun, rather than in the engine. It is subtracted from both pools - the
    ///     grammatical slot is not what makes a word unwelcome, so "boring" written here is refused
    ///     as an adjective and as a participle alike.
    /// </remarks>
    public IReadOnlyList<string> Except { get; init; } = [];

}