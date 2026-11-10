#region Usings declarations

using Slugger.Domain.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What an error holds once its factory has built it. Pinned through <see cref="DomainError" />,
///     since <see cref="Error" /> cannot be built on its own, and every catalogue's refusal goes
///     through the same constructor.
/// </summary>
public sealed class ErrorTests {

    #region Static members

    private static ErrorCode AnyCode() {
        return ErrorCode.Create(Dummies.AnyWord());
    }

    #endregion

    [Fact]
    public void Keeps_the_code_and_the_messages_its_factory_gave_it() {
        // Setup
        ErrorCode code       = AnyCode();
        string    diagnostic = Dummies.AnyWord();
        string    shortOne   = Dummies.AnyWord();
        string    detailed   = Dummies.AnyWord();

        // Exercise
        DomainError error = DomainError.Create(code, diagnostic).WithPublicMessage(shortOne, detailed);

        // Verify
        Assert.Equal(code, error.Code);
        Assert.Equal(diagnostic, error.DiagnosticMessage);
        Assert.Equal(shortOne, error.ShortMessage);
        Assert.Equal(detailed, error.DetailedMessage);
    }

    /// <summary>
    ///     Literal on purpose: the whitespace around each message is the whole of the case, and a
    ///     message is often built from an interpolated value that may bring some along.
    /// </summary>
    [Fact]
    public void Trims_the_whitespace_around_each_message() {
        // Exercise
        DomainError error = DomainError.Create(AnyCode(), "  the diagnostic\n").WithPublicMessage(" the summary ", "\tthe detail ");

        // Verify
        Assert.Equal("the diagnostic", error.DiagnosticMessage);
        Assert.Equal("the summary", error.ShortMessage);
        Assert.Equal("the detail", error.DetailedMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reads_a_missing_or_blank_detail_as_none(string? detail) {
        // Exercise
        DomainError error = DomainError.Create(AnyCode(), Dummies.AnyWord()).WithPublicMessage(Dummies.AnyWord(), detail);

        // Verify
        Assert.Null(error.DetailedMessage);
    }

    /// <summary>
    ///     A diagnostic is what whoever fixes the cause reads; an error without one has nothing to
    ///     tell them, and a factory that forgot it should fail the first test that builds it.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_blank_diagnostic(string diagnostic) {
        // Setup
        PublicMessageStage<DomainError> stage = DomainError.Create(AnyCode(), diagnostic);

        // Exercise
        Assert.Throws<ArgumentException>(() => stage.WithPublicMessage(Dummies.AnyWord()));
    }

    /// <summary>The same for what an end user is shown: a blank sentence tells them nothing either.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_blank_short_message(string shortMessage) {
        // Setup
        PublicMessageStage<DomainError> stage = DomainError.Create(AnyCode(), Dummies.AnyWord());

        // Exercise
        Assert.Throws<ArgumentException>(() => stage.WithPublicMessage(shortMessage));
    }

    [Fact]
    public void Stands_on_its_own_when_its_factory_gathers_nothing() {
        // Exercise
        DomainError error = DomainError.Create(AnyCode(), Dummies.AnyWord()).WithPublicMessage(Dummies.AnyWord());

        // Verify
        Assert.Empty(error.InnerErrors);
    }

    /// <summary>The grouped report DEC0006 asks for: every reason, in the order it was found.</summary>
    [Fact]
    public void Carries_every_error_it_gathers_in_the_order_they_were_found() {
        // Setup
        DomainError first  = DomainError.Create(AnyCode(), Dummies.AnyWord()).WithPublicMessage(Dummies.AnyWord());
        DomainError second = DomainError.Create(AnyCode(), Dummies.AnyWord()).WithPublicMessage(Dummies.AnyWord());

        // Exercise
        DomainError gathered = DomainError.Create(AnyCode(), Dummies.AnyWord(), [first, second]).WithPublicMessage(Dummies.AnyWord());

        // Verify
        Assert.Equal([first, second], gathered.InnerErrors);
    }

    [Fact]
    public void Carries_the_facts_its_factory_recorded() {
        // Setup
        ErrorContextKey<string> key  = new(Dummies.AnyWord());
        string                  fact = Dummies.AnyWord();

        // Exercise
        DomainError error = DomainError.Create(AnyCode(), Dummies.AnyWord(), context => context.Add(key, fact))
                                       .WithPublicMessage(Dummies.AnyWord());

        // Verify
        Assert.True(error.Context.TryGet(key, out string? read));
        Assert.Equal(fact, read);
    }

    [Fact]
    public void Is_written_out_as_its_diagnostic_followed_by_its_code() {
        // Setup
        ErrorCode code       = AnyCode();
        string    diagnostic = Dummies.AnyWord();

        // Exercise
        DomainError error = DomainError.Create(code, diagnostic).WithPublicMessage(Dummies.AnyWord());

        // Verify
        Assert.Equal($"{diagnostic} ({code.Dehydrate()})", error.ToString());
    }

    /// <summary>
    ///     A caller that throws rather than returns catches the exception and reads the error back
    ///     from it; its message is the diagnostic, so a log that prints only the message still says
    ///     what happened.
    /// </summary>
    [Fact]
    public void Raises_itself_as_a_domain_exception_whose_message_is_its_diagnostic() {
        // Setup
        string      diagnostic = Dummies.AnyWord();
        DomainError error      = DomainError.Create(AnyCode(), diagnostic).WithPublicMessage(Dummies.AnyWord());

        // Exercise
        DiagnosableException raised = error.ToException();

        // Verify
        DomainException domain = Assert.IsType<DomainException>(raised);
        Assert.Same(error, domain.Error);
        Assert.Equal(diagnostic, domain.Message);
    }

}
