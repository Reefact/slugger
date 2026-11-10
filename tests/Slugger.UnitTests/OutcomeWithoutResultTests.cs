#region Usings declarations

using Slugger.Domain.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What an operation that gives nothing back returns, such as removing a registered theme:
///     success, or the error that says why it was refused.
/// </summary>
public sealed class OutcomeWithoutResultTests {

    [Fact]
    public void A_success_carries_no_error() {
        // Exercise
        Outcome outcome = Outcome.Success;

        // Verify
        Assert.True(outcome.IsSuccess);
        Assert.False(outcome.IsFailure);
        Assert.Null(outcome.Error);
    }

    [Fact]
    public void A_failure_carries_the_error_it_was_refused_for() {
        // Setup
        DomainError error = DomainError.Create(ErrorCode.Create(Dummies.AnyWord()), Dummies.AnyWord()).WithPublicMessage(Dummies.AnyWord());

        // Exercise
        Outcome outcome = Outcome.Failure(error);

        // Verify
        Assert.True(outcome.IsFailure);
        Assert.False(outcome.IsSuccess);
        Assert.Same(error, outcome.Error);
    }

    [Fact]
    public void Refuses_to_fail_without_a_reason() {
        // Exercise
        Assert.Throws<ArgumentNullException>(() => Outcome.Failure(null!));
    }

}
