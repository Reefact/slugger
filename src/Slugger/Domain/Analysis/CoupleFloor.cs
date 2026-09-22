namespace Slugger.Domain.Analysis;

/// <summary>
///     The same, for the participles a noun keeps once one adjective has refused what it refuses.
///     Two names rather than one, because the pair is what the author has to go and look at.
/// </summary>
/// <param name="Smallest">What the poorest couple still reaches.</param>
/// <param name="Noun">The noun of that couple.</param>
/// <param name="Adjective">The adjective that leaves it fewest.</param>
/// <param name="Floor">The threshold it had to clear.</param>
internal sealed record CoupleFloor(int Smallest, string Noun, string Adjective, int Floor);