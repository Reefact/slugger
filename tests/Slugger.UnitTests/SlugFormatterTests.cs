using Slugger.Domain;
using Slugger.Domain.Generation;

namespace Slugger.UnitTests;

public sealed class SlugFormatterTests
{
    [Theory]
    [InlineData(Casing.Kebab, '-', "gorgeous-khorana")]
    [InlineData(Casing.Snake, '_', "gorgeous_khorana")]
    public void Joins_the_segments_with_the_separator(Casing casing, char separator, string expected)
    {
        // Setup
        GenerationOptions options = new() { Casing = casing, Separator = separator };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "khorana"], token: null, options);

        // Verify
        Assert.Equal(expected, slug);
    }

    /// <summary>
    /// Step 4 of the spec's normalization, applied here rather than at load time because the
    /// separator is only known now - see WordNormalizer for why that matters.
    /// </summary>
    [Fact]
    public void Replaces_the_internal_spaces_of_a_compound_value_with_the_separator()
    {
        // Setup
        GenerationOptions options = new() { Separator = '-' };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], token: null, options);

        // Verify
        Assert.Equal("gorgeous-john-doe", slug);
    }

    [Fact]
    public void Camel_drops_the_separator_and_capitalises_every_word_but_the_first()
    {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "wandering", "john doe"], token: null, options);

        // Verify - the compound value's own words are capitalised too, having no separator left.
        Assert.Equal("gorgeousWanderingJohnDoe", slug);
    }

    [Fact]
    public void Appends_a_token_behind_the_separator()
    {
        // Setup
        GenerationOptions options = new() { Separator = '-', TokenGlued = false };

        // Exercise
        string slug = SlugFormatter.Format(["wispy", "dust"], "1337", options);

        // Verify
        Assert.Equal("wispy-dust-1337", slug);
    }

    /// <summary>Docker's collision digit rides on the noun with nothing between them.</summary>
    [Fact]
    public void Glues_a_token_straight_onto_the_last_segment()
    {
        // Setup
        GenerationOptions options = new() { Separator = '_', Casing = Casing.Snake, TokenGlued = true };

        // Exercise
        string slug = SlugFormatter.Format(["focused", "turing"], "3", options);

        // Verify
        Assert.Equal("focused_turing3", slug);
    }

    [Fact]
    public void Camel_appends_the_token_directly_having_no_separator_to_glue_it_with()
    {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel, TokenGlued = false };

        // Exercise
        string slug = SlugFormatter.Format(["wispy", "dust"], "1337", options);

        // Verify
        Assert.Equal("wispyDust1337", slug);
    }

    [Fact]
    public void Draws_no_token_when_the_length_is_zero()
    {
        // Setup
        GenerationOptions options = new() { TokenLength = 0 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource());

        // Verify
        Assert.Null(token);
    }

    [Fact]
    public void Draws_no_token_when_the_chance_is_zero()
    {
        // Setup
        GenerationOptions options = new() { TokenLength = Any.Int32().Between(1, 8).Generate(), TokenChance = 0 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource());

        // Verify
        Assert.Null(token);
    }

    [Fact]
    public void Draws_a_token_of_the_asked_length_in_decimal()
    {
        // Setup
        GenerationOptions options = new() { TokenLength = 4, TokenHex = false };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(1, 3, 3, 7));

        // Verify
        Assert.Equal("1337", token);
    }

    [Fact]
    public void Draws_a_hexadecimal_token_from_the_wider_alphabet()
    {
        // Setup
        GenerationOptions options = new() { TokenLength = 3, TokenHex = true };

        // Exercise - 10, 11, 15 index into "0123456789abcdef".
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(10, 11, 15));

        // Verify
        Assert.Equal("abf", token);
    }

    /// <summary>
    /// Next(100) lands in 0..99, so a chance of 1 draws only on a roll of 0. Docker leans on
    /// that to make its collision digit as rare as a real collision would be.
    /// </summary>
    [Theory]
    [InlineData(0, "7")]
    [InlineData(1, null)]
    [InlineData(99, null)]
    public void Weighs_the_token_against_the_chance_before_drawing_it(int roll, string? expected)
    {
        // Setup
        GenerationOptions options = new() { TokenLength = 1, TokenChance = 1 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(roll, 7));

        // Verify
        Assert.Equal(expected, token);
    }
}
