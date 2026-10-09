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

}
