#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What qualifies the noun, as <c>docs/ubiquitous-language.md</c> names it: an adjective, a
///     participle, or both. One or two terms, never none.
/// </summary>
public sealed class EpithetTests {

    #region Static members

    private static Adjective AnyAdjective() {
        return Adjective.Of(Term.FromOrThrow(Dummies.AnyWord()));
    }

    private static Participle AnyParticiple() {
        return Participle.Of(Term.FromOrThrow(Dummies.AnyWord()));
    }

    #endregion

    /// <summary>One in front of the noun, whichever of the two it is.</summary>
    [Fact]
    public void One_role_alone_puts_one_term_in_front_of_the_noun() {
        // Verify
        Assert.Equal(1, new Epithet(AnyAdjective()).TermCount);
        Assert.Equal(1, new Epithet(AnyParticiple()).TermCount);
    }

    /// <summary>And both together put two, which is the three segment form.</summary>
    [Fact]
    public void Both_roles_put_two_terms_in_front_of_the_noun() {
        // Verify
        Assert.Equal(2, new Epithet(AnyAdjective(), AnyParticiple()).TermCount);
    }

    /// <summary>
    ///     The roles are what tell two epithets apart, not the terms: the same word read as an
    ///     adjective and as a participle makes two different epithets (DEC0013).
    /// </summary>
    [Fact]
    public void The_same_term_in_two_roles_makes_two_epithets() {
        // Setup
        Term term = Term.FromOrThrow(Dummies.AnyWord());

        // Verify
        Assert.NotEqual(new Epithet(Adjective.Of(term)), new Epithet(Participle.Of(term)));
    }

    /// <summary>Two epithets of the same roles are the same epithet.</summary>
    [Fact]
    public void Two_epithets_of_the_same_roles_are_the_same() {
        // Setup
        Adjective  adjective  = AnyAdjective();
        Participle participle = AnyParticiple();

        // Verify
        Assert.Equal(new Epithet(adjective, participle), new Epithet(adjective, participle));
        Assert.NotEqual(new Epithet(adjective, participle), new Epithet(adjective));
    }

    /// <summary>
    ///     Literal on purpose: what a debugger shows is the terms in the order they reach the slug,
    ///     the adjective first as DEC0017 draws them.
    /// </summary>
    [Fact]
    public void Reads_as_its_terms_in_order_where_a_debugger_shows_it() {
        // Setup
        Adjective  dazzling = Adjective.Of(Term.FromOrThrow("dazzling"));
        Participle flaring  = Participle.Of(Term.FromOrThrow("flaring"));

        // Verify
        Assert.Equal("dazzling flaring", new Epithet(dazzling, flaring).ToString());
        Assert.Equal("flaring", new Epithet(flaring).ToString());
    }

}
