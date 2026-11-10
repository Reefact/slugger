#region Usings declarations

using Slugger.Domain.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     How the facts of an error are read back. A caller branches on them - the CLI reads the theme
///     name of a refusal to decide which hint to give - so a fact that reads back wrong is a hint
///     that is given wrong.
/// </summary>
public sealed class ErrorContextTests {

    [Fact]
    public void Reads_back_a_fact_with_the_key_that_wrote_it() {
        // Setup
        ErrorContextKey<int> counted = new(Dummies.AnyWord());
        int                  fact    = Any.Int32().Between(1, 100_000).Generate();

        // Exercise
        ErrorContext context = ErrorContextBuilder.Build(builder => builder.Add(counted, fact));

        // Verify
        Assert.True(context.TryGet(counted, out int read));
        Assert.Equal(fact, read);
    }

    [Fact]
    public void Reads_nothing_under_a_key_nothing_was_written_with() {
        // Setup
        ErrorContextKey<string> written = new(Dummies.AnyWord());
        ErrorContextKey<string> other   = new(Dummies.AnyWordOtherThan(written.Name));

        // Exercise
        ErrorContext context = ErrorContextBuilder.Build(builder => builder.Add(written, Dummies.AnyWord()));

        // Verify
        Assert.False(context.TryGet(other, out string? _));
    }

    [Fact]
    public void Reads_nothing_from_an_error_that_recorded_no_fact() {
        // Exercise
        ErrorContext context = ErrorContextBuilder.Build(null);

        // Verify
        Assert.False(context.TryGet(new ErrorContextKey<string>(Dummies.AnyWord()), out string? _));
    }

    /// <summary>
    ///     A key's type is what makes the read safe: a key declared for a number never hands back the
    ///     string another key of the same name wrote.
    /// </summary>
    [Fact]
    public void Reads_nothing_when_the_fact_is_of_another_type() {
        // Setup
        string                  name    = Dummies.AnyWord();
        ErrorContextKey<string> written = new(name);
        ErrorContextKey<int>    asked   = new(name);

        // Exercise
        ErrorContext context = ErrorContextBuilder.Build(builder => builder.Add(written, Dummies.AnyWord()));

        // Verify
        Assert.False(context.TryGet(asked, out int _));
    }

    /// <summary>
    ///     Several catalogues declare a <c>Value</c> key of their own; a fact is found by its key's
    ///     name, so any of them reads what another wrote.
    /// </summary>
    [Fact]
    public void Reads_a_fact_written_by_another_key_of_the_same_name_and_type() {
        // Setup
        string                  name    = Dummies.AnyWord();
        ErrorContextKey<string> written = new(name);
        ErrorContextKey<string> asked   = new(name);
        string                  fact    = Dummies.AnyWord();

        // Exercise
        ErrorContext context = ErrorContextBuilder.Build(builder => builder.Add(written, fact));

        // Verify
        Assert.True(context.TryGet(asked, out string? read));
        Assert.Equal(fact, read);
    }

    /// <summary>A recorded null is no fact, so a caller that found one never has to test it again.</summary>
    [Fact]
    public void Reads_a_null_fact_as_none() {
        // Setup
        ErrorContextKey<string> key = new(Dummies.AnyWord());

        // Exercise
        ErrorContext context = ErrorContextBuilder.Build(builder => builder.Add(key, null!));

        // Verify
        Assert.False(context.TryGet(key, out string? _));
    }

    [Fact]
    public void Keeps_the_last_of_two_facts_written_with_the_same_key() {
        // Setup
        ErrorContextKey<string> key  = new(Dummies.AnyWord());
        string                  last = Dummies.AnyWord();

        // Exercise
        ErrorContext context = ErrorContextBuilder.Build(builder => builder.Add(key, Dummies.AnyWord()).Add(key, last));

        // Verify
        Assert.True(context.TryGet(key, out string? read));
        Assert.Equal(last, read);
    }

}
