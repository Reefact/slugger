#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What <c>docs/ubiquitous-language.md</c> says a word is: the smallest unit the vocabulary
///     names, one or more of them making a term. These are written from that page and from the
///     decisions it rests on - DEC0008 for what a boundary is, DEC0006 for how a refusal travels -
///     rather than from the type, so that a rule nobody implemented comes out red.
/// </summary>
public sealed class WordTests {

    /// <summary>A word is the letters it is spelled with, and reading one back is the whole job.</summary>
    [Fact]
    public void Reads_a_value_of_letters_as_one_word() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Exercise
        Outcome<Word> outcome = Word.From(spelling);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     Literal on purpose: the accent is the whole point of the case. DEC0008 makes a boundary of
    ///     everything that is neither a letter nor a digit, and an accented letter is a letter - so
    ///     the value is one word, and four characters long rather than five decomposed ones.
    /// </summary>
    [Fact]
    public void Keeps_an_accented_letter_because_a_letter_is_what_it_is() {
        // Exercise
        Word word = Word.FromOrThrow("rené");

        // Verify
        Assert.Equal(4, word.Length);
    }

    /// <summary>
    ///     Literal on purpose: DEC0008 says a boundary is what is neither a letter nor a digit, which
    ///     says in passing that a digit is not one. A theme naming "route 66" spells its second word
    ///     with nothing else.
    /// </summary>
    [Fact]
    public void Reads_digits_as_a_word_because_DEC0008_counts_them_as_it_counts_letters() {
        // Exercise
        Outcome<Word> outcome = Word.From("66");

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     A word has one spelling, so the case is settled rather than carried. Asserted through
    ///     equality because the spelling never leaves the type - rendering is not its business.
    /// </summary>
    [Fact]
    public void Lowers_the_case_because_a_word_has_only_one_spelling() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Verify
        Assert.Equal(Word.FromOrThrow(spelling), Word.FromOrThrow(spelling.ToUpperInvariant()));
    }

    /// <summary>
    ///     The same rule applied to what surrounds the word: trimming cannot change how many words a
    ///     value spells, so it is normalized rather than refused.
    /// </summary>
    [Fact]
    public void Trims_what_surrounds_it_because_that_cannot_change_how_many_words_it_spells() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Verify
        Assert.Equal(Word.FromOrThrow(spelling), Word.FromOrThrow($"  {spelling}\t"));
    }

    /// <summary>
    ///     Literal on purpose: "rock crystal" is the compound the vocabulary page uses to say a term
    ///     may count several words. Refused rather than reduced - handing back "rock" would drop
    ///     "crystal" without saying so.
    /// </summary>
    [Fact]
    public void Refuses_a_compound_value_rather_than_keeping_one_of_its_words() {
        // Exercise
        Outcome<Word> outcome = Word.From("rock crystal");

        // Verify
        Assert.Equal(WordErrors.Codes.NotOneWord, outcome.Error!.Code);
    }

    /// <summary>
    ///     Literal on purpose: the apostrophe is the case DEC0008 was written for. It is a boundary
    ///     like any other, so the value spells three words and not one.
    /// </summary>
    [Fact]
    public void Refuses_a_boundary_that_is_not_a_space_just_the_same() {
        // Exercise
        Outcome<Word> outcome = Word.From("jack o'neil");

        // Verify
        Assert.Equal(WordErrors.Codes.NotOneWord, outcome.Error!.Code);
    }

    /// <summary>Nothing written spells no word, which is its own refusal and not a boundary one.</summary>
    [Fact]
    public void Reads_an_empty_value_as_no_word() {
        // Exercise
        Outcome<Word> outcome = Word.From(string.Empty);

        // Verify
        Assert.Equal(WordErrors.Codes.Empty, outcome.Error!.Code);
    }

    /// <summary>
    ///     Whitespace alone spells no word either. It reads as several only if the emptiness is
    ///     tested before the trim, which is the wrong order and the wrong answer: there is no word
    ///     here, not too many.
    /// </summary>
    [Fact]
    public void Reads_whitespace_alone_as_no_word_rather_than_as_several() {
        // Exercise
        Outcome<Word> outcome = Word.From("        ");

        // Verify
        Assert.Equal(WordErrors.Codes.Empty, outcome.Error!.Code);
    }

    /// <summary>
    ///     A refusal is read by whoever wrote the theme, so the character at fault has to be legible.
    ///     A tab quoted between apostrophes shows nothing at all.
    /// </summary>
    [Fact]
    public void Names_a_boundary_nobody_can_see_by_its_code_point() {
        // Exercise
        Outcome<Word> outcome = Word.From("rock\tcrystal");

        // Verify
        Assert.Contains("U+0009", outcome.Error!.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>The spelling is all a word is, so two values spelled the same are the same word.</summary>
    [Fact]
    public void Two_values_spelled_the_same_are_the_same_word() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Verify
        Assert.Equal(Word.FromOrThrow(spelling), Word.FromOrThrow(spelling));
    }

    /// <summary>And the other half, which an equality that always agreed would pass just as well.</summary>
    [Fact]
    public void Two_values_spelled_differently_are_different_words() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Verify
        Assert.NotEqual(Word.FromOrThrow(spelling), Word.FromOrThrow($"{spelling}s"));
    }

    /// <summary>
    ///     How long a word is, which is what a length budget counts. Drawn rather than written, so
    ///     that the test leans on no particular spelling.
    /// </summary>
    [Fact]
    public void Is_as_long_as_the_characters_it_carries() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Verify
        Assert.Equal(spelling.Length, Word.FromOrThrow(spelling).Length);
    }

    /// <summary>
    ///     Every value object renders itself for a human to read in a debugger, which for a word is
    ///     its spelling and nothing around it. Pinned so that the default rendering - the type's own
    ///     name, which says nothing in a watch window - cannot come back.
    /// </summary>
    [Fact]
    public void Reads_as_its_own_spelling_where_a_debugger_shows_it() {
        // Setup
        string spelling = Dummies.AnyWord();

        // Verify
        Assert.Equal(spelling, Word.FromOrThrow(spelling).ToString());
    }

    /// <summary>
    ///     DEC0006 wants every reason a theme was refused, not the first. A word that threw could
    ///     only ever give the first, so the reporting door is the one the domain is built on.
    /// </summary>
    [Fact]
    public void Reports_a_refusal_rather_than_throwing_it_so_a_caller_can_collect_the_next_one() {
        // Exercise
        Outcome<Word> outcome = Word.From("rock crystal");

        // Verify
        Assert.False(outcome.IsSuccess);
    }

    /// <summary>
    ///     The other door, for a caller writing a literal, where a refusal is a bug rather than a
    ///     finding. It carries the same reason the reporting one would have given.
    /// </summary>
    [Fact]
    public void Throws_only_where_the_caller_asked_for_that() {
        // Exercise
        DomainException thrown = Assert.Throws<DomainException>(() => Word.FromOrThrow("rock crystal"));

        // Verify
        Assert.Equal(WordErrors.Codes.NotOneWord, thrown.Error.Code);
    }

}
