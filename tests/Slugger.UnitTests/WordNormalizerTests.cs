using Slugger.Domain.Normalization;

namespace Slugger.UnitTests;

public sealed class WordNormalizerTests
{
    [Fact]
    public void Strips_the_whitespace_around_a_value()
    {
        // Setup
        string word = Dummies.AnyWord();

        // Exercise
        string canonical = WordNormalizer.Canonicalize($"   {word}  ");

        // Verify
        Assert.Equal(word, canonical);
    }

    [Fact]
    public void Collapses_a_run_of_spaces_into_a_single_one()
    {
        // Setup
        string first = Dummies.AnyWord();
        string second = Dummies.AnyWord();

        // Exercise
        string canonical = WordNormalizer.Canonicalize($"{first}     {second}");

        // Verify
        Assert.Equal($"{first} {second}", canonical);
    }

    [Fact]
    public void Lowercases_a_value()
    {
        // Setup
        string word = Dummies.AnyWord();

        // Exercise
        string canonical = WordNormalizer.Canonicalize(word.ToUpperInvariant());

        // Verify
        Assert.Equal(word, canonical);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reduces_a_blank_value_to_an_empty_string(string raw)
    {
        // Exercise
        string canonical = WordNormalizer.Canonicalize(raw);

        // Verify
        Assert.Equal(string.Empty, canonical);
    }

    /// <summary>
    /// Literal on purpose: the accent is the whole point of the case, so an arbitrary value
    /// would say nothing. Accents are kept as written rather than transliterated.
    /// </summary>
    [Fact]
    public void Preserves_accents_instead_of_transliterating_them()
    {
        // Exercise
        string canonical = WordNormalizer.Canonicalize(" René     Dupont ");

        // Verify
        Assert.Equal("rené dupont", canonical);
    }

    /// <summary>
    /// Literal on purpose: the apostrophe is the whole case. A name written as people write it
    /// has to reach the slug as something a slug may hold, and an apostrophe is not that.
    /// </summary>
    [Fact]
    public void Turns_a_punctuation_mark_into_a_word_boundary()
    {
        // Exercise
        string canonical = WordNormalizer.Canonicalize("Jack O'Neil");

        // Verify
        Assert.Equal("jack o neil", canonical);
    }

    /// <summary>
    /// The one that matters most: a value's own hyphen used to survive into the slug while the
    /// separator was something else, so a single slug carried two different joins and the reader
    /// could not tell a segment boundary from one inside a name.
    /// </summary>
    [Fact]
    public void Turns_a_value_s_own_hyphen_into_a_boundary_like_any_other()
    {
        // Exercise
        string canonical = WordNormalizer.Canonicalize("Jean-Luc Picard");

        // Verify
        Assert.Equal("jean luc picard", canonical);
    }

    /// <summary>
    /// Literal on purpose: an ampersand between two spaces is three boundaries in a row, and
    /// three separators in the slug if they are not collapsed together.
    /// </summary>
    [Fact]
    public void Collapses_a_run_of_mixed_spaces_and_punctuation_into_one_boundary()
    {
        // Exercise
        string canonical = WordNormalizer.Canonicalize("Smith & Wesson");

        // Verify
        Assert.Equal("smith wesson", canonical);
    }

    /// <summary>
    /// A boundary at either end would otherwise become a leading or trailing separator, which is
    /// the one place a slug cannot carry one.
    /// </summary>
    [Fact]
    public void Trims_a_punctuation_mark_at_either_end()
    {
        // Setup
        string word = Dummies.AnyWord();

        // Exercise
        string canonical = WordNormalizer.Canonicalize($"!{word}!");

        // Verify
        Assert.Equal(word, canonical);
    }

    /// <summary>
    /// A digit is not a letter and must not be reduced: a theme may name a noun "Apollo 11", and
    /// the token drawn beside a slug is digits too.
    /// </summary>
    [Fact]
    public void Keeps_a_digit_which_is_no_more_a_boundary_than_a_letter()
    {
        // Exercise
        string canonical = WordNormalizer.Canonicalize("Apollo 11");

        // Verify
        Assert.Equal("apollo 11", canonical);
    }

    /// <summary>
    /// The consequence of reducing everything that is not a letter or a digit: a value written
    /// only of punctuation holds nothing to slug. The serializer refuses it rather than letting
    /// an empty noun through - see JsonThemeSerializerTests.
    /// </summary>
    [Fact]
    public void Reduces_a_value_made_only_of_punctuation_to_an_empty_string()
    {
        // Exercise
        string canonical = WordNormalizer.Canonicalize("!?&.-");

        // Verify
        Assert.Equal(string.Empty, canonical);
    }

    /// <summary>
    /// Step 4 of normalization - spaces becoming the separator - deliberately does
    /// not happen here; it belongs to SlugFormatter, which is the only place that knows which
    /// separator the current draw uses.
    /// </summary>
    [Fact]
    public void Leaves_the_internal_space_alone_because_the_separator_belongs_to_the_formatter()
    {
        // Setup
        string first = Dummies.AnyWord();
        string second = Dummies.AnyWord();

        // Exercise
        string canonical = WordNormalizer.Canonicalize($"{first} {second}");

        // Verify
        Assert.Equal($"{first} {second}", canonical);
    }
}
