namespace Slugger.Domain.Resolution;

/// <summary>
/// The category algebra of a single theme.
/// <code>
/// pool(noun)          = union of adjectives[c]  for c in noun.categories, plus "common"
/// partPool(noun)      = union of participles[c] for c in noun.categories, plus "common"
/// partPool(noun, adj) = partPool(noun) minus incompatible[adj]
/// </code>
/// Both are resolved strictly inside one theme, never across files, even when two files
/// happen to use the same category name.
/// </summary>
/// <remarks>
/// <para>
/// <b>"common" is universal - a shared floor, not a fallback (DEC0002).</b> A noun with no
/// category reaches "common"; a noun that declares categories reaches its own <i>and</i>
/// "common". The shipped themes are what settled it: all 236 of docker's nouns and 103 of
/// heroku's carry no category at all, and neither file lists "common" on a noun, so the
/// narrower reading resolves both to an empty pool for every noun and refuses them at load.
/// It holds for adjectives as well as participles.
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
    private readonly Dictionary<string, HashSet<string>> _refusedBeside;

    /// <param name="theme">The single theme every resolution stays inside.</param>
    public ThemeResolver(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Theme = theme;

        // Built once rather than per draw: validation asks for the same adjective's refusals on
        // every noun, and only the adjectives a pair names are ever looked up at all.
        _refusedBeside = theme.Incompatible.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);
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

    /// <summary>
    /// The participles this noun reaches once the adjective already drawn has had its say
    /// (DEC0017). Subtracted before the draw rather than corrected after it, which is what
    /// keeps the draw uniform over what is left and the number of draws fixed.
    /// </summary>
    /// <param name="noun">The noun being drawn for.</param>
    /// <param name="adjective">The adjective already drawn, whose refusals apply.</param>
    public IReadOnlyList<string> ParticiplePool(Noun noun, string adjective)
    {
        ArgumentNullException.ThrowIfNull(noun);
        ArgumentException.ThrowIfNullOrEmpty(adjective);

        IReadOnlyList<string> pool = ParticiplePool(noun);

        // The overwhelming case: this adjective refuses nothing, so the pool is handed back as
        // it is rather than copied to remove nothing from it.
        return _refusedBeside.TryGetValue(adjective, out HashSet<string>? refused)
            ? [.. pool.Where(word => !refused.Contains(word))]
            : pool;
    }

    /// <summary>Whether this adjective refuses any participle at all, so a caller can skip it.</summary>
    /// <param name="adjective">The adjective to look up.</param>
    public bool RefusesAnything(string adjective)
    {
        ArgumentException.ThrowIfNullOrEmpty(adjective);

        return _refusedBeside.ContainsKey(adjective);
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
        Noun noun)
    {
        // Subtracted after the union rather than filtered per category: a word reached through
        // two categories has to go once, and the exclusion is about the word, not the route.
        HashSet<string> refused = new(noun.Except, StringComparer.Ordinal);

        return noun.Categories
            .Append(CommonCategory)
            .Where(words.ContainsKey)
            .SelectMany(category => words[category])
            .Distinct(StringComparer.Ordinal)
            .Where(word => !refused.Contains(word))
            .ToList();
    }
}
