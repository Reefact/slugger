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
///         Which is why <see cref="Draw(TokenMould, IRandomSource, Chance)" /> is
///         the only way to one, and why the constructor below is private. Everything a request can
///         get wrong is held by the types it is made of, so a token that exists was drawn, and a
///         token that was drawn is valid.
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
    /// <param name="mould">What the token is drawn from.</param>
    /// <param name="random">Where the draw comes from.</param>
    /// <param name="chance">How often a token appears at all.</param>
    public static Token? Draw(TokenMould mould, IRandomSource random, Chance chance) {
        ArgumentNullException.ThrowIfNull(mould);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(chance);

        if (chance.IsNever) { return null; }
        if (TheRollFallsShort(chance, random)) { return null; }

        StringBuilder drawn = new(mould.CharacterCount);
        for (int written = 0; mould.Reaches(written); written++) {
            char digit = mould.DigitFrom(random);
            drawn.Append(digit);
        }

        return new Token(drawn.ToString());
    }

    /// <summary>A token that always appears, for a caller that has no chance to apply.</summary>
    /// <param name="mould">What the token is drawn from.</param>
    /// <param name="random">Where the draw comes from.</param>
    public static Token Draw(TokenMould mould, IRandomSource random) {
        return Draw(mould, random, Chance.Always)!;
    }

    /// <summary>
    ///     Whether this draw's roll leaves no token. Next(100) lands in 0..99, so a chance of 100
    ///     always draws and one of 1 draws a hundredth of the time. Neither end rolls: a certainty
    ///     and an impossibility have nothing to decide, and a roll they do not need would shift
    ///     every draw a scripted test wrote down after it.
    /// </summary>
    /// <param name="chance">How often a token appears.</param>
    /// <param name="random">Where the roll comes from.</param>
    private static bool TheRollFallsShort(Chance chance, IRandomSource random) {
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

    /// <summary>The digits it holds.</summary>
    [DehydrationMethod]
    public string Dehydrate() {
        return _digits;
    }

    /// <summary>The token, for a human reading a watch window.</summary>
    public override string ToString() {
        return _digits;
    }

    /// <summary>Two tokens spelled the same are the same token, whatever draw produced them.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _digits;
    }

}
