namespace Slugger.Domain;

/// <summary>
/// A noun and the categories it belongs to. Zero categories means an empty adjective pool,
/// not access to everything: there is no implicit "anything goes" branch.
/// </summary>
public sealed record Noun(string Value, IReadOnlyList<string> Categories)
{
    /// <summary>
    /// Words this noun refuses, whatever its categories would otherwise reach - the escape hatch
    /// for a pair that is unfortunate rather than implausible.
    /// </summary>
    /// <remarks>
    /// Categories already keep an adjective away from a noun it cannot describe. They cannot keep
    /// one away from a noun it describes perfectly well and insults anyway: Docker ships a
    /// hardcoded refusal of "boring_wozniak" for exactly that. This is where a theme says it
    /// itself, per noun, rather than in the engine. It is subtracted from both pools - the
    /// grammatical slot is not what makes a word unwelcome, so "boring" written here is refused
    /// as an adjective and as a participle alike.
    /// </remarks>
    public IReadOnlyList<string> Except { get; init; } = [];
}
