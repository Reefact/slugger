namespace Slugger.Domain.Resolution;

/// <summary>
///     Picks one theme among several, proportionally to its noun count, without concatenating
///     their noun lists: a cumulative count array plus a binary search. Mathematically identical
///     to drawing uniformly from a flattened list (every noun keeps probability 1/total), but
///     O(D) extra memory for D themes instead of O(N) for N nouns.
/// </summary>
public sealed class WeightedThemePicker {

    #region Fields

    private readonly int[] _cumulativeNounCounts;

    #endregion

    #region Constructors & Destructor

    /// <param name="themes">The themes in scope, each weighted by how many nouns it holds.</param>
    public WeightedThemePicker(IReadOnlyList<Theme> themes) {
        ArgumentNullException.ThrowIfNull(themes);
        if (themes.Count == 0) {
            throw new ArgumentException("At least one theme is needed to pick from.", nameof(themes));
        }

        Themes                = themes;
        _cumulativeNounCounts = new int[themes.Count];

        int running = 0;
        for (int index = 0; index < themes.Count; index++) {
            running                      += themes[index].Nouns.Count;
            _cumulativeNounCounts[index] =  running;
        }

        TotalNouns = running;
    }

    #endregion

    /// <summary>The themes in scope.</summary>
    public IReadOnlyList<Theme> Themes { get; }

    /// <summary>How many nouns the themes hold between them - the span the draw lands in.</summary>
    public int TotalNouns { get; }

    /// <summary>Draws one theme, with a probability proportional to its share of the nouns.</summary>
    /// <param name="random">Where the draw comes from.</param>
    public Theme Pick(IRandomSource random) {
        ArgumentNullException.ThrowIfNull(random);

        if (Themes.Count == 1 || TotalNouns == 0) {
            return Themes[0];
        }

        int drawn = random.Next(TotalNouns);

        // Array.BinarySearch returns the complement of the first index whose cumulative count
        // exceeds the draw, which is exactly the interval the draw fell into. An exact hit means
        // the draw landed on a boundary, and the boundary belongs to the next theme.
        int found = Array.BinarySearch(_cumulativeNounCounts, drawn);

        return Themes[found >= 0 ? found + 1 : ~found];
    }

}