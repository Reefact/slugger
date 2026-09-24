#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     A proportion of a hundred, whole. These pin what the type holds so that nothing reading one
///     has to check the range again - which is the only reason it exists rather than an int.
/// </summary>
public sealed class ChanceTests {

    /// <summary>Any figure of the range is a percentage, and the ends belong to it.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Reads_any_figure_from_nought_to_a_hundred(int value) {
        // Exercise
        Outcome<Chance> outcome = Chance.From(value);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>And a figure outside it names no proportion, so there is nothing to build.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Refuses_a_figure_outside_that_range(int value) {
        // Exercise
        Outcome<Chance> outcome = Chance.From(value);

        // Verify
        Assert.Equal(ChanceError.Codes.OutsideTheRange, outcome.Error!.Code);
    }

    /// <summary>The other door, carrying the same reason.</summary>
    [Fact]
    public void Throws_only_where_the_caller_asked_for_that() {
        // Exercise
        ChanceException thrown = Assert.Throws<ChanceException>(() => Chance.FromOrThrow(101));

        // Verify
        Assert.Equal(ChanceError.Codes.OutsideTheRange, thrown.Error.Code);
    }

    /// <summary>
    ///     The two ends decide without asking anything, which is what lets a caller skip a draw it
    ///     would otherwise have to make and then ignore.
    /// </summary>
    [Fact]
    public void Knows_the_two_ends_that_need_no_draw() {
        // Verify
        Assert.True(Chance.Never.IsNever);
        Assert.True(Chance.Always.IsAlways);
        Assert.False(Chance.FromOrThrow(50).IsNever);
        Assert.False(Chance.FromOrThrow(50).IsAlways);
    }

    /// <summary>
    ///     A roll of nought to ninety-nine covers a hundred outcomes, so a percentage of one takes
    ///     the roll that is nought and nothing else. Both sides of the boundary, since an off-by-one
    ///     here shifts every frequency the domain promises.
    /// </summary>
    [Fact]
    public void Covers_the_rolls_below_it_and_no_others() {
        // Setup
        Chance one = Chance.FromOrThrow(1);

        // Verify
        Assert.True(one.Covers(0));
        Assert.False(one.Covers(1));
        Assert.True(Chance.Always.Covers(99));
        Assert.False(Chance.Never.Covers(0));
    }

    /// <summary>
    ///     The same boundary read the other way round, which is what a caller with a negation to
    ///     spare asks instead. The two never disagree.
    /// </summary>
    [Fact]
    public void Says_what_it_does_not_cover_as_readily() {
        // Setup
        Chance one  = Chance.FromOrThrow(1);
        int    roll = Any.Int32().Between(0, 99).Generate();

        // Verify
        Assert.False(one.DoesNotCover(0));
        Assert.True(one.DoesNotCover(1));
        Assert.NotEqual(one.Covers(roll), one.DoesNotCover(roll));
    }

    /// <summary>Two chances of the same frequency are the same chance.</summary>
    [Fact]
    public void Two_chances_of_the_same_frequency_are_the_same() {
        // Setup
        int proportion = Any.Int32().Between(0, 100).Generate();

        // Verify
        Assert.Equal(Chance.FromOrThrow(proportion), Chance.FromOrThrow(proportion));
        Assert.Equal(Chance.Never, Chance.FromOrThrow(0));
    }

    /// <summary>What a debugger shows: the chance as a human writes one.</summary>
    [Fact]
    public void Reads_as_a_proportion_where_a_debugger_shows_it() {
        // Verify
        Assert.Equal("37%", Chance.FromOrThrow(37).ToString());
    }

}
