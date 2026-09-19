using Slugger.Infrastructure.Serialization;

namespace Slugger.Core.UnitTests;

public sealed class StringInternPoolTests
{
    [Fact]
    public void Hands_back_one_instance_for_a_word_it_has_already_seen()
    {
        // Setup
        StringInternPool pool = new();
        string word = Dummies.AnyWord();
        // Two instances carrying the same characters: interning the very same reference twice
        // would pass without the pool doing anything.
        string firstOccurrence = new(word.ToCharArray());
        string secondOccurrence = new(word.ToCharArray());

        // Exercise
        string firstInterned = pool.Intern(firstOccurrence);
        string secondInterned = pool.Intern(secondOccurrence);

        // Verify
        Assert.NotSame(firstOccurrence, secondOccurrence);
        Assert.Same(firstInterned, secondInterned);
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void Gives_distinct_words_an_entry_each()
    {
        // Setup
        StringInternPool pool = new();
        string word = Dummies.AnyWord();
        string otherWord = Any.String().DifferentFrom(word).WithLengthBetween(3, 10).Generate();

        // Exercise
        pool.Intern(word);
        pool.Intern(otherWord);

        // Verify
        Assert.Equal(2, pool.Count);
    }

    /// <summary>
    /// The pool is a memory concern only, sitting behind normalization rather than replacing
    /// it: by the time a value reaches it, casing has already been settled.
    /// </summary>
    [Fact]
    public void Treats_two_casings_of_a_word_as_two_entries()
    {
        // Setup
        StringInternPool pool = new();
        string word = Dummies.AnyWord();

        // Exercise
        pool.Intern(word);
        pool.Intern(word.ToUpperInvariant());

        // Verify
        Assert.Equal(2, pool.Count);
    }
}
