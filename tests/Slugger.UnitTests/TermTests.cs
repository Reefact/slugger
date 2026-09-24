#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Normalization;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What <c>docs/ubiquitous-language.md</c> says a term is: what a theme draws - a noun, an
///     adjective or a participle - made of one or more words. Written from that page and from
///     DEC0008, which decides where one word ends and the next begins.
/// </summary>
public sealed class TermTests {

    /// <summary>The ordinary case: one word spells a term of one.</summary>
    [Fact]
    public void Reads_a_single_word_as_a_term_of_one() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Exercise
        Term term = Term.FromOrThrow(spelling);

        // Verify
        Assert.Equal(1, term.WordCount);
    }

    /// <summary>
    ///     Literal on purpose: "rock crystal" is the compound the vocabulary page uses to say a term
    ///     may count several words. One term, two words - which is the whole distinction the page
    ///     was written to fix.
    /// </summary>
    [Fact]
    public void Reads_a_compound_value_as_one_term_of_several_words() {
        // Exercise
        Term term = Term.FromOrThrow("rock crystal");

        // Verify
        Assert.Equal(2, term.WordCount);
    }

    /// <summary>
    ///     Literal on purpose: the apostrophe is a boundary like any other (DEC0008), so this is a
    ///     term of three words. Where a word refuses the boundary, a term reduces it - how many
    ///     words it holds is what a term is there to say.
    /// </summary>
    [Fact]
    public void Reduces_a_boundary_that_a_word_would_have_refused() {
        // Exercise
        Term term = Term.FromOrThrow("jack o'neil");

        // Verify
        Assert.Equal(3, term.WordCount);
    }

    /// <summary>
    ///     Literal on purpose: space, ampersand, space is one boundary rather than three, and the
    ///     ampersand is no word of its own - a word is letters and digits, and it is neither. Two
    ///     words, where a boundary per character would have left a hole between them.
    /// </summary>
    [Fact]
    public void Reads_a_run_of_boundaries_as_one_and_keeps_no_word_from_it() {
        // Exercise
        Term term = Term.FromOrThrow("smith & wesson");

        // Verify
        Assert.Equal(2, term.WordCount);
    }

    /// <summary>
    ///     Literal on purpose: a boundary at either end belongs to nothing, so it leaves no empty
    ///     word behind it.
    /// </summary>
    [Fact]
    public void Drops_a_boundary_that_starts_or_ends_the_value() {
        // Exercise
        Term term = Term.FromOrThrow("!yahoo!");

        // Verify
        Assert.Equal(1, term.WordCount);
    }

    /// <summary>
    ///     Literal on purpose: the accent is the point. An accented letter is a letter, so it never
    ///     splits a word, and this is two words rather than four.
    /// </summary>
    [Fact]
    public void Keeps_accented_letters_inside_their_words() {
        // Exercise
        Term term = Term.FromOrThrow("rené dupont");

        // Verify
        Assert.Equal(2, term.WordCount);
    }

    /// <summary>
    ///     Literal on purpose: normalization empties this one, and a value that spells no word
    ///     spells no term either.
    /// </summary>
    [Fact]
    public void Refuses_a_value_that_spells_no_word_at_all() {
        // Exercise
        Outcome<Term> outcome = Term.From("!!!");

        // Verify
        Assert.Equal(TermErrors.Codes.Empty, outcome.Error!.Code);
    }

    /// <summary>The same refusal for an empty value, which is the case that gets there first.</summary>
    [Fact]
    public void Refuses_an_empty_value() {
        // Exercise
        Outcome<Term> outcome = Term.From(string.Empty);

        // Verify
        Assert.Equal(TermErrors.Codes.Empty, outcome.Error!.Code);
    }

    /// <summary>
    ///     Literal on purpose: DEC0008 makes a boundary of what is neither a letter nor a digit,
    ///     so a digit joins a word like a letter does. Two words here, the second spelled with
    ///     nothing else.
    /// </summary>
    [Fact]
    public void Spells_a_word_with_digits_as_readily_as_with_letters() {
        // Exercise
        Term term = Term.FromOrThrow("route 66");

        // Verify
        Assert.Equal(2, term.WordCount);
    }

    /// <summary>
    ///     Literal on purpose: normalization empties this value, so the refusal cannot show what
    ///     was refused unless it carries what the file still holds. Whoever reads the report is
    ///     looking for the line, and "" would not find it.
    /// </summary>
    [Fact]
    public void Carries_the_value_as_written_so_a_report_can_point_at_the_line() {
        // Exercise
        Outcome<Term> outcome = Term.From("!!!");

        // Verify
        Assert.True(outcome.Error!.Context.TryGet(TermErrors.Value, out string? refused));
        Assert.Equal("!!!", refused);
    }

    /// <summary>A term is its words, so two values spelling the same ones are the same term.</summary>
    [Fact]
    public void Two_values_spelling_the_same_words_are_the_same_term() {
        // Setup
        string first  = Dummies.AnyWord();
        string second = Dummies.AnyWord();

        // Verify
        Assert.Equal(Term.FromOrThrow($"{first} {second}"), Term.FromOrThrow($"{first} {second}"));
    }

    /// <summary>
    ///     And order counts, which an equality comparing a bag of words would let through: the
    ///     noun "crystal rock" is not the noun "rock crystal".
    /// </summary>
    [Fact]
    public void The_same_words_in_another_order_are_another_term() {
        // Setup
        string first  = Dummies.AnyWord();
        string second = Dummies.AnyWord();

        // Verify
        Assert.NotEqual(Term.FromOrThrow($"{first} {second}"), Term.FromOrThrow($"{second} {first}"));
    }

    /// <summary>
    ///     A term is made of words, and a word settles its case - so the case a theme wrote never
    ///     reaches the term either.
    /// </summary>
    [Fact]
    public void Carries_no_case_of_its_own_because_its_words_carry_none() {
        // Setup
        string spelling = $"{Dummies.AnyWord()} {Dummies.AnyWord()}";

        // Verify
        Assert.Equal(Term.FromOrThrow(spelling), Term.FromOrThrow(spelling.ToUpperInvariant()));
    }

    /// <summary>
    ///     Literal on purpose: what a debugger shows is the term as a theme writes it, a space
    ///     between its words - and a space rather than a separator, which no term knows.
    /// </summary>
    [Fact]
    public void Reads_as_its_words_spaced_where_a_debugger_shows_it() {
        // Verify
        Assert.Equal("rock crystal", Term.FromOrThrow("rock crystal").ToString());
    }

    /// <summary>The reporting door, which a theme carrying hundreds of entries is read through.</summary>
    [Fact]
    public void Reports_a_refusal_rather_than_throwing_it() {
        // Exercise
        Outcome<Term> outcome = Term.From("   ");

        // Verify
        Assert.False(outcome.IsSuccess);
    }

    /// <summary>
    ///     Term reduces a value to its words itself rather than through WordNormalizer, so DEC0008
    ///     is written in two places until the load path stops going through the older one. This is
    ///     what makes the two drift into a red build rather than into two behaviours: drawn values
    ///     carrying boundaries, runs of them, and boundaries at either end.
    /// </summary>
    [Theory]
    [InlineData("rock crystal")]
    [InlineData("jack o'neil")]
    [InlineData("smith & wesson")]
    [InlineData("!yahoo!")]
    [InlineData("  René   Dupont  ")]
    [InlineData("---")]
    [InlineData("")]
    public void Reduces_a_value_to_the_same_words_the_shared_normalizer_does(string raw) {
        // Setup - the older path spells a term as one string; the empty one is its way of saying
        // no word survived, which Term says by refusing instead.
        string canonical = WordNormalizer.Canonicalize(raw);

        // Exercise
        Outcome<Term> outcome = Term.From(raw);

        // Verify
        Assert.Equal(canonical, outcome.IsSuccess ? outcome.GetResultOrThrow().ToString() : string.Empty);
    }

    /// <summary>The other door, carrying the same reason.</summary>
    [Fact]
    public void Throws_only_where_the_caller_asked_for_that() {
        // Exercise
        DomainException thrown = Assert.Throws<DomainException>(() => Term.FromOrThrow("---"));

        // Verify
        Assert.Equal(TermErrors.Codes.Empty, thrown.Error.Code);
    }

}
