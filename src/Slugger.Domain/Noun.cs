namespace Slugger.Domain;

/// <summary>
/// A noun and the categories it belongs to. Zero categories means an empty adjective pool,
/// not access to everything: there is no implicit "anything goes" branch.
/// </summary>
public sealed record Noun(string Value, IReadOnlyList<string> Categories);
