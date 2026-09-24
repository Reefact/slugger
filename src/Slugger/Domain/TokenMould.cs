#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     What a token is drawn from: the alphabet its characters are taken from, and how many it
///     counts. It says what a token looks like and nothing else - whether one appears at all is the
///     slug's question, and a <see cref="Chance" /> answers it.
/// </summary>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class TokenMould : ValueType<TokenMould> {

    #region Static members

    /// <summary>A mould of that many characters, taken from that alphabet.</summary>
    /// <param name="alphabet">Where the characters come from.</param>
    /// <param name="length">How many of them.</param>
    public static TokenMould Of(TokenAlphabet alphabet, TokenLength length) {
        ArgumentNullException.ThrowIfNull(alphabet);
        ArgumentNullException.ThrowIfNull(length);

        return new TokenMould(alphabet, length);
    }

    #endregion

    #region Fields

    private readonly TokenAlphabet _alphabet;
    private readonly TokenLength   _length;

    #endregion

    #region Constructors & Destructor

    private TokenMould(TokenAlphabet alphabet, TokenLength length) {
        _alphabet = alphabet;
        _length   = length;
    }

    #endregion

    /// <summary>How many characters it makes, for a caller sizing what it writes them into.</summary>
    public int CharacterCount => (int)_length;

    /// <summary>Whether a character is still owed at that position, so a draw knows to carry on.</summary>
    /// <param name="written">How many characters have been written so far.</param>
    public bool Reaches(int written) {
        return _length.Reaches(written);
    }

    /// <summary>One character of the alphabet, drawn from that source.</summary>
    /// <param name="random">Where the draw comes from.</param>
    public char DigitFrom(IRandomSource random) {
        ArgumentNullException.ThrowIfNull(random);

        int position = random.Next(_alphabet.Length);

        return _alphabet.GetDigit(position);
    }

    /// <summary>The mould, for a human reading a watch window.</summary>
    public override string ToString() {
        return $"{_length} of {_alphabet}";
    }

    /// <summary>Two moulds of the same alphabet and the same length are the same mould.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _alphabet;
        yield return _length;
    }

}
