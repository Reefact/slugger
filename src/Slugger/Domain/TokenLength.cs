#region Usings declarations

using System.Diagnostics;

using FirstClassErrors;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     How many characters a token carries: one or more. Nought is not a length here - a slug that
///     carries no token says so by drawing none.
/// </summary>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class TokenLength : ValueType<TokenLength> {

    #region Static members

    /// <summary>The length that number names, or the reason it names none.</summary>
    /// <param name="value">A whole number of characters, one or more.</param>
    public static Outcome<TokenLength> From(int value) {
        if (value < 1) { return Outcome<TokenLength>.Failure(TokenLengthError.BelowOne(value)); }

        return Outcome<TokenLength>.Success(new TokenLength(value));
    }

    /// <summary>The same, for a caller that has no report to fill.</summary>
    /// <param name="value">A whole number of characters, one or more.</param>
    /// <exception cref="TokenLengthException">The number is below one; the exception carries the reason.</exception>
    public static TokenLength FromOrThrow(int value) {
        Outcome<TokenLength> outcome = From(value);
        if (outcome.Error is TokenLengthError refused) { throw refused.ToException(); }

        return outcome.GetResultOrThrow();
    }

    /// <summary>
    ///     The count itself, for a caller that needs the number rather than an answer - sizing a
    ///     buffer, and little else.
    /// </summary>
    /// <param name="length">The length to read.</param>
    public static explicit operator int(TokenLength length) {
        ArgumentNullException.ThrowIfNull(length);

        return length._value;
    }

    #endregion

    #region Fields

    private readonly int _value;

    #endregion

    #region Constructors & Destructor

    private TokenLength(int value) {
        _value = value;
    }

    #endregion

    /// <summary>Whether a character is still owed at that position, so a draw knows to carry on.</summary>
    /// <param name="written">How many characters have been written so far.</param>
    public bool Reaches(int written) {
        return written < _value;
    }

    /// <summary>The length, for a human reading a watch window.</summary>
    public override string ToString() {
        return $"{_value} characters";
    }

    /// <summary>Two lengths of the same count are the same length.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _value;
    }

}
