using Slugger.Domain.Resolution;

namespace Slugger.Domain.Validation;

/// <summary>
/// <code>
/// combos(noun)     = |pool(noun)| * max(1, |partPool(noun)|)
/// combos(category) = sum of combos(noun) over every noun carrying that category
/// total            = sum of combos(noun) over every noun
/// </code>
/// The max(1, ...) keeps a noun with no participle from zeroing the count. A noun in several
/// categories contributes to each: this is a coverage check per branch, not a partition.
/// The participle is counted whether or not segmentMode ends up using it, because
/// segmentMode changes behaviour, not the theme's real combinatorial space - and
/// <c>--segment both</c> reaches that space from any theme, whatever its defaults say.
/// <para>
/// The overloads taking a <see cref="SegmentMode"/> answer the other question: not what the
/// theme could produce, but what it produces left alone. There the pools add under "either"
/// and only one of them counts under "adjective" or "participle".
/// </para>
/// </summary>
public sealed class ThemeCombinatorics
{
    private readonly ThemeResolver _resolver;

    /// <param name="theme">The theme whose combinations are counted.</param>
    public ThemeCombinatorics(Theme theme)
        : this(new ThemeResolver(theme))
    {
    }

    /// <param name="resolver">
    /// A resolver already warmed on the theme. Validation resolves every pool anyway, so sharing
    /// one keeps the counting free rather than resolving a second time.
    /// </param>
    public ThemeCombinatorics(ThemeResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <summary>The theme being counted.</summary>
    public Theme Theme => _resolver.Theme;

    /// <summary>How many distinct slugs this one noun can produce.</summary>
    /// <param name="noun">The noun to count for.</param>
    public long CombinationsFor(Noun noun)
    {
        ArgumentNullException.ThrowIfNull(noun);

        long adjectives = _resolver.Pool(noun).Count;
        long participles = Math.Max(1, _resolver.ParticiplePool(noun).Count);

        return adjectives * participles;
    }

    /// <summary>How many distinct slugs this one noun can produce under one segment mode.</summary>
    /// <param name="noun">The noun to count for.</param>
    /// <param name="mode">What sits in front of it.</param>
    public long CombinationsFor(Noun noun, SegmentMode mode)
    {
        ArgumentNullException.ThrowIfNull(noun);

        long adjectives = _resolver.Pool(noun).Count;
        long participles = _resolver.ParticiplePool(noun).Count;

        return mode switch
        {
            SegmentMode.Adjective => adjectives,
            SegmentMode.Participle => participles,

            // One word, drawn from the two pools as one - so they add, where "both" multiplies.
            SegmentMode.Either => adjectives + participles,

            // The absence is one more participle to draw (DEC0020), so it is one slug more per
            // adjective and not fewer: a two word slug is one "both" cannot produce at all.
            SegmentMode.ThreeOrTwo => adjectives * (participles + 1),
            _ => adjectives * Math.Max(1, participles)
        };
    }

    /// <summary>How many distinct slugs the nouns carrying this category can produce between them.</summary>
    /// <param name="category">The category to count for.</param>
    public long CombinationsForCategory(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        return _resolver.Nouns
            .Where(noun => noun.Categories.Contains(category, StringComparer.Ordinal))
            .Sum(CombinationsFor);
    }

    /// <summary>The theme's whole combinatorial space, summed over every noun.</summary>
    public long Total()
    {
        return _resolver.Nouns.Sum(CombinationsFor);
    }

    /// <summary>What the theme produces under one segment mode, summed over every noun.</summary>
    /// <param name="mode">What sits in front of the noun.</param>
    public long Total(SegmentMode mode)
    {
        return _resolver.Nouns.Sum(noun => CombinationsFor(noun, mode));
    }
}
