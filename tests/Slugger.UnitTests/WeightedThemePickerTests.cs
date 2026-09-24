#region Usings declarations

using Slugger.Domain;
using Slugger.Domain.Resolution;

#endregion

namespace Slugger.UnitTests;

public sealed class WeightedThemePickerTests {

    #region Static members

    private static ThemeDocument ThemeOf(string name, int nouns) {
        return new ThemeDocument(name,
                         new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"] },
                         new Dictionary<string, IReadOnlyList<string>>(),
                         [.. Enumerable.Range(0, nouns).Select(index => new NounEntry($"noun{index}", []))]);
    }

    #endregion

    [Fact]
    public void A_single_theme_is_always_the_one_picked() {
        // Setup
        ThemeDocument               only   = ThemeOf("only", Any.Int32().Between(1, 50).Generate());
        WeightedThemePicker picker = new([only]);

        // Exercise
        ThemeDocument picked = picker.Pick(new ScriptedRandomSource());

        // Verify - no draw was even needed, so the script stayed untouched.
        Assert.Same(only, picked);
    }

    /// <summary>
    ///     The whole point of the cumulative array: a draw falls in the interval of the theme it
    ///     belongs to, and a draw landing exactly on a boundary belongs to the theme that starts
    ///     there - an off-by-one here would quietly skew every multi-theme run.
    /// </summary>
    [Theory]
    [InlineData(0, "small")]
    [InlineData(1, "small")]
    [InlineData(2, "large")]
    [InlineData(3, "large")]
    [InlineData(4, "large")]
    public void A_draw_lands_in_the_interval_of_its_theme(int draw, string expected) {
        // Setup - two nouns then three, so the cumulative counts are [2, 5].
        WeightedThemePicker picker = new([ThemeOf("small", 2), ThemeOf("large", 3)]);

        // Exercise
        ThemeDocument picked = picker.Pick(new ScriptedRandomSource(draw));

        // Verify
        Assert.Equal(expected, picked.Name);
    }

    [Fact]
    public void The_span_drawn_from_is_the_nouns_the_themes_hold_between_them() {
        // Setup
        WeightedThemePicker picker = new([ThemeOf("small", 2), ThemeOf("large", 3)]);

        // Verify
        Assert.Equal(5, picker.TotalNouns);
    }

    /// <summary>
    ///     Equivalent to drawing uniformly from the concatenated noun lists, which is what the
    ///     cumulative array exists to reproduce without paying O(N) for it.
    /// </summary>
    [Fact]
    public void A_theme_is_picked_in_proportion_to_its_share_of_the_nouns() {
        // Setup - one noun against nine, so roughly one draw in ten should land on the small one.
        WeightedThemePicker picker = new([ThemeOf("small", 1), ThemeOf("large", 9)]);
        DefaultRandomSource random = new(20260919);

        // Exercise
        int small = Enumerable.Range(0, 10_000).Count(_ => picker.Pick(random).Name == "small");

        // Verify - a wide band: this pins the proportion, not the generator's exact sequence.
        Assert.InRange(small, 800, 1200);
    }

    [Fact]
    public void Refuses_to_be_built_with_no_theme_at_all() {
        // Exercise
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new WeightedThemePicker([]));

        // Verify
        Assert.Equal("themes", refused.ParamName);
    }

}