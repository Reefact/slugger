#region Usings declarations

using Slugger.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What a caller branches on. Two codes are compared far more often than they are read, so the
///     comparison is what these pin.
/// </summary>
public sealed class ErrorCodeTests {

    [Fact]
    public void Two_codes_spelled_the_same_are_the_same_kind_of_refusal() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Exercise
        ErrorCode first  = ErrorCode.Create(spelling);
        ErrorCode second = ErrorCode.Create(spelling);

        // Verify
        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Two_codes_spelled_differently_are_two_kinds_of_refusal() {
        // Setup
        string spelling = Dummies.AnyWord();
        string other    = Dummies.AnyWordOtherThan(spelling);

        // Exercise
        ErrorCode first  = ErrorCode.Create(spelling);
        ErrorCode second = ErrorCode.Create(other);

        // Verify
        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void Gives_back_its_spelling() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Exercise
        ErrorCode code = ErrorCode.Create(spelling);

        // Verify
        Assert.Equal(spelling, code.Dehydrate());
        Assert.Equal(spelling, code.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_blank_spelling(string spelling) {
        // Exercise
        Assert.Throws<ArgumentException>(() => ErrorCode.Create(spelling));
    }

}
