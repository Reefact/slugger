using Slugger.Domain.Generation;

namespace Slugger.Domain.Resolution;

/// <summary>
/// The category algebra of a single theme.
/// <code>
/// pool(noun)          = union of adjectives[c]  for c in noun.categories, plus "common"
/// partPool(noun)      = union of participles[c] for c in noun.categories, plus "common"
/// partPool(noun, adj) = partPool(noun) minus incompatible[adj]
/// </code>
/// Each of them minus whatever a <see cref="SlugBudget"/> leaves no room for, when the run
/// declares one (DEC0018): a limit reduces the surface once, here, and everything downstream -
/// the draw, the size rules, the analysis - sees the smaller theme without knowing why.
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
    private readonly SegmentMode? _drawn;
    private readonly SlugBudget? _budget;
    private readonly int? _maxSegmentWords;
    private IReadOnlyList<Noun>? _nouns;

    /// <param name="theme">The single theme every resolution stays inside.</param>
    /// <param name="drawn">
    /// What the run puts in front of the noun, or null when no run has spoken and the theme's own
    /// defaults still decide. A run declares a mode whether or not it also declares a ceiling, and
    /// it is that mode the floors follow (DEC0016) - so it is carried here rather than inside the
    /// budget, which only knows how long a slug comes out.
    /// </param>
    /// <param name="budget">
    /// What the run has room for, or null for no ceiling. It only ever removes: a word too long
    /// leaves the pool before the draw rather than the slug being trimmed after it.
    /// </param>
    /// <param name="maxSegmentWords">
    /// The most words a single value may carry, or null for no cap (DEC0023). It removes like
    /// the budget does - a value over the cap leaves the pool before the draw, and is never
    /// shortened to fit.
    /// </param>
    public ThemeResolver(
        Theme theme,
        SegmentMode? drawn = null,
        SlugBudget? budget = null,
        int? maxSegmentWords = null)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSegmentWords ?? 1, 1);
        Theme = theme;
        _drawn = drawn;
        _budget = budget;
        _maxSegmentWords = maxSegmentWords;

        // Built once rather than per draw: validation asks for the same adjective's refusals on
        // every noun, and only the adjectives a pair names are ever looked up at all.
        _refusedBeside = theme.Incompatible.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    /// <summary>The theme being resolved.</summary>
    public Theme Theme { get; }

    /// <summary>
    /// What is asked in front of the noun: the run's mode where it declared one, the theme's own
    /// otherwise, and "both" when neither said anything. The one place that chain is written, so
    /// that a run cannot ask for one shape and be measured against another (DEC0016). Not yet
    /// degraded for a theme declaring no participle - <c>ThemeValidator.DrawnMode</c> is where
    /// that is applied.
    /// </summary>
    public SegmentMode AskedMode => _drawn ?? Theme.Defaults.SegmentMode ?? SegmentMode.Both;

    /// <summary>What the run has room for, or null when it declared no ceiling.</summary>
    public SlugBudget? Budget => _budget;

    /// <summary>The most words a value may carry, or null when nothing set a cap (DEC0023).</summary>
    public int? MaxSegmentWords => _maxSegmentWords;

    /// <summary>
    /// Whether anything at all reduces this theme's pools. Asked wherever the answer decides
    /// between handing a pool back untouched and copying it to remove from: there are two
    /// reasons to narrow now, and reading them one at a time is how the second gets forgotten.
    /// </summary>
    public bool Narrows => _budget is not null || _maxSegmentWords is not null;

    /// <summary>
    /// The nouns a slug can be built on: all of them, or those a narrowed theme still has a slug
    /// to build on. Walked by the draw and by the size rules alike, so a limit narrows both from
    /// one place.
    /// </summary>
    /// <remarks>
    /// The two reasons read differently on the noun itself. A budget needs no clause for it: a
    /// noun too long leaves no room for any word in front of it, so its pools come back empty
    /// and it falls out here. A word cap does not work that way - a two word noun still reaches
    /// every one word adjective - so the noun is measured against the cap in its own right
    /// (DEC0023), which is the whole point of the cap where a compound noun is the long part.
    /// </remarks>
    public IReadOnlyList<Noun> Nouns => _nouns ??= !Narrows
        ? Theme.Nouns
        : [.. Theme.Nouns.Where(noun =>
            WithinTheWordCap(noun.Value) && (Pool(noun).Count > 0 || ParticiplePool(noun).Count > 0))];

    /// <summary>The adjectives reachable from this noun, and short enough for the run's budget.</summary>
    public IReadOnlyList<string> Pool(Noun noun)
    {
        ArgumentNullException.ThrowIfNull(noun);

        return Memoise(_adjectivePools, Theme.Adjectives, noun, WithRoomForAParticiple);
    }

    /// <summary>The participles reachable from this noun. Empty when the theme declares none for its categories.</summary>
    public IReadOnlyList<string> ParticiplePool(Noun noun)
    {
        ArgumentNullException.ThrowIfNull(noun);

        return Memoise(_participlePools, Theme.Participles, noun, Alone);
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
        _refusedBeside.TryGetValue(adjective, out HashSet<string>? refused);

        // The overwhelming case: nothing refuses and nothing is too long, so the pool is handed
        // back as it is rather than copied to remove nothing from it.
        if (refused is null && _budget is null)
        {
            return pool;
        }

        return
        [
            .. pool.Where(word =>
                (refused is null || !refused.Contains(word))
                && (_budget is null || _budget.Fits(adjective, word, noun.Value)))
        ];
    }

    /// <summary>
    /// An adjective needs room for the participle that will follow it, where the mode draws one.
    /// The shortest participle the noun reaches is what is reserved, which is generous by a word
    /// the adjective may refuse (DEC0017) - the per-adjective overload is where the truth is, and
    /// the couple floor is what refuses a theme this approximation would have let through.
    /// </summary>
    private bool WithRoomForAParticiple(Noun noun, string adjective)
    {
        if (!WithinTheWordCap(adjective))
        {
            return false;
        }

        if (_budget is null || !AskedMode.AlwaysDrawsTwoWords())
        {
            return Alone(noun, adjective);
        }

        IReadOnlyList<string> participles = ParticiplePool(noun);

        return participles.Count == 0
            ? Alone(noun, adjective)
            : _budget.Fits(adjective, participles.MinBy(word => word.Length)!, noun.Value);
    }

    private bool Alone(Noun noun, string word) =>
        WithinTheWordCap(word) && (_budget?.Fits(word, noun.Value) ?? true);

    /// <summary>
    /// Whether a value is short enough in words for the run's cap (DEC0023). A property of the
    /// value alone - unlike the budget, nothing about the noun or the adjective beside it can
    /// change the answer - so it is asked once, where a pool is built.
    /// </summary>
    private bool WithinTheWordCap(string word) =>
        _maxSegmentWords is not { } cap || word.Count(character => character == ' ') < cap;

    /// <summary>
    /// Whether this adjective can narrow the participles a noun reaches, so a caller walking
    /// every adjective of every noun can skip the ones that change nothing. A pair narrows by
    /// refusing (DEC0017); a budget narrows by leaving no room, and then every adjective does.
    /// </summary>
    /// <param name="adjective">The adjective to look up.</param>
    public bool NarrowsTheParticiples(string adjective)
    {
        ArgumentException.ThrowIfNullOrEmpty(adjective);

        return _budget is not null || _refusedBeside.ContainsKey(adjective);
    }

    private IReadOnlyList<string> Memoise(
        Dictionary<string, IReadOnlyList<string>> cache,
        IReadOnlyDictionary<string, IReadOnlyList<string>> words,
        Noun noun,
        Func<Noun, string, bool> fits)
    {
        if (cache.TryGetValue(noun.Value, out IReadOnlyList<string>? cached))
        {
            return cached;
        }

        List<string> pool = Resolve(words, noun);
        IReadOnlyList<string> reduced = Narrows ? [.. pool.Where(word => fits(noun, word))] : pool;
        cache[noun.Value] = reduced;

        return reduced;
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
