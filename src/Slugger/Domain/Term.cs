#region Usings declarations

using System.Diagnostics;
using System.Text;

using FirstClassErrors;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     What a theme draws: a noun, an adjective or a participle, whichever of the three, made of
///     one or more <see cref="Word" />. It is the unit of the draw - a term is drawn whole, never
///     in part.
/// </summary>
/// <remarks>
///     <para>
///         A term is where DEC0008 applies. A word refuses a boundary, since reducing one would
///         change how many words the caller asked for; a term reduces it, since how many words it
///         holds is what it is there to say. So "rock crystal" is a term of two words, "jack
///         o'neil" one of three, and neither is a refusal.
///     </para>
///     <para>
///         How long a term comes out is not asked here: that depends on the separator, the casing
///         and the fold, none of which the domain knows before a run. <see cref="WordCount" /> is
///         what a term can answer on its own, and it is the figure a cap on words per term reads.
///     </para>
/// </remarks>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Term : ValueType<Term> {

    #region Static members

    /// <summary>The term that value spells, or the reason it spells none.</summary>
    /// <param name="value">A theme's entry, written as its author wrote it.</param>
    public static Outcome<Term> From(string value) {
        ArgumentNullException.ThrowIfNull(value);

        List<Word> words = WordsOf(value);
        if (words.Count == 0) { return Outcome<Term>.Failure(TermError.Empty(value)); }

        return Outcome<Term>.Success(new Term(words));
    }

    /// <summary>The same, for a caller that has no report to fill.</summary>
    /// <param name="value">A theme's entry, written as its author wrote it.</param>
    /// <exception cref="TermException">The value spells no term; the exception carries the reason.</exception>
    public static Term FromOrThrow(string value) {
        Outcome<Term> outcome = From(value);
        if (outcome.Error is TermError refused) { throw refused.ToException(); }

        return outcome.GetResultOrThrow();
    }

    /// <summary>
    ///     DEC0008 read straight into words: a letter or a digit continues the word being spelled,
    ///     anything else ends it. A run of boundaries ends nothing twice, and one at either end of
    ///     the value ends a word nobody had started - so no empty word can come out of here, which
    ///     is why <see cref="Word.FromOrThrow" /> below can never refuse.
    /// </summary>
    private static List<Word> WordsOf(string value) {
        List<Word>    words    = [];
        StringBuilder spelling = new(value.Length);

        foreach (char character in value) {
            if (char.IsLetterOrDigit(character)) {
                spelling.Append(character);

                continue;
            }

            Flush(words, spelling);
        }

        Flush(words, spelling);

        return words;
    }

    private static void Flush(List<Word> words, StringBuilder spelling) {
        if (spelling.Length == 0) { return; }

        words.Add(Word.FromOrThrow(spelling.ToString()));
        spelling.Clear();
    }

    #endregion

    #region Fields

    private readonly IReadOnlyList<Word> _words;

    #endregion

    #region Constructors & Destructor

    private Term(IReadOnlyList<Word> words) {
        _words = words;
    }

    #endregion

    /// <summary>How many words the term holds, which is what a cap on words per term counts.</summary>
    public int WordCount => _words.Count;

    /// <summary>The words it holds, spaced as a theme writes them.</summary>
    [DehydrationMethod]
    public string Dehydrate() {
        return string.Join(' ', _words.Select(word => word.Dehydrate()));
    }

    /// <summary>The term, for a human reading a watch window: its words, spaced as a theme writes them.</summary>
    public override string ToString() {
        return string.Join(' ', _words);
    }

    /// <summary>
    ///     The words, in order. Order counts: "crystal rock" is not the term "rock crystal".
    /// </summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        foreach (Word word in _words) {
            yield return word;
        }
    }

}
