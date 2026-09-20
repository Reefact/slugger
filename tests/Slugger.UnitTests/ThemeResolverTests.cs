using Slugger.Domain;
using Slugger.Domain.Resolution;

namespace Slugger.UnitTests;

public sealed class ThemeResolverTests
{
    /// <summary>
    /// Pins the reading documented on ThemeResolver, and the reason it exists: read literally,
    /// a pool rule without a shared floor gives a noun with no category nothing, which refuses docker and heroku as
    /// shipped - all 236 of docker's nouns carry no category at all.
    /// </summary>
    [Fact]
    public void A_noun_carrying_no_category_still_reaches_common()
    {
        // Setup
        string adjective = Dummies.AnyWord();
        Theme theme = ThemeWith(
            adjectives: new() { ["common"] = [adjective] },
            noun: new Noun("moon", []));

        // Exercise
        IReadOnlyList<string> pool = new ThemeResolver(theme).Pool(theme.Nouns[0]);

        // Verify
        Assert.Equal([adjective], pool);
    }

    [Fact]
    public void A_noun_reaches_its_own_categories_on_top_of_common()
    {
        // Setup
        Theme theme = ThemeWith(
            adjectives: new() { ["common"] = ["keen"], ["stadium"] = ["roaring"], ["award"] = ["golden"] },
            noun: new Noun("bleachers", ["stadium"]));

        // Exercise
        IReadOnlyList<string> pool = new ThemeResolver(theme).Pool(theme.Nouns[0]);

        // Verify - its own category and common, and nothing from a category it does not carry.
        Assert.Equal(["roaring", "keen"], pool);
    }

    [Fact]
    public void A_word_in_two_of_a_nouns_categories_is_reached_once()
    {
        // Setup
        Theme theme = ThemeWith(
            adjectives: new() { ["common"] = ["keen"], ["water"] = ["keen"] },
            noun: new Noun("river", ["water"]));

        // Exercise
        IReadOnlyList<string> pool = new ThemeResolver(theme).Pool(theme.Nouns[0]);

        // Verify
        Assert.Equal(["keen"], pool);
    }

    [Fact]
    public void A_theme_declaring_no_participle_resolves_an_empty_participle_pool()
    {
        // Setup
        Theme theme = ThemeWith(
            adjectives: new() { ["common"] = [Dummies.AnyWord()] },
            noun: new Noun("moon", []));

        // Exercise
        IReadOnlyList<string> pool = new ThemeResolver(theme).ParticiplePool(theme.Nouns[0]);

        // Verify
        Assert.Empty(pool);
    }

    /// <summary>
    /// The worked example of DEC0002, pinned against the shipped file: moon is declared
    /// [lumineux, mobile] and "waning" lives in participles.common, yet the theme gives
    /// "waning-moon" as a possible draw. It is only possible if a noun that *has* categories
    /// still reaches common - so this is what fails if common is ever narrowed to a fallback
    /// for nouns that declare none.
    /// </summary>
    [Fact]
    public void A_noun_that_declares_categories_still_reaches_common_participles()
    {
        // Setup
        Theme heroku = Themes.LoadEmbedded("heroku");
        Noun moon = heroku.Nouns.Single(noun => noun.Value == "moon");

        // Exercise
        IReadOnlyList<string> participles = new ThemeResolver(heroku).ParticiplePool(moon);

        // Verify
        Assert.NotEmpty(moon.Categories);
        Assert.Contains("waning", participles);
    }

    private static Theme ThemeWith(Dictionary<string, IReadOnlyList<string>> adjectives, Noun noun) =>
        new(Dummies.AnyThemeNameOtherThanTheBuiltInOnes(), adjectives, new Dictionary<string, IReadOnlyList<string>>(), [noun]);
}
