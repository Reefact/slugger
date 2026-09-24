#region Usings declarations

using Slugger.Domain;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What makes a theme an entity rather than a bag of words: it is the name it is known by, and
///     a theme edited between two reads is still that theme.
/// </summary>
public sealed class ThemeTests {

    #region Static members

    private static Noun AnyNoun() {
        return Noun.Of(Term.FromOrThrow(Dummies.AnyWord()));
    }

    private static Theme AnyThemeNamed(string name, params Noun[] nouns) {
        return new Theme(ThemeName.FromOrThrow(name), nouns.Length > 0 ? nouns : [AnyNoun()]);
    }

    #endregion

    [Fact]
    public void Counts_the_nouns_there_are_to_draw_from() {
        // Setup
        Noun[] nouns = [AnyNoun(), AnyNoun(), AnyNoun()];

        // Exercise
        Theme theme = AnyThemeNamed("flowers", nouns);

        // Verify
        Assert.Equal(3, theme.NounCount);
    }

    [Fact]
    public void Hands_back_the_noun_a_draw_picked() {
        // Setup
        Noun picked = AnyNoun();
        Theme theme = AnyThemeNamed("flowers", AnyNoun(), picked);

        // Verify
        Assert.Equal(picked, theme.GetNoun(1));
    }

    /// <summary>
    ///     The identity is the name, so the same theme read before and after an edit is the same
    ///     theme. Written with different nouns on purpose: equality that looked at them would be
    ///     the value object's answer, not the entity's.
    /// </summary>
    [Fact]
    public void Is_the_same_theme_as_one_of_its_name_holding_something_else() {
        // Setup
        Theme before = AnyThemeNamed("flowers", AnyNoun());
        Theme after  = AnyThemeNamed("flowers", AnyNoun(), AnyNoun());

        // Verify
        Assert.Equal(before, after);
    }

    [Fact]
    public void Is_a_different_theme_from_one_of_another_name_holding_the_same_nouns() {
        // Setup
        Noun shared = AnyNoun();

        // Verify
        Assert.NotEqual(AnyThemeNamed("flowers", shared), AnyThemeNamed("mineralogy", shared));
    }

    /// <summary>
    ///     A theme with no noun produces nothing, so one never reaches the domain: the read refuses
    ///     it first. Technical rather than first-class on purpose - by the time this fires, the
    ///     refusal a reader should have been shown has already been skipped.
    /// </summary>
    [Fact]
    public void Refuses_to_exist_with_no_noun_to_draw_from() {
        // Verify
        Assert.Throws<ArgumentException>(() => new Theme(ThemeName.FromOrThrow("flowers"), []));
    }

    [Fact]
    public void Refuses_a_position_that_names_no_noun_of_its_own() {
        // Setup
        Theme theme = AnyThemeNamed("flowers", AnyNoun());

        // Verify
        Assert.Throws<ArgumentOutOfRangeException>(() => theme.GetNoun(1));
    }

}
