#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What a token is drawn from, as <c>docs/ubiquitous-language.md</c> names it: an alphabet and
///     a count of characters, and nothing about whether a token appears at all.
/// </summary>
public sealed class TokenMouldTests {

    /// <summary>What it makes, for a caller sizing what it writes the characters into.</summary>
    [Fact]
    public void Counts_the_characters_it_makes() {
        // Setup
        int count = Any.Int32().Between(1, 32).Generate();

        // Exercise
        TokenMould mould = TokenMould.Of(TokenAlphabet.Decimal, TokenLength.FromOrThrow(count));

        // Verify
        Assert.Equal(count, mould.CharacterCount);
    }

    /// <summary>
    ///     The length's question, asked of the mould. Both sides of the boundary, since an off-by-one
    ///     here makes a token of the wrong size.
    /// </summary>
    [Fact]
    public void Reaches_every_position_below_its_length_and_no_others() {
        // Setup
        TokenMould mould = TokenMould.Of(TokenAlphabet.Decimal, TokenLength.FromOrThrow(3));

        // Verify
        Assert.True(mould.Reaches(0));
        Assert.True(mould.Reaches(2));
        Assert.False(mould.Reaches(3));
    }

    /// <summary>
    ///     The alphabet's question, asked of the mould: one character, at the position the source
    ///     answered with. Literal on purpose - the script names the position, so the character it
    ///     produces is the one written down here.
    /// </summary>
    [Fact]
    public void Draws_one_character_at_the_position_the_source_gives() {
        // Setup
        TokenMould mould = TokenMould.Of(TokenAlphabet.Hexadecimal, TokenLength.FromOrThrow(1));

        // Exercise
        char digit = mould.DigitFrom(new ScriptedRandomSource(10));

        // Verify
        Assert.Equal('a', digit);
    }

    /// <summary>Two moulds of the same alphabet and the same length are the same mould.</summary>
    [Fact]
    public void Two_moulds_of_the_same_alphabet_and_length_are_the_same() {
        // Verify
        Assert.Equal(
            TokenMould.Of(TokenAlphabet.Decimal, TokenLength.FromOrThrow(4)),
            TokenMould.Of(TokenAlphabet.Decimal, TokenLength.FromOrThrow(4)));
    }

    /// <summary>And either half differing makes another mould, which one comparison alone would miss.</summary>
    [Fact]
    public void Either_half_differing_makes_another_mould() {
        // Setup
        TokenMould four = TokenMould.Of(TokenAlphabet.Decimal, TokenLength.FromOrThrow(4));

        // Verify
        Assert.NotEqual(four, TokenMould.Of(TokenAlphabet.Hexadecimal, TokenLength.FromOrThrow(4)));
        Assert.NotEqual(four, TokenMould.Of(TokenAlphabet.Decimal, TokenLength.FromOrThrow(5)));
    }

    /// <summary>What a debugger shows: the count and where the characters come from.</summary>
    [Fact]
    public void Reads_as_its_length_and_its_alphabet_where_a_debugger_shows_it() {
        // Verify
        Assert.Equal(
            "2 characters of 0123456789abcdef",
            TokenMould.Of(TokenAlphabet.Hexadecimal, TokenLength.FromOrThrow(2)).ToString());
    }

}
