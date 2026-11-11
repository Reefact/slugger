#region Usings declarations

using Slugger.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What a load gives back to a library consumer: a result, or the error that says why there is
///     none - never both, and never neither.
/// </summary>
public sealed class OutcomeTests {

    #region Static members

    private static DomainError AnyError() {
        return DomainError.Create(ErrorCode.Create(Dummies.AnyWord()), Dummies.AnyWord()).WithPublicMessage(Dummies.AnyWord());
    }

    #endregion

    [Fact]
    public void A_success_gives_back_its_result_and_carries_no_error() {
        // Setup
        string result = Dummies.AnyWord();

        // Exercise
        Outcome<string> outcome = Outcome<string>.Success(result);

        // Verify
        Assert.True(outcome.IsSuccess);
        Assert.False(outcome.IsFailure);
        Assert.Null(outcome.Error);
        Assert.Equal(result, outcome.GetResultOrThrow());
    }

    [Fact]
    public void A_failure_carries_the_error_it_was_refused_for() {
        // Setup
        DomainError error = AnyError();

        // Exercise
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Verify
        Assert.True(outcome.IsFailure);
        Assert.False(outcome.IsSuccess);
        Assert.Same(error, outcome.Error);
    }

    /// <summary>The throwing forms of the facade are this, so what they raise is what this raises.</summary>
    [Fact]
    public void A_failure_asked_for_its_result_raises_its_error() {
        // Setup
        DomainError     error   = AnyError();
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise
        DomainException raised = Assert.Throws<DomainException>(outcome.GetResultOrThrow);

        // Verify
        Assert.Same(error, raised.Error);
    }

    [Fact]
    public void Refuses_to_succeed_with_nothing() {
        // Exercise
        Assert.Throws<ArgumentNullException>(() => Outcome<string>.Success(null!));
    }

    [Fact]
    public void Refuses_to_fail_without_a_reason() {
        // Exercise
        Assert.Throws<ArgumentNullException>(() => Outcome<string>.Failure(null!));
    }

}
