#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What a draw produces, as <c>docs/ubiquitous-language.md</c> names it: a noun, an optional
///     epithet in front of it, an optional token behind. Written from that page - and a slug is not
///     a string there, so nothing here asserts on one.
/// </summary>
public sealed class SlugTests {

    #region Static members

    private static Noun AnyNoun() {
        return Noun.Of(Term.FromOrThrow(Dummies.AnyWord()));
    }

    private static Epithet AnyEpithet() {
        return new Epithet(Adjective.Of(Term.FromOrThrow(Dummies.AnyWord())));
    }

    private static Token AnyToken() {
        return Token.Draw(
            TokenMould.Of(TokenAlphabet.Decimal, TokenLength.FromOrThrow(2)),
            new ScriptedRandomSource(0, 0));
    }

    #endregion

    /// <summary>
    ///     A noun alone is a slug of one term. It is the case a theme reaching no word to put in
    ///     front of a noun produces, which allowSmall lets through.
    /// </summary>
    [Fact]
    public void A_noun_alone_carries_one_term() {
        // Verify
        Assert.Equal(1, new Slug(AnyNoun()).TermCount);
    }

    /// <summary>One epithet of one term makes the two term form - what docker and heroku produce.</summary>
    [Fact]
    public void A_noun_and_a_single_epithet_carry_two_terms() {
        // Verify
        Assert.Equal(2, new Slug(AnyNoun(), AnyEpithet()).TermCount);
    }

    /// <summary>And an epithet of two makes the three term form, which is slugger's own.</summary>
    [Fact]
    public void A_noun_and_a_double_epithet_carry_three_terms() {
        // Setup
        Epithet both = new(
            Adjective.Of(Term.FromOrThrow(Dummies.AnyWord())),
            Participle.Of(Term.FromOrThrow(Dummies.AnyWord())));

        // Verify
        Assert.Equal(3, new Slug(AnyNoun(), both).TermCount);
    }

    /// <summary>
    ///     The token counts for no term - it is drawn at random and not from the vocabulary - so it
    ///     is asked for separately.
    /// </summary>
    [Fact]
    public void A_token_adds_no_term_and_is_asked_for_on_its_own() {
        // Setup
        Slug withToken = new(AnyNoun(), token: AnyToken());

        // Verify
        Assert.Equal(1, withToken.TermCount);
        Assert.True(withToken.HasToken);
        Assert.False(new Slug(AnyNoun()).HasToken);
    }

    /// <summary>Two slugs of the same parts are the same slug.</summary>
    [Fact]
    public void Two_slugs_of_the_same_parts_are_the_same() {
        // Setup
        Epithet epithet = AnyEpithet();
        Noun    noun    = AnyNoun();

        // Verify
        Assert.Equal(new Slug(noun, epithet), new Slug(noun, epithet));
        Assert.NotEqual(new Slug(noun, epithet), new Slug(noun));
    }

    /// <summary>
    ///     Literal on purpose: what a debugger shows is the parts in the order they reach the
    ///     slug - not the order the constructor takes them. Not the slug a destination receives
    ///     either: that one needs a separator, and a slug carries none.
    /// </summary>
    [Fact]
    public void Reads_as_its_parts_in_order_where_a_debugger_shows_it() {
        // Setup
        Epithet dazzling = new(Adjective.Of(Term.FromOrThrow("dazzling")));
        Noun    olivine  = Noun.Of(Term.FromOrThrow("olivine"));

        // Verify
        Assert.Equal("dazzling olivine", new Slug(olivine, dazzling).ToString());
        Assert.Equal("olivine", new Slug(olivine).ToString());
    }

}
