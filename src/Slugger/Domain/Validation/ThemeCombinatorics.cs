#region Usings declarations

using Slugger.Domain.Resolution;

#endregion

namespace Slugger.Domain.Validation;

/// <summary>
///     Counts the combinations a theme can produce, token aside: per noun, per category or over the
///     whole theme. <see cref="Total(SegmentMode)" /> answers the usual question - how many distinct
///     slugs a theme gives in one segment mode.
/// </summary>
/// <remarks>
///     <para>
///         The overloads without a mode count the largest space, which is what the per-category floor
///         measures:
///         <code>
/// combos(noun)     = |pool(noun)| * max(1, |partPool(noun)|)
/// combos(category) = sum of combos(noun) over every noun that lists that category
/// total            = sum of combos(noun) over every noun
/// </code>
///         The max(1, ...) keeps a noun with no participle from zeroing the count. A noun in several
///         categories counts in each: this checks every branch of the theme, it does not partition it.
///         The participle is counted whatever the segment mode, because any mode can be asked for.
///     </para>
///     <para>
///         The overloads taking a <see cref="SegmentMode" /> count what the theme produces in that mode:
///         the pools add under "either", and only one of them counts under "adjective" or "participle".
///     </para>
///     <para>
///         They count combinations, not strings: an incompatible pair is not subtracted, and two
///         combinations that happen to write the same string count twice. Read the result as an order
///         of magnitude. A token multiplies it by the number of values the token can take.
///     </para>
/// </remarks>
public sealed class ThemeCombinatorics {

    #region Fields

    private readonly ThemeResolver _resolver;

    #endregion

    #region Constructors & Destructor

    /// <summary>
    ///     Counts over the whole theme as it describes itself - see <see cref="ThemeResolver.AsDeclared" />.
    /// </summary>
    /// <param name="theme">The theme whose combinations are counted.</param>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> is null.</exception>
    public ThemeCombinatorics(ThemeDocument theme)
        : this(ThemeResolver.AsDeclared(theme)) { }

    /// <summary>
    ///     Counts over a theme as this resolver narrows it - under a length limit, for instance - reusing
    ///     the pools it has already computed.
    /// </summary>
    /// <param name="resolver">The resolver to count over.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resolver" /> is null.</exception>
    public ThemeCombinatorics(ThemeResolver resolver) {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    #endregion

    /// <summary>The theme being counted.</summary>
    public ThemeDocument Document => _resolver.Document;

    /// <summary>
    ///     How many combinations this noun can produce at most: its adjectives times its participles, the
    ///     participles counted whatever the mode.
    /// </summary>
    /// <param name="noun">The noun to count for.</param>
    public long CombinationsFor(NounEntry noun) {
        ArgumentNullException.ThrowIfNull(noun);

        long adjectives  = _resolver.Pool(noun).Count;
        long participles = Math.Max(1, _resolver.ParticiplePool(noun).Count);

        return adjectives * participles;
    }

    /// <summary>How many combinations this noun produces under one segment mode.</summary>
    /// <param name="noun">The noun to count for.</param>
    /// <param name="mode">What sits in front of it.</param>
    public long CombinationsFor(NounEntry noun, SegmentMode mode) {
        ArgumentNullException.ThrowIfNull(noun);

        long adjectives  = _resolver.Pool(noun).Count;
        long participles = _resolver.ParticiplePool(noun).Count;

        return mode switch {
            SegmentMode.Adjective  => adjectives,
            SegmentMode.Participle => participles,

            // One word, drawn from the two pools as one - so they add, where "both" multiplies.
            SegmentMode.Either => adjectives + participles,

            // The absence is one more participle to draw (DEC0020), so it is one slug more per
            // adjective and not fewer: a two word slug is one "both" cannot produce at all.
            SegmentMode.ThreeOrTwo => adjectives * (participles + 1),
            _                      => adjectives * Math.Max(1, participles)
        };
    }

    /// <summary>
    ///     How many combinations the nouns that list this category can produce at most, between them. A
    ///     noun reaches <c>common</c> without listing it, so <c>common</c> only counts the nouns that do.
    /// </summary>
    /// <param name="category">The category to count for.</param>
    public long CombinationsForCategory(string category) {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        return _resolver.Nouns
                        .Where(noun => noun.Categories.Contains(category, StringComparer.Ordinal))
                        .Sum(CombinationsFor);
    }

    /// <summary>The largest space the theme can produce, summed over every noun.</summary>
    public long Total() {
        return _resolver.Nouns.Sum(CombinationsFor);
    }

    /// <summary>How many combinations the theme produces under one segment mode, summed over every noun.</summary>
    /// <param name="mode">What sits in front of the noun.</param>
    public long Total(SegmentMode mode) {
        return _resolver.Nouns.Sum(noun => CombinationsFor(noun, mode));
    }

}