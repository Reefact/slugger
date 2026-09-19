namespace Slugger.Domain.Resolution;

/// <summary>
/// Picks one theme among several, proportionally to its noun count, without concatenating
/// their noun lists: a cumulative count array plus a binary search. Mathematically identical
/// to drawing uniformly from a flattened list (every noun keeps probability 1/total), but
/// O(D) extra memory for D themes instead of O(N) for N nouns.
/// </summary>
public sealed class WeightedThemePicker
{
    public WeightedThemePicker(IReadOnlyList<Theme> themes)
    {
        ArgumentNullException.ThrowIfNull(themes);
        Themes = themes;
    }

    public IReadOnlyList<Theme> Themes { get; }

    public Theme Pick(IRandomSource random) => throw new NotImplementedException();
}
