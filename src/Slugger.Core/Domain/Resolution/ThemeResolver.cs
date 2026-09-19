namespace Slugger.Domain.Resolution;

/// <summary>
/// The category algebra of a single theme.
/// <code>
/// pool(noun)     = union of adjectives[c]  for c in noun.categories, plus "common"
/// partPool(noun) = union of participles[c] for c in noun.categories, plus "common"
/// </code>
/// Both are resolved strictly inside one theme, never across files, even when two files
/// happen to use the same category name.
/// </summary>
/// <remarks>
/// <para>
/// <b>"common" is universal, which the spec states only for participles.</b> Read literally,
/// the adjective rule says a noun with no category reaches no adjective and that "common" is a
/// naming convention with no status in the code. The shipped themes contradict that: all 236 of
/// docker's nouns and 103 of heroku's carry no category at all, and neither file lists "common"
/// on a noun. Under the literal rule both themes resolve to an empty pool for every noun and are
/// refused at load - while the spec claims in the same breath that they clear all three rules by
/// themselves, quoting 236 nouns against 187 adjectives. Those numbers only hold if every noun
/// reaches "common", and the participle section says as much: "le pool common reste accessible
/// à tout nom quelles que soient ses capacités". So that is the reading implemented here, for
/// adjectives as well as participles.
/// </para>
/// <para>
/// Pools are memoised per noun: validation walks every noun and rule 3 walks them again per
/// category, so resolving twice would be the bulk of the work.
/// </para>
/// </remarks>
public sealed class ThemeResolver
{
    /// <summary>
    /// The category every noun reaches on top of its own. Still an ordinary key in the file -
    /// a theme that declares none simply has nothing extra to offer.
    /// </summary>
    public const string CommonCategory = "common";

    private readonly Dictionary<string, IReadOnlyList<string>> _adjectivePools = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<string>> _participlePools = new(StringComparer.Ordinal);

    /// <param name="theme">The single theme every resolution stays inside.</param>
    public ThemeResolver(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Theme = theme;
    }

    /// <summary>The theme being resolved.</summary>
    public Theme Theme { get; }

    /// <summary>The adjectives reachable from this noun.</summary>
    public IReadOnlyList<string> Pool(Noun noun)
    {
        ArgumentNullException.ThrowIfNull(noun);

        return Memoise(_adjectivePools, Theme.Adjectives, noun);
    }

    /// <summary>The participles reachable from this noun. Empty when the theme declares none for its categories.</summary>
    public IReadOnlyList<string> ParticiplePool(Noun noun)
    {
        ArgumentNullException.ThrowIfNull(noun);

        return Memoise(_participlePools, Theme.Participles, noun);
    }

    private static IReadOnlyList<string> Memoise(
        Dictionary<string, IReadOnlyList<string>> cache,
        IReadOnlyDictionary<string, IReadOnlyList<string>> words,
        Noun noun)
    {
        if (cache.TryGetValue(noun.Value, out IReadOnlyList<string>? cached))
        {
            return cached;
        }

        IReadOnlyList<string> pool = Resolve(words, noun);
        cache[noun.Value] = pool;

        return pool;
    }

    private static List<string> Resolve(
        IReadOnlyDictionary<string, IReadOnlyList<string>> words,
        Noun noun) => noun.Categories
        .Append(CommonCategory)
        .Where(words.ContainsKey)
        .SelectMany(category => words[category])
        .Distinct(StringComparer.Ordinal)
        .ToList();
}
