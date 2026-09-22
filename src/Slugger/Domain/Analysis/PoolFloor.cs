namespace Slugger.Domain.Analysis;

/// <summary>
///     How close a per-noun floor is, and which noun is closest to it. Named rather than averaged:
///     an average hides the one noun that fails, which is the whole point of DEC0003.
/// </summary>
/// <param name="Smallest">What the poorest noun actually reaches.</param>
/// <param name="Noun">That noun, so the author knows where to look.</param>
/// <param name="Floor">
///     The threshold it had to clear, or null where the theme's segment mode asks nothing of this
///     pool - which is a measurement worth reporting all the same, next to the one that does.
/// </param>
internal sealed record PoolFloor(int Smallest, string Noun, int? Floor);