#region Usings declarations

using System.Diagnostics;

using FirstClassErrors;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     A word in the literal sense: letters and digits, nothing else. It is the smallest unit the
///     vocabulary names - a <c>Term</c> is made of one or more of them, and a slug of one or more
///     terms.
/// </summary>
/// <remarks>
///     <para>
///         A letter is what <see cref="char.IsLetterOrDigit(char)" /> says it is, so "rené" is one
///         word: DEC0008 has already turned everything else into a boundary by the time a value
///         reaches the domain.
///     </para>
///     <para>
///         The rule for what is normalized and what is refused: what cannot change how many words a
///         value spells is settled here - the case, the surrounding whitespace - and what can is
///         refused. Reducing a boundary would hand back one word out of "jack o'neil" and drop two.
///     </para>
///     <para>
///         <see cref="From" /> reports and <see cref="FromOrThrow" /> throws. The domain is built on
///         the first, since DEC0006 wants every reason a theme was refused and an exception carries
///         only the first.
///     </para>
/// </remarks>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Word : ValueType<Word> {

    #region Static members

    /// <summary>The word that value spells, lowercased, or the reason it is not one word.</summary>
    /// <param name="value">One word, of letters and digits. Surrounding whitespace is trimmed off.</param>
    public static Outcome<Word> From(string value) {
        ArgumentNullException.ThrowIfNull(value);

        // Trimmed before the emptiness test, so that whitespace alone is reported as the no word
        // it is rather than as several.
        string trimmed = value.Trim();
        if (trimmed.Length == 0) { return Outcome<Word>.Failure(WordError.Empty(value)); }

        char boundary = trimmed.FirstOrDefault(character => !char.IsLetterOrDigit(character));
        if (boundary != '\0') { return Outcome<Word>.Failure(WordError.NotOneWord(trimmed, boundary)); }

        return Outcome<Word>.Success(new Word(trimmed.ToLowerInvariant()));
    }

    /// <summary>The same, for a caller that has no report to fill.</summary>
    /// <param name="value">One word, of letters and digits. Surrounding whitespace is trimmed off.</param>
    /// <exception cref="WordException">The value is not one word; the exception carries the reason.</exception>
    public static Word FromOrThrow(string value) {
        // The rules live in From and are read once. This door only decides what a refusal
        // becomes, since GetResultOrThrow would raise a bare DomainException.
        Outcome<Word> outcome = From(value);
        if (outcome.Error is WordError refused) { throw refused.ToException(); }

        return outcome.GetResultOrThrow();
    }

    #endregion

    #region Fields

    private readonly string _value;

    #endregion

    #region Constructors & Destructor

    private Word(string value) {
        _value = value;
    }

    #endregion

    /// <summary>How many characters the word carries, which is what a length budget counts.</summary>
    public int Length => _value.Length;

    /// <summary>The word, for a human reading a watch window.</summary>
    /// <remarks>
    ///     A debugging aid, and not how the spelling leaves the type: rendering a word into a slug
    ///     is the formatter's business.
    /// </remarks>
    public override string ToString() {
        return _value;
    }

    /// <summary>Two words spelled the same are the same word, which is all a word is.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _value;
    }

}
