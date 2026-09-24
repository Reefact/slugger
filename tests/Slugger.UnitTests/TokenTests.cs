#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What <c>docs/ubiquitous-language.md</c> says a token is: the characters a slug ends with,
///     drawn at random and not taken from a theme. Written from that page and from what a scripted
///     draw has to be able to count on.
/// </summary>
public sealed class TokenTests {

    /// <summary>A token is as long as it was asked to be, whatever the draws come back with.</summary>
    [Fact]
    public void Is_as_long_as_the_draw_asked_for() {
        // Setup
        int          length = Any.Int32().Between(1, 8).Generate();
        IRandomSource random = new ScriptedRandomSource([.. Enumerable.Repeat(0, length)]);

        // Exercise
        Token token = Token.Draw(random, length, TokenAlphabet.Decimal);

        // Verify
        Assert.Equal(length, token.Length);
    }

    /// <summary>
    ///     Literal on purpose: the script names each position in the alphabet, so the token it
    ///     produces is the one written down here and not merely one of the right shape.
    /// </summary>
    [Fact]
    public void Draws_its_characters_from_the_decimal_digits() {
        // Setup - the fourth digit, the second, the last.
        ScriptedRandomSource random = new(3, 1, 9);

        // Exercise
        Token token = Token.Draw(random, 3, TokenAlphabet.Decimal);

        // Verify
        Assert.Equal("319", token.ToString());
    }

    /// <summary>
    ///     Literal on purpose: the six positions above nine are what separates the two alphabets,
    ///     so the case has to reach them.
    /// </summary>
    [Fact]
    public void Reaches_the_letters_above_nine_when_the_alphabet_is_hexadecimal() {
        // Setup - positions ten and fifteen, which decimal does not have.
        ScriptedRandomSource random = new(10, 15);

        // Exercise
        Token token = Token.Draw(random, 2, TokenAlphabet.Hexadecimal);

        // Verify
        Assert.Equal("af", token.ToString());
    }

    /// <summary>
    ///     A certainty has nothing to decide, so it does not roll. A scripted test counts on that:
    ///     a roll taken here would shift every draw written down after it.
    /// </summary>
    [Fact]
    public void Takes_no_roll_when_the_token_is_certain() {
        // Setup - two draws for two characters, and not one more.
        ScriptedRandomSource random = new(0, 0);

        // Exercise
        Token.Draw(random, 2, TokenAlphabet.Decimal, Chance.Always);

        // Verify
        Assert.Equal(0, random.Remaining);
    }

    /// <summary>And an impossibility has nothing to decide either, so it draws nothing at all.</summary>
    [Fact]
    public void Takes_no_roll_and_no_draw_when_the_token_is_impossible() {
        // Setup - a script with nothing in it, which throws on any draw.
        ScriptedRandomSource random = new();

        // Exercise
        Token? token = Token.Draw(random, 4, TokenAlphabet.Decimal, Chance.Never);

        // Verify
        Assert.Null(token);
    }

    /// <summary>
    ///     Between the two ends, the roll comes first and decides. A roll that lands on or above
    ///     the chance leaves no token, and the digits are never drawn.
    /// </summary>
    [Fact]
    public void Rolls_before_the_digits_and_leaves_no_token_when_the_roll_falls_short() {
        // Setup - one roll and nothing else, so drawing a digit would throw.
        ScriptedRandomSource random = new(50);

        // Exercise
        Token? token = Token.Draw(random, 4, TokenAlphabet.Decimal, Chance.FromOrThrow(50));

        // Verify
        Assert.Null(token);
        Assert.Equal(0, random.Remaining);
    }

    /// <summary>The other side of the same roll: under the chance, the digits follow.</summary>
    [Fact]
    public void Draws_the_digits_when_the_roll_comes_in_under_the_chance() {
        // Setup - a roll of zero, then one digit.
        ScriptedRandomSource random = new(0, 7);

        // Exercise
        Token? token = Token.Draw(random, 1, TokenAlphabet.Decimal, Chance.FromOrThrow(1));

        // Verify
        Assert.Equal("7", token?.ToString());
    }

    /// <summary>Two tokens spelled the same are the same token, whatever draw produced them.</summary>
    [Fact]
    public void Two_tokens_spelled_the_same_are_the_same_token() {
        // Verify
        Assert.Equal(
            Token.Draw(new ScriptedRandomSource(4, 2), 2, TokenAlphabet.Decimal),
            Token.Draw(new ScriptedRandomSource(4, 2), 2, TokenAlphabet.Hexadecimal));
    }

    /// <summary>And two spelled differently are not, which an equality always agreeing would pass.</summary>
    [Fact]
    public void Two_tokens_spelled_differently_are_different_tokens() {
        // Verify
        Assert.NotEqual(
            Token.Draw(new ScriptedRandomSource(4, 2), 2, TokenAlphabet.Decimal),
            Token.Draw(new ScriptedRandomSource(4, 3), 2, TokenAlphabet.Decimal));
    }

    /// <summary>
    ///     A token of no characters is no token, so the request is refused - and refused in the
    ///     domain's own words. A caller catching what the domain throws would not see an argument
    ///     exception go past.
    /// </summary>
    [Fact]
    public void Refuses_to_draw_a_token_of_no_characters() {
        // Exercise
        TokenException thrown = Assert.Throws<TokenException>(
            () => Token.Draw(new ScriptedRandomSource(), 0, TokenAlphabet.Decimal));

        // Verify
        Assert.Equal(TokenError.Codes.LengthBelowOne, thrown.Error.Code);
    }

    /// <summary>
    ///     An alphabet answers for the positions it holds. A random source answering above the bound
    ///     it was given is the case this guards, and it is a rule of the domain like the others.
    /// </summary>
    [Fact]
    public void Refuses_a_position_outside_the_alphabet() {
        // Exercise
        TokenAlphabetException thrown = Assert.Throws<TokenAlphabetException>(() => TokenAlphabet.Decimal.GetDigit(10));

        // Verify
        Assert.Equal(TokenAlphabetError.Codes.PositionOutsideTheAlphabet, thrown.Error.Code);
    }

}
