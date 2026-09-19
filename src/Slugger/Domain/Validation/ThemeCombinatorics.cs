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
/// segmentMode changes behaviour, not the theme's real combinatorial space.
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

    /// <summary>How many distinct slugs the nouns carrying this category can produce between them.</summary>
    /// <param name="category">The category to count for.</param>
    public long CombinationsForCategory(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        return Theme.Nouns
            .Where(noun => noun.Categories.Contains(category, StringComparer.Ordinal))
            .Sum(CombinationsFor);
    }

    /// <summary>The theme's whole combinatorial space, summed over every noun.</summary>
    public long Total()
    {
        return Theme.Nouns.Sum(CombinationsFor);
    }
}
