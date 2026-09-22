namespace Slugger.Domain.Analysis;

/// <summary>The same, for the per-category combination floor.</summary>
/// <param name="Smallest">What the poorest category totals.</param>
/// <param name="Category">That category.</param>
/// <param name="Floor">The threshold it had to clear.</param>
internal sealed record CategoryFloor(long Smallest, string Category, long Floor);