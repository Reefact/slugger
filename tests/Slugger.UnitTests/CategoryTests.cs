#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     The label a noun and a section of adjectives share. Not a term - it is never drawn and never
///     reaches a slug - so its rules are its own.
/// </summary>
public sealed class CategoryTests {

    /// <summary>A label of any characters names a category, punctuation and all.</summary>
    [Fact]
    public void Reads_a_label_as_a_category() {
        // Setup
        string label = Dummies.AnyWord();

        // Exercise
        Outcome<Category> outcome = Category.From(label);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     Literal on purpose: a category is not a term, so nothing reduces its punctuation. What
    ///     an adjectives section calls "big-game" is one label, not two.
    /// </summary>
    [Fact]
    public void Keeps_what_a_term_would_have_reduced() {
        // Verify
        Assert.Equal("big-game", Category.FromOrThrow("big-game").ToString());
    }

    /// <summary>
    ///     The rule this type exists to settle: a category matches whatever the case. A noun
    ///     labelled "Common" reaching an adjectives section called "common" is what fails open
    ///     today, silently and with an empty pool.
    /// </summary>
    [Fact]
    public void Matches_whatever_case_wrote_it() {
        // Verify
        Assert.Equal(Category.FromOrThrow("common"), Category.FromOrThrow("Common"));
    }

    /// <summary>Surrounding whitespace names nothing, so it is trimmed rather than kept.</summary>
    [Fact]
    public void Trims_what_surrounds_it() {
        // Setup
        string label = Dummies.AnyWord();

        // Verify
        Assert.Equal(Category.FromOrThrow(label), Category.FromOrThrow($"  {label}\t"));
    }

    /// <summary>And a value that spells nothing labels nothing, whitespace included.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_value_that_names_nothing(string value) {
        // Exercise
        Outcome<Category> outcome = Category.From(value);

        // Verify
        Assert.Equal(CategoryError.Codes.Empty, outcome.Error!.Code);
    }

    /// <summary>The other door, carrying the same reason and the value as written.</summary>
    [Fact]
    public void Throws_only_where_the_caller_asked_for_that() {
        // Exercise
        CategoryException thrown = Assert.Throws<CategoryException>(() => Category.FromOrThrow("  "));

        // Verify
        Assert.Equal(CategoryError.Codes.Empty, thrown.Error.Code);
        Assert.True(thrown.Error.Context.TryGet(CategoryError.Value, out string? refused));
        Assert.Equal("  ", refused);
    }

}
