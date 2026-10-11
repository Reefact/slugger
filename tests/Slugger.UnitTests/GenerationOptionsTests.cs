#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     The library refuses what the command line refuses, where the value is set rather than when a
///     slug is generated: a chance of 150 was taken as "always", "../" was written between the words of
///     a noun, and a limit of zero waited for the first draw to throw under another name.
/// </summary>
public sealed class GenerationOptionsTests {

    [Fact]
    public void Refuses_a_token_chance_above_a_hundred() {
        // Setup
        int chance = Any.Int32().Between(101, int.MaxValue).Generate();

        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions { TokenChance = chance });

        // Verify
        Assert.Equal(nameof(GenerationOptions.TokenChance), refused.ParamName);
    }

    [Fact]
    public void Refuses_a_negative_token_chance() {
        // Setup
        int chance = Any.Int32().Between(int.MinValue, -1).Generate();

        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions { TokenChance = chance });

        // Verify
        Assert.Equal(nameof(GenerationOptions.TokenChance), refused.ParamName);
    }

    [Fact]
    public void Refuses_a_negative_token_length() {
        // Setup
        int length = Any.Int32().Between(int.MinValue, -1).Generate();

        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions { TokenLength = length });

        // Verify
        Assert.Equal(nameof(GenerationOptions.TokenLength), refused.ParamName);
    }

    /// <summary>Literal on purpose: "../" is the value the issue saw written into a slug.</summary>
    [Fact]
    public void Refuses_a_word_separator_of_more_than_one_character() {
        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions { WordSeparator = "../" });

        // Verify
        Assert.Equal(nameof(GenerationOptions.WordSeparator), refused.ParamName);
    }

    /// <summary>The empty string is the useful value: it glues a compound term's words together.</summary>
    [Fact]
    public void Accepts_an_empty_word_separator() {
        // Exercise
        GenerationOptions options = new() { WordSeparator = string.Empty };

        // Verify
        Assert.Equal(string.Empty, options.WordSeparator);
    }

    [Fact]
    public void Refuses_a_length_limit_below_one_where_it_is_set() {
        // Setup
        int limit = Any.Int32().Between(int.MinValue, 0).Generate();

        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions { MaxLength = limit });

        // Verify
        Assert.Equal(nameof(GenerationOptions.MaxLength), refused.ParamName);
    }

    [Fact]
    public void Refuses_a_word_cap_below_one_where_it_is_set() {
        // Setup
        int cap = Any.Int32().Between(int.MinValue, 0).Generate();

        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions { MaxSegmentWords = cap });

        // Verify
        Assert.Equal(nameof(GenerationOptions.MaxSegmentWords), refused.ParamName);
    }

    [Fact]
    public void Refuses_a_casing_the_enumeration_does_not_declare() {
        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions { Casing = (Casing)42 });

        // Verify
        Assert.Equal(nameof(GenerationOptions.Casing), refused.ParamName);
    }

    [Fact]
    public void Refuses_a_segment_mode_the_enumeration_does_not_declare() {
        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions { SegmentMode = (SegmentMode)42 });

        // Verify
        Assert.Equal(nameof(GenerationOptions.SegmentMode), refused.ParamName);
    }

    /// <summary>A copy with "with" goes through the same accessors, so it cannot slip a value past them.</summary>
    [Fact]
    public void Refuses_an_out_of_range_value_set_through_a_copy() {
        // Exercise
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => GenerationOptions.Default with { TokenChance = 101 });

        // Verify
        Assert.Equal(nameof(GenerationOptions.TokenChance), refused.ParamName);
    }

    /// <summary>The bounds themselves are values, and the defaults keep working.</summary>
    [Fact]
    public void Accepts_the_bounds_of_every_range() {
        // Exercise
        GenerationOptions options = new() { TokenChance = 0, TokenLength = 0, MaxLength = 1, MaxSegmentWords = 1 };

        // Verify
        Assert.Equal((0, 0, 1, 1), (options.TokenChance, options.TokenLength, options.MaxLength, options.MaxSegmentWords));
        Assert.Equal(100, GenerationOptions.Default.TokenChance);
    }

}
