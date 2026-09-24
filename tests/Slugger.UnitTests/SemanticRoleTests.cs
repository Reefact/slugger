#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What the three roles of <c>docs/ubiquitous-language.md</c> are for: telling a noun from an
///     adjective from a participle where three terms would have passed for one another. The same
///     term can be declared in both sections (DEC0013), so the term is never what tells them apart.
/// </summary>
public sealed class SemanticRoleTests {

    #region Static members

    private static Term AnyTerm() {
        return Term.FromOrThrow(Dummies.AnyWord());
    }

    #endregion

    /// <summary>Each role hands its term back, which is what a formatter and a budget both want.</summary>
    [Fact]
    public void A_role_gives_up_the_term_it_reads() {
        // Setup
        Term term = AnyTerm();

        // Verify
        Assert.Equal(term, Adjective.Of(term).Value);
        Assert.Equal(term, Participle.Of(term).Value);
        Assert.Equal(term, Noun.Of(term).Value);
    }

    /// <summary>
    ///     The two halves of DEC0013 at once: the same word declared in both sections is one term,
    ///     and the adjective it makes is not the participle it makes.
    /// </summary>
    [Fact]
    public void The_same_term_read_in_two_roles_stays_one_term_and_makes_two_things() {
        // Setup
        Term term = AnyTerm();

        // Exercise
        Adjective  adjective  = Adjective.Of(term);
        Participle participle = Participle.Of(term);

        // Verify
        Assert.Equal(adjective.Value, participle.Value);
        Assert.False(adjective.Equals(participle));
    }

    /// <summary>Within one role the term decides, both ways round.</summary>
    [Fact]
    public void Two_roles_of_the_same_kind_agree_when_their_terms_do() {
        // Setup
        Term term  = AnyTerm();
        Term other = Term.FromOrThrow($"{term}s");

        // Verify
        Assert.Equal(Adjective.Of(term), Adjective.Of(term));
        Assert.NotEqual(Adjective.Of(term), Adjective.Of(other));
    }

    /// <summary>What a debugger shows is the term, since that is all there is to see.</summary>
    [Fact]
    public void A_role_reads_as_its_term_where_a_debugger_shows_it() {
        // Verify
        Assert.Equal("rock crystal", Noun.Of(Term.FromOrThrow("rock crystal")).ToString());
    }

}
