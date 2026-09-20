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
