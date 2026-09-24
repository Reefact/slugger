#region Usings declarations

using System.Diagnostics;
using System.Text;

using FirstClassErrors;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     The characters a slug ends with, drawn at random rather than taken from a theme. It is
///     neither a term nor a word: nothing in the vocabulary spells it.
/// </summary>
/// <remarks>
///     <para>
///         Which is why <see cref="Draw(IRandomSource, int, TokenAlphabet, Chance)" /> is the only way
///         to one, and why the constructor below is private. A term is read from a file and so can
///         be malformed; a token is produced, and the only thing that can be wrong about one is the
///         request that drew it - so a token that exists was drawn, and a token that was drawn is
///         valid.
///     </para>
///     <para>
///         How many draws a request takes from the source is fixed rather than incidental, because
///         a scripted test counts on it: a certainty and an impossibility both skip the roll, and
///         everything between rolls once before the digits.
///     </para>
/// </remarks>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Token : ValueType<Token> {

    #region Static members

    /// <summary>Draws a token of that many characters, or returns null when the chance says not to.</summary>
    /// <param name="random">Where the draw comes from.</param>
    /// <param name="length">How many characters to draw. At least one - a token of none is no token.</param>
    /// <param name="alphabet">What to draw them from.</param>
    /// <param name="chance">How often a token appears at all.</param>
    /// <exception cref="TokenException">The length is below one.</exception>
    public static Token? Draw(IRandomSource random, int length, TokenAlphabet alphabet, Chance chance) {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(alphabet);
        ArgumentNullException.ThrowIfNull(chance);

        if (length < 1) { throw TokenError.LengthBelowOne(length).ToException(); }
        if (chance.IsNever) { return null; }
        if (TheRollFallsShort(random, chance)) { return null; }

        StringBuilder drawn = new(length);
        for (int written = 0; written < length; written++) {
            int  position = random.Next(alphabet.Length);
            char digit    = alphabet.GetDigit(position);
            drawn.Append(digit);
        }

        return new Token(drawn.ToString());
    }

    /// <summary>A token that always appears, for a caller that has no chance to apply.</summary>
    /// <param name="random">Where the draw comes from.</param>
    /// <param name="length">How many characters to draw.</param>
    /// <param name="alphabet">What to draw them from.</param>
    /// <exception cref="TokenException">The length is below one.</exception>
    public static Token Draw(IRandomSource random, int length, TokenAlphabet alphabet) {
        return Draw(random, length, alphabet, Chance.Always)!;
    }

    /// <summary>
    ///     Whether this draw's roll leaves no token. Next(100) lands in 0..99, so a chance of 100
    ///     always draws and one of 1 draws a hundredth of the time. Neither end rolls: a certainty
    ///     and an impossibility have nothing to decide, and a roll they do not need would shift
    ///     every draw a scripted test wrote down after it.
    /// </summary>
    /// <param name="random">Where the roll comes from.</param>
    /// <param name="chance">How often a token appears.</param>
    private static bool TheRollFallsShort(IRandomSource random, Chance chance) {
        if (chance.IsAlways) { return false; }

        int roll = random.Next(100);

        return chance.DoesNotCover(roll);
    }

    #endregion

    #region Fields

    private readonly string _digits;

    #endregion

    #region Constructors & Destructor

    private Token(string digits) {
        _digits = digits;
    }

    #endregion

    /// <summary>How many characters the token carries, which a length budget counts like any other.</summary>
    public int Length => _digits.Length;

    /// <summary>The token, for a human reading a watch window.</summary>
    public override string ToString() {
        return _digits;
    }

    /// <summary>Two tokens spelled the same are the same token, whatever draw produced them.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _digits;
    }

}
