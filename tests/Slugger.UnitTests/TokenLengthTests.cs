#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     How many characters a token carries. These pin what the type holds so that a draw does not
///     check it again - which is the only reason it exists rather than an int.
/// </summary>
public sealed class TokenLengthTests {

    /// <summary>One character is a token, and so is any count above it.</summary>
    [Fact]
    public void Reads_any_count_of_one_or_more() {
        // Setup
        int count = Any.Int32().Between(1, 64).Generate();

        // Exercise
        Outcome<TokenLength> outcome = TokenLength.From(count);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     Nought is not a short token, it is no token - which a caller says by drawing none. So it
    ///     names no length, and neither does anything below it.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Refuses_a_count_that_leaves_no_character(int count) {
        // Exercise
        Outcome<TokenLength> outcome = TokenLength.From(count);

        // Verify
        Assert.Equal(TokenLengthError.Codes.BelowOne, outcome.Error!.Code);
    }

    /// <summary>The other door, carrying the same reason.</summary>
    [Fact]
    public void Throws_only_where_the_caller_asked_for_that() {
        // Exercise
        TokenLengthException thrown = Assert.Throws<TokenLengthException>(() => TokenLength.FromOrThrow(0));

        // Verify
        Assert.Equal(TokenLengthError.Codes.BelowOne, thrown.Error.Code);
    }

    /// <summary>
    ///     What a draw asks it, one character at a time. Both sides of the boundary, since an
    ///     off-by-one here writes a token of the wrong size.
    /// </summary>
    [Fact]
    public void Reaches_every_position_below_it_and_no_others() {
        // Setup
        TokenLength three = TokenLength.FromOrThrow(3);

        // Verify
        Assert.True(three.Reaches(0));
        Assert.True(three.Reaches(2));
        Assert.False(three.Reaches(3));
    }

    /// <summary>
    ///     The count read out, which a buffer being sized needs and an answer cannot give. Explicit
    ///     so that every place taking the number says so.
    /// </summary>
    [Fact]
    public void Gives_up_its_count_to_a_caller_that_asks_for_it() {
        // Setup
        int count = Any.Int32().Between(1, 64).Generate();

        // Verify
        Assert.Equal(count, (int)TokenLength.FromOrThrow(count));
    }

    /// <summary>Two lengths of the same count are the same length.</summary>
    [Fact]
    public void Two_lengths_of_the_same_count_are_the_same() {
        // Setup
        int count = Any.Int32().Between(1, 64).Generate();

        // Verify
        Assert.Equal(TokenLength.FromOrThrow(count), TokenLength.FromOrThrow(count));
    }

    /// <summary>What a debugger shows: the count and what it counts.</summary>
    [Fact]
    public void Reads_as_a_count_of_characters_where_a_debugger_shows_it() {
        // Verify
        Assert.Equal("4 characters", TokenLength.FromOrThrow(4).ToString());
    }

}
