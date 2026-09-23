#region Usings declarations

using Slugger.Domain;
using Slugger.Domain.Normalization;
using Slugger.Domain.Resolution;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     Reads a finished slug back into the words a theme declares, and says whether the theme
///     allows them together. The check the engine cannot perform on itself: it works from the
///     JSON's own lists and rebuilds the category algebra rather than asking
///     <see cref="ThemeResolver" />, so a resolver that let a word through would still be caught.
/// </summary>
/// <remarks>
///     <para>
///         <b>A prefix is not a word.</b> Longest match, and every candidate noun rather than only
///         the longest: a theme that declares <c>head cheese</c> and <c>soured head cheese</c>
///         leaves the first word of <c>rhubarb-soured-head-cheese</c> orphaned if the longer noun is
///         taken and never reconsidered, although the slug reads perfectly well the other way.
///     </para>
///     <para>
///         <b>One valid reading is enough.</b> A slug is only a fault when no reading of it holds,
///         because an adjective written in two words looks exactly like an adjective followed by a
///         participle. Reporting the first reading that fails would accuse the theme of the reader's
///         ambiguity.
///     </para>
/// </remarks>
internal sealed class SlugDecomposer {

    #region Static members

    /// <summary>
    ///     A declared value as the slug writes it. Canonicalising again is deliberate rather than
    ///     redundant: it is what the loader did, so this reads the same word the draw did.
    /// </summary>
    private static string AsDrawn(string value, char separator) {
        return WordNormalizer.Canonicalize(value).Replace(' ', separator);
    }

    /// <summary>One word against the noun it was drawn for: refused by name, or out of reach.</summary>
    private static string? FaultOf(string?                                            word,
                                   Noun                                               noun,
                                   IReadOnlyDictionary<string, IReadOnlyList<string>> declared,
                                   Dictionary<Noun, HashSet<string>>                  memoised) {
        if (word is null) { return null; }
        if (noun.Except.Contains(word)) { return $"\"{word}\" is refused by \"{noun.Value}\" itself"; }
        if (PoolOf(noun, declared, memoised).Contains(word)) { return null; }

        return $"\"{word}\" is in no category \"{noun.Value}\" reaches";
    }

    /// <summary>
    ///     The words this noun reaches, rebuilt from the file rather than asked of the engine:
    ///     its own categories and "common" on top, minus what it refuses (DEC0001, DEC0002).
    /// </summary>
    private static HashSet<string> PoolOf(Noun                                               noun,
                                          IReadOnlyDictionary<string, IReadOnlyList<string>> declared,
                                          Dictionary<Noun, HashSet<string>>                  memoised) {
        if (memoised.TryGetValue(noun, out HashSet<string>? known)) { return known; }

        HashSet<string> pool = new(StringComparer.Ordinal);
        foreach (string category in noun.Categories.Append(ThemeResolver.CommonCategory)) {
            if (!declared.TryGetValue(category, out IReadOnlyList<string>? words)) { continue; }

            pool.UnionWith(words);
        }

        pool.ExceptWith(noun.Except);
        memoised[noun] = pool;

        return pool;
    }

    #endregion

    #region Fields

    private readonly Theme                             _theme;
    private readonly char                              _separator;
    private readonly Dictionary<string, Noun>          _nouns           = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>>  _byLastWord      = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>>  _endingWith      = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string>        _adjectives      = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string>        _participles     = new(StringComparer.Ordinal);
    private readonly Dictionary<Noun, HashSet<string>> _adjectivePools  = [];
    private readonly Dictionary<Noun, HashSet<string>> _participlePools = [];

    #endregion

    #region Constructors & Destructor

    /// <param name="theme">The theme whose own lists the slug is read against.</param>
    /// <param name="separator">
    ///     What joins the slug's segments and the words inside one, which is what a declared value
    ///     looks like once it reaches the slug.
    /// </param>
    internal SlugDecomposer(Theme theme, char separator) {
        ArgumentNullException.ThrowIfNull(theme);

        _theme     = theme;
        _separator = separator;

        foreach (Noun noun in theme.Nouns) {
            string drawn = AsDrawn(noun.Value, separator);
            _nouns[drawn] = noun;

            // Indexed by last word rather than scanned whole: a slug ends with its noun, so they
            // share that word, and a theme of six hundred nouns is then a handful of candidates
            // instead of six hundred string comparisons per slug.
            string last = drawn[(drawn.LastIndexOf(separator) + 1)..];
            if (!_byLastWord.TryGetValue(last, out List<string>? sharing)) {
                sharing           = [];
                _byLastWord[last] = sharing;
            }

            sharing.Add(drawn);
        }

        foreach (string word in theme.Adjectives.SelectMany(category => category.Value)) {
            _adjectives[AsDrawn(word, separator)] = word;
        }

        foreach (string word in theme.Participles.SelectMany(category => category.Value)) {
            string drawn = AsDrawn(word, separator);
            _participles[drawn] = word;

            // Same trick as the nouns: a participle is a suffix of what stands in front of the
            // noun, so the two share their last word.
            string last = drawn[(drawn.LastIndexOf(separator) + 1)..];
            if (!_endingWith.TryGetValue(last, out List<string>? sharing)) {
                sharing           = [];
                _endingWith[last] = sharing;
            }

            sharing.Add(drawn);
        }
    }

    #endregion

    /// <summary>Every way the slug can be read, or none at all when it holds no declared noun.</summary>
    /// <param name="slug">A slug the engine produced, as it would be written down.</param>
    internal IReadOnlyList<SlugReading> Readings(string slug) {
        ArgumentNullException.ThrowIfNull(slug);

        foreach (string candidate in NounsEndingIt(slug)) {
            Noun   noun = _nouns[candidate];
            string head = slug[..(slug.Length - candidate.Length)].TrimEnd(_separator);
            if (head.Length == 0) { return [new SlugReading(null, null, noun)]; }

            List<SlugReading> readings = ReadingsOf(head, noun);
            if (readings.Count > 0) { return readings; }
        }

        return [];
    }

    /// <summary>
    ///     Why this reading cannot be, or null where the theme allows it. One sentence naming the
    ///     word and the noun, because a failure is read by whoever did not write the theme.
    /// </summary>
    /// <param name="reading">One way of reading a slug back.</param>
    internal string? Fault(SlugReading reading) {
        ArgumentNullException.ThrowIfNull(reading);

        string? adjective = FaultOf(reading.Adjective, reading.Noun, _theme.Adjectives, _adjectivePools);
        if (adjective is not null) { return adjective; }

        string? participle = FaultOf(reading.Participle, reading.Noun, _theme.Participles, _participlePools);
        if (participle is not null) { return participle; }

        return RefusedBeside(reading);
    }

    /// <summary>The pair the theme declares impossible, which only a three-segment slug can form (DEC0017).</summary>
    private string? RefusedBeside(SlugReading reading) {
        if (reading is not { Adjective: { } drawn, Participle: { } beside }) { return null; }
        if (!_theme.Incompatible.TryGetValue(drawn, out IReadOnlyList<string>? refused)) { return null; }
        if (!refused.Contains(beside)) { return null; }

        return $"\"{drawn}\" refuses \"{beside}\" beside it";
    }

    /// <summary>Every declared noun the slug ends with, longest first so the whole name wins.</summary>
    private IEnumerable<string> NounsEndingIt(string slug) {
        string last = slug[(slug.LastIndexOf(_separator) + 1)..];
        if (!_byLastWord.TryGetValue(last, out List<string>? sharing)) { return []; }

        return sharing.Where(noun => slug.Equals(noun, StringComparison.Ordinal)
                                  || slug.EndsWith(_separator + noun, StringComparison.Ordinal))
                      .OrderByDescending(noun => noun.Length);
    }

    /// <summary>What can stand in front of the noun: one word, or an adjective and a participle.</summary>
    private List<SlugReading> ReadingsOf(string head, Noun noun) {
        List<SlugReading> readings = [];
        if (_adjectives.TryGetValue(head, out string? alone)) {
            readings.Add(new SlugReading(alone, null, noun));
        }

        if (_participles.TryGetValue(head, out string? single)) {
            readings.Add(new SlugReading(null, single, noun));
        }

        string last = head[(head.LastIndexOf(_separator) + 1)..];
        if (!_endingWith.TryGetValue(last, out List<string>? ending)) { return readings; }

        foreach (string written in ending) {
            if (!head.EndsWith(_separator + written, StringComparison.Ordinal)) { continue; }

            string front = head[..(head.Length - written.Length)].TrimEnd(_separator);
            if (_adjectives.TryGetValue(front, out string? adjective)) {
                readings.Add(new SlugReading(adjective, _participles[written], noun));
            }
        }

        return readings;
    }

}