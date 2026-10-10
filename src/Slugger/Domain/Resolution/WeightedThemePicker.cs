namespace Slugger.Domain.Resolution;

/// <summary>
///     Draws one theme among several, each weighted by how many nouns it holds, so that every noun of
///     every theme has the same chance - as if their noun lists were one list.
/// </summary>
/// <remarks>
///     <para>
///         The weights follow the order of the list. Build it in a stable order - sorted by name, for
///         instance - when a seeded run has to give the same slugs again, and list each theme once: a
///         theme listed twice is drawn twice as often.
///     </para>
///     <para>
///         It keeps one running count per theme rather than a merged list, so it costs one integer per
///         theme. It never changes once built and can be shared between threads, as long as the list
///         you passed does not change either; the random source passed to <see cref="Pick" /> is the
///         part to watch.
///     </para>
/// </remarks>
public sealed class WeightedThemePicker {

    #region Fields

    private readonly int[] _cumulativeNounCounts;

    #endregion

    #region Constructors & Destructor

    /// <summary>Prepares the draw over these themes.</summary>
    /// <param name="themes">The themes to draw from, each weighted by how many nouns it holds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="themes" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="themes" /> is empty.</exception>
    public WeightedThemePicker(IReadOnlyList<ThemeDocument> themes) {
        ArgumentNullException.ThrowIfNull(themes);
        if (themes.Count == 0) { throw new ArgumentException("At least one theme is needed to pick from.", nameof(themes)); }

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

    /// <summary>The themes to draw from, in the order given.</summary>
    public IReadOnlyList<ThemeDocument> Themes { get; }

    /// <summary>How many nouns the themes hold between them.</summary>
    public int TotalNouns { get; }

    /// <summary>
    ///     Draws one theme, with a probability proportional to its share of the nouns. A single theme is
    ///     returned without a draw.
    /// </summary>
    /// <param name="random">Where the draw comes from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="random" /> is null.</exception>
    public ThemeDocument Pick(IRandomSource random) {
        ArgumentNullException.ThrowIfNull(random);

        if (Themes.Count == 1 || TotalNouns == 0) { return Themes[0]; }

        int drawn = random.Next(TotalNouns);

        // Array.BinarySearch returns the complement of the first index whose cumulative count
        // exceeds the draw, which is exactly the interval the draw fell into. An exact hit means
        // the draw landed on a boundary, and the boundary belongs to the next theme.
        int found = Array.BinarySearch(_cumulativeNounCounts, drawn);

        return Themes[found >= 0 ? found + 1 : ~found];
    }

}