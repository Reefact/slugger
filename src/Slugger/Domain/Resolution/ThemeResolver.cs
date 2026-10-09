#region Usings declarations

using Slugger.Domain.Generation;

#endregion

namespace Slugger.Domain.Resolution;

/// <summary>
///     Works out what each noun of one theme can be drawn with - its pool of adjectives and its pool of
///     participles - optionally narrowed by a length limit and a limit on words per term:
///     <code>
/// pool(noun)          = union of adjectives[c]  for c in noun.categories, plus "common"
/// partPool(noun)      = union of participles[c] for c in noun.categories, plus "common"
/// partPool(noun, adj) = partPool(noun) minus incompatible[adj]
/// </code>
///     Each pool also loses the words the noun refuses and, under a limit, the words that would not
///     fit. Everything stays inside one theme, even when two themes use the same category name.
/// </summary>
/// <remarks>
///     <para>
///         <b>"common" is a shared floor, not a fallback.</b> A noun with no category reaches "common";
///         a noun that declares categories reaches its own <i>and</i> "common". It holds for adjectives
///         as well as participles.
///     </para>
///     <para>
///         Build one to measure what a set of options leaves of a theme, once: pass it to
///         <see cref="Validation.ThemeValidator.Validate(ThemeResolver, bool)" /> or to
///         <see cref="Validation.ThemeCombinatorics" />. Generation builds its own on every call; the
///         library offers no way to generate from one you built.
///     </para>
///     <para>
///         <b>Not thread-safe.</b> Pools are computed when first asked for and kept in a cache that is
///         not synchronised: use one instance from one thread at a time.
///     </para>
///     <para>
///         See decision records DEC0002 and DEC0018 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0002-common-atteint-par-tout-nom.md and
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md
///     </para>
/// </remarks>
public sealed class ThemeResolver {

    /// <summary>
    ///     The category every noun reaches on top of its own: <c>common</c>. Still an ordinary key in the
    ///     file - a theme that declares none simply has nothing extra to offer.
    /// </summary>
    public const string CommonCategory = "common";

    #region Static members

    /// <summary>
    ///     The theme as it describes itself, which is what a load measures: no length limit, its own
    ///     segment mode, and the words-per-term limit its own <c>defaults</c> state, if any.
    /// </summary>
    /// <remarks>
    ///     A style a theme states about itself is what its floors are measured against, or stating it
    ///     would mean nothing. Options you generate with are another matter: build the resolver from them
    ///     with the constructor.
    /// </remarks>
    /// <param name="theme">The theme to read as it stands.</param>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> is null.</exception>
    public static ThemeResolver AsDeclared(ThemeDocument theme) {
        ArgumentNullException.ThrowIfNull(theme);

        return new ThemeResolver(theme, maxSegmentWords: theme.Defaults.MaxSegmentWords);
    }

    private static List<string> Resolve(IReadOnlyDictionary<string, IReadOnlyList<string>> words,
                                        NounEntry                                               noun) {
        // Subtracted after the union rather than filtered per category: a word reached through
        // two categories has to go once, and the exclusion is about the word, not the route.
        HashSet<string> refused = new(noun.Exclusions, StringComparer.Ordinal);

        return noun.Categories
                   .Append(CommonCategory)
                   .Where(words.ContainsKey)
                   .SelectMany(category => words[category])
                   .Distinct(StringComparer.Ordinal)
                   .Where(word => !refused.Contains(word))
                   .ToList();
    }

    #endregion

    #region Fields

    private readonly Dictionary<string, IReadOnlyList<string>> _adjectivePools  = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<string>> _participlePools = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>>       _refusedBeside;
    private readonly SegmentMode?                              _drawn;
    private          IReadOnlyList<NounEntry>?                      _nouns;

    #endregion

    #region Constructors & Destructor

    /// <summary>A resolver over one theme, narrowed by the limits given.</summary>
    /// <remarks>
    ///     To measure what generation will draw from with a set of options, pass
    ///     <c>new ThemeResolver(theme, options.SegmentMode, budget, options.MaxSegmentWords)</c>, where
    ///     <c>budget</c> is <c>new SlugBudget(cap, options)</c> when <see cref="GenerationOptions.MaxLength" />
    ///     is set, and null otherwise.
    /// </remarks>
    /// <param name="theme">The theme; every pool stays inside it.</param>
    /// <param name="drawn">
    ///     The segment mode slugs will be drawn with, or null to take the theme's own. The floors a
    ///     validator applies follow this mode.
    /// </param>
    /// <param name="budget">
    ///     The length limit, or null for none. It only ever removes: a word too long leaves the pool
    ///     before the draw, and a slug is never trimmed after it.
    /// </param>
    /// <param name="maxSegmentWords">
    ///     The most words a single term may have, or null for no limit. A term over it leaves the pool
    ///     before the draw, and is never shortened to fit.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSegmentWords" /> is below one.</exception>
    public ThemeResolver(ThemeDocument        theme,
                         SegmentMode? drawn           = null,
                         SlugBudget?  budget          = null,
                         int?         maxSegmentWords = null) {
        ArgumentNullException.ThrowIfNull(theme);
        if (maxSegmentWords < 1) { throw new ArgumentOutOfRangeException(nameof(maxSegmentWords), maxSegmentWords, "A term has at least one word, so the cap must be 1 or more."); }

        Document           = theme;
        _drawn          = drawn;
        Budget          = budget;
        MaxSegmentWords = maxSegmentWords;

        // Built once rather than per draw: validation asks for the same adjective's refusals on
        // every noun, and only the adjectives a pair names are ever looked up at all.
        _refusedBeside = theme.Incompatible.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    #endregion

    /// <summary>The theme being resolved.</summary>
    public ThemeDocument Document { get; }

    /// <summary>
    ///     The segment mode asked for: the one given to the constructor, else the theme's own, else
    ///     <see cref="SegmentMode.Both" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It is what was asked, not what is drawn: a theme that declares no participle draws
    ///         adjectives whatever this says, and its floors are measured that way.
    ///     </para>
    ///     <para>
    ///         See decision record DEC0016 (in French):
    ///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0016-planchers-alignes-sur-le-mode-de-segment.md
    ///     </para>
    /// </remarks>
    public SegmentMode AskedMode => _drawn ?? Document.Defaults.SegmentMode ?? SegmentMode.Both;

    /// <summary>The length limit, or null when none was given.</summary>
    public SlugBudget? Budget { get; }

    /// <summary>The most words a term may have, or null when no limit was given.</summary>
    public int? MaxSegmentWords { get; }

    /// <summary>Whether a length limit or a words-per-term limit narrows this theme's pools.</summary>
    public bool Narrows => Budget is not null || MaxSegmentWords is not null;

    /// <summary>
    ///     The nouns a slug can be built on: all of them or, when a limit narrows the theme, those that
    ///     still have a word to put in front of them and are short enough themselves.
    /// </summary>
    /// <remarks>
    ///     The two limits act differently on the noun itself. Under a length limit, a noun too long leaves
    ///     no room for any word in front of it, so its pools come back empty and it falls out. A
    ///     words-per-term limit does not work that way - a two-word noun still reaches every one-word
    ///     adjective - so the noun is measured against the limit in its own right.
    /// </remarks>
    public IReadOnlyList<NounEntry> Nouns => _nouns ??= !Narrows
        ? Document.Nouns
        : [
            .. Document.Nouns.Where(noun =>
                                     WithinTheWordCap(noun.Value) && (Pool(noun).Count > 0 || ParticiplePool(noun).Count > 0))
        ];

    /// <summary>
    ///     The adjectives this noun reaches, minus the words it refuses and, under a limit, the words that
    ///     would not fit.
    /// </summary>
    /// <param name="noun">A noun of this theme.</param>
    /// <exception cref="ArgumentNullException"><paramref name="noun" /> is null.</exception>
    public IReadOnlyList<string> Pool(NounEntry noun) {
        ArgumentNullException.ThrowIfNull(noun);

        return Memoise(_adjectivePools, Document.Adjectives, noun, WithRoomForAParticiple);
    }

    /// <summary>
    ///     The participles this noun reaches, minus the words it refuses and, under a limit, the words that
    ///     would not fit. Empty when the theme declares none for its categories.
    /// </summary>
    /// <param name="noun">A noun of this theme.</param>
    /// <exception cref="ArgumentNullException"><paramref name="noun" /> is null.</exception>
    public IReadOnlyList<string> ParticiplePool(NounEntry noun) {
        ArgumentNullException.ThrowIfNull(noun);

        return Memoise(_participlePools, Document.Participles, noun, Alone);
    }

    /// <summary>
    ///     The participles that may follow this adjective before this noun: the noun's participles, minus
    ///     those the adjective refuses (see <see cref="ThemeDocument.Incompatible" />) and, under a length
    ///     limit, minus those that no longer fit behind it.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Removed before the draw rather than corrected after it, which keeps the draw uniform over
    ///         what is left.
    ///     </para>
    ///     <para>
    ///         See decision record DEC0017 (in French):
    ///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md
    ///     </para>
    /// </remarks>
    /// <param name="noun">The noun being drawn for.</param>
    /// <param name="adjective">The adjective already drawn, whose refusals apply.</param>
    /// <exception cref="ArgumentNullException"><paramref name="noun" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="adjective" /> is null or empty.</exception>
    public IReadOnlyList<string> ParticiplePool(NounEntry noun, string adjective) {
        ArgumentNullException.ThrowIfNull(noun);
        ArgumentException.ThrowIfNullOrEmpty(adjective);

        IReadOnlyList<string> pool = ParticiplePool(noun);
        _refusedBeside.TryGetValue(adjective, out HashSet<string>? refused);

        if (NothingNarrowsThePool(refused)) { return pool; }

        return [
            .. pool.Where(word =>
                              (refused is null || !refused.Contains(word))
                           && (Budget is null  || Budget.Fits(adjective, word, noun.Value)))
        ];
    }

    /// <summary>
    ///     The overwhelming case: nothing refuses beside this adjective and no budget applies, so
    ///     the pool can be handed back as it is rather than copied to remove nothing from it.
    /// </summary>
    /// <param name="refused">What this adjective refuses, or null where it refuses nothing.</param>
    private bool NothingNarrowsThePool(HashSet<string>? refused) {
        return refused is null && Budget is null;
    }

    /// <summary>
    ///     Whether this adjective can narrow the participles a noun reaches, so that a caller walking every
    ///     adjective of every noun can skip those that change nothing. An adjective narrows them when it
    ///     refuses some; under a length limit, every adjective does.
    /// </summary>
    /// <param name="adjective">The adjective to look up.</param>
    /// <exception cref="ArgumentException"><paramref name="adjective" /> is null or empty.</exception>
    public bool NarrowsTheParticiples(string adjective) {
        ArgumentException.ThrowIfNullOrEmpty(adjective);

        return Budget is not null || _refusedBeside.ContainsKey(adjective);
    }

    /// <summary>
    ///     An adjective needs room for the participle that will follow it, where the mode draws one.
    ///     The shortest participle the noun reaches is what is reserved, which is generous by a word
    ///     the adjective may refuse (DEC0017) - the per-adjective overload is where the truth is, and
    ///     the couple floor is what refuses a theme this approximation would have let through.
    /// </summary>
    private bool WithRoomForAParticiple(NounEntry noun, string adjective) {
        if (!WithinTheWordCap(adjective)) { return false; }
        if (Budget is null || !AskedMode.AlwaysDrawsTwoWords()) { return Alone(noun, adjective); }

        IReadOnlyList<string> participles = ParticiplePool(noun);

        return participles.Count == 0
            ? Alone(noun, adjective)
            : Budget.Fits(adjective, participles.MinBy(word => word.Length)!, noun.Value);
    }

    private bool Alone(NounEntry noun, string word) {
        return WithinTheWordCap(word) && (Budget?.Fits(word, noun.Value) ?? true);
    }

    /// <summary>
    ///     Whether a value is short enough in words for the run's cap (DEC0023). A property of the
    ///     value alone - unlike the budget, nothing about the noun or the adjective beside it can
    ///     change the answer - so it is asked once, where a pool is built.
    /// </summary>
    private bool WithinTheWordCap(string word) {
        return MaxSegmentWords is not { } cap || word.Count(character => character == ' ') < cap;
    }

    private IReadOnlyList<string> Memoise(Dictionary<string, IReadOnlyList<string>>          cache,
                                          IReadOnlyDictionary<string, IReadOnlyList<string>> words,
                                          NounEntry                                               noun,
                                          Func<NounEntry, string, bool>                           fits) {
        if (cache.TryGetValue(noun.Value, out IReadOnlyList<string>? cached)) { return cached; }

        List<string>          pool    = Resolve(words, noun);
        IReadOnlyList<string> reduced = Narrows ? [.. pool.Where(word => fits(noun, word))] : pool;
        cache[noun.Value] = reduced;

        return reduced;
    }

}