#region Usings declarations

using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     The sentences a refusal is written in, read whole: an author reads them rather than the codes,
///     so a count of one that comes out plural is a bug the code never sees.
/// </summary>
/// <remarks>
///     The counts here are literals on purpose: one is the case, against a value that is not one -
///     and every one of them stays under a thousand, so no culture's digit grouping enters a sentence.
/// </remarks>
public sealed class ThemeErrorsTests {

    [Fact]
    public void A_category_totalling_one_combination_says_it_in_the_singular() {
        // Exercise
        string message = ThemeErrors.CategoryTooPoor("hot", 1, 400).DiagnosticMessage;

        // Verify
        Assert.Equal("Category \"hot\" totals 1 combination, but every category needs at least 400.", message);
    }

    [Fact]
    public void A_category_totalling_several_combinations_says_it_in_the_plural() {
        // Exercise
        string message = ThemeErrors.CategoryTooPoor("hot", 12, 400).DiagnosticMessage;

        // Verify
        Assert.Equal("Category \"hot\" totals 12 combinations, but every category needs at least 400.", message);
    }

    [Fact]
    public void A_promise_of_one_character_says_it_in_the_singular() {
        // Exercise
        string message = ThemeErrors.LongerThanPromised("twoWords", "keen-moon", 1).DiagnosticMessage;

        // Verify
        Assert.Equal("maxLength.twoWords promises 1 character, but the theme can produce \"keen-moon\" at 9.", message);
    }

    [Fact]
    public void A_limit_of_one_character_nothing_fits_in_says_it_in_the_singular() {
        // Exercise
        string message = ThemeErrors.NothingFitsTheLimit("docker", 1).DiagnosticMessage;

        // Verify
        Assert.Equal("No slug of theme \"docker\" fits in 1 character.", message);
    }

    [Fact]
    public void A_limit_of_several_characters_nothing_fits_in_says_it_in_the_plural() {
        // Exercise
        string message = ThemeErrors.NothingFitsTheLimit("docker", 5).DiagnosticMessage;

        // Verify
        Assert.Equal("No slug of theme \"docker\" fits in 5 characters.", message);
    }

    [Fact]
    public void A_limit_of_one_character_starving_a_noun_says_it_in_the_singular() {
        // Exercise
        string message = ThemeErrors.TheLimitStarvesTheNoun("moon", "keen", 1, 20, 1).DiagnosticMessage;

        // Verify
        Assert.StartsWith("Under 1 character, \"moon\" reaches 1 participle ", message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The remedy comes after the reason it answers, so the sentence reads as advice rather than
    ///     as "not to use it" hanging at its end.
    /// </summary>
    [Fact]
    public void A_built_in_theme_that_cannot_be_unregistered_says_how_to_stop_using_it() {
        // Exercise
        string message = ThemeErrors.NotAFile("docker").DiagnosticMessage;

        // Verify
        Assert.Equal(
            "\"docker\" is embedded in the binary, so there is nothing to unregister - to stop using it, leave it out of --theme.",
            message);
    }

    /// <summary>
    ///     The key is quoted, as every other message quotes one: written bare, "incompatible names"
    ///     reads as an adjective followed by a noun.
    /// </summary>
    [Fact]
    public void An_undeclared_adjective_in_incompatible_names_the_key_in_quotes() {
        // Exercise
        string message = ThemeErrors.IncompatibleAdjectiveNotDeclared("waning", false).DiagnosticMessage;

        // Verify
        Assert.Equal("\"incompatible\" names \"waning\" as an adjective, which the theme declares nowhere in \"adjectives\".", message);
    }

    /// <summary>
    ///     A participle follows its adjective, and both refusals that measure what is left of it say
    ///     so with the same word - "beside" and "behind" said one relation two ways.
    /// </summary>
    [Fact]
    public void An_incompatibility_starving_a_noun_counts_the_participles_after_the_adjective() {
        // Exercise
        string message = ThemeErrors.IncompatibilityStarvesTheNoun("moon", "keen", 15, 20).DiagnosticMessage;

        // Verify
        Assert.Equal(
            "\"moon\" reaches 15 participles after \"keen\", but a theme drawing \"both\" needs at least 20 per noun for every "
          + "adjective it can draw - either declare more participles for it, or drop the incompatibility.",
            message);
    }

    [Fact]
    public void A_limit_starving_a_noun_counts_the_participles_after_the_adjective() {
        // Exercise
        string message = ThemeErrors.TheLimitStarvesTheNoun("moon", "keen", 5, 20, 32).DiagnosticMessage;

        // Verify
        Assert.Equal(
            "Under 32 characters, \"moon\" reaches 5 participles after \"keen\", but a theme drawing \"both\" needs at least 20 per "
          + "noun for every adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.",
            message);
    }

}
