#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     The characters a token is drawn from, and how to reach one of them.
/// </summary>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class TokenAlphabet : ValueType<TokenAlphabet> {

    #region Static members

    /// <summary>The ten decimal digits, which is what docker's collision suffix uses.</summary>
    public static TokenAlphabet Decimal { get; } = new("0123456789");

    /// <summary>The same ten and the six letters above them, lowercase.</summary>
    public static TokenAlphabet Hexadecimal { get; } = new("0123456789abcdef");

    #endregion

    #region Fields

    private readonly string _digits;

    #endregion

    #region Constructors & Destructor

    private TokenAlphabet(string digits) {
        _digits = digits;
    }

    #endregion

    /// <summary>How many characters it holds, which is the bound a draw asks its source for.</summary>
    public int Length => _digits.Length;

    /// <summary>The character at that position.</summary>
    /// <param name="index">A position, from zero to <see cref="Length" /> exclusive.</param>
    /// <exception cref="TokenAlphabetException">The position is outside the alphabet.</exception>
    public char GetDigit(int index) {
        if (index < 0 || index >= Length) {
            throw TokenAlphabetError.PositionOutsideTheAlphabet(index, Length).ToException();
        }

        return _digits[index];
    }

    /// <summary>The alphabet, for a human reading a watch window.</summary>
    public override string ToString() {
        return _digits;
    }

    /// <summary>An alphabet is the characters it holds.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _digits;
    }

}
