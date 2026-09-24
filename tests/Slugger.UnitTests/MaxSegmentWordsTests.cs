#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     DEC0023, from both sides: a theme states how wide its segments run and a run overrides it.
///     The cap counts the words of one value, never of the slug, and it removes a value rather than
///     shorten it - the same bargain <see cref="MaxLengthTests" /> pins for characters.
/// </summary>
public sealed class MaxSegmentWordsTests {

    #region Static members

    private static Theme ThemeWith(string[] adjectives, string[] nouns) {
        return new Theme(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = adjectives },
            new Dictionary<string, IReadOnlyList<string>>(),
            [.. nouns.Select(noun => new NounEntry(noun, []))]);
    }

    #endregion

    /// <summary>
    ///     The bargain itself. "long forgotten" leaves the pool whole rather than come out as
    ///     "long", which is what makes the cap safe to set on a theme somebody else wrote.
    /// </summary>
    [Fact]
    public void A_cap_removes_a_value_rather_than_shortening_it() {
        // Setup
        Theme             theme   = ThemeWith(["keen", "long forgotten"], ["moon"]);
        GenerationOptions options = new() { Separator = '-', MaxSegmentWords = 1 };

        // Exercise
        ThemeResolver reduced = SlugGenerator.ResolverFor(theme, options);

        // Verify
        Assert.Equal(["keen"], reduced.Pool(theme.Nouns[0]));
        Assert.Equal("keen-moon", SlugGenerator.Generate(theme, options, new ScriptedRandomSource(0, 0)));
    }

    /// <summary>
    ///     The noun is measured in its own right, which a budget never has to be: a two word noun
    ///     still reaches every one word adjective, so nothing else would have taken it out. This is
    ///     the case the cap exists for - a compound noun is usually the long part.
    /// </summary>
    [Fact]
    public void A_noun_of_more_words_than_the_cap_leaves_the_surface() {
        // Setup
        Theme theme = ThemeWith(["keen"], ["moon", "harvest moon"]);

        // Exercise
        ThemeResolver reduced = SlugGenerator.ResolverFor(
            theme,
            new GenerationOptions { Separator = '-', MaxSegmentWords = 1 });

        // Verify
        Assert.Equal(["moon"], reduced.Nouns.Select(noun => noun.Value));
    }

    /// <summary>
    ///     A cap of two keeps a two word value: the figure is what a value may carry, not what it
    ///     must stay under.
    /// </summary>
    [Fact]
    public void A_value_of_exactly_the_cap_is_kept() {
        // Setup
        Theme theme = ThemeWith(["long forgotten", "all but forgotten"], ["moon"]);

        // Exercise
        ThemeResolver reduced = SlugGenerator.ResolverFor(
            theme,
            new GenerationOptions { Separator = '-', MaxSegmentWords = 2 });

        // Verify
        Assert.Equal(["long forgotten"], reduced.Pool(theme.Nouns[0]));
    }

    /// <summary>
    ///     The hard case, and the reason it needed a refusal of its own: a cap can empty the surface
    ///     while no budget is in force, where reading the budget to explain it would have thrown.
    /// </summary>
    [Fact]
    public void A_cap_no_noun_is_short_enough_for_is_refused_by_name() {
        // Setup
        Theme theme = ThemeWith(["keen"], ["harvest moon"]);
        ThemeResolver reduced = SlugGenerator.ResolverFor(
            theme,
            new GenerationOptions { Separator = '-', MaxSegmentWords = 1 });

        // Exercise
        IReadOnlyList<DomainError> refusals = ThemeValidator.Validate(reduced, true);

        // Verify
        DomainError refusal = Assert.Single(
            refusals,
            reason => reason.Code == ThemeErrors.Codes.NoValueIsShortEnough);
        Assert.Contains("1 word or fewer", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     A theme states how wide its segments run, as it states its separator: docker and heroku
    ///     draw one word a segment and would say so here rather than leave it to each caller.
    /// </summary>
    [Fact]
    public void A_theme_states_its_own_cap_in_its_defaults() {
        // Setup
        const string Json = """
                            {
                              "defaults": { "maxSegmentWords": 1 },
                              "adjectives": { "common": ["keen", "long forgotten"] },
                              "nouns": [{ "value": "moon" }]
                            }
                            """;
        Outcome<Theme> loaded = Themes.LoadFromJsonResult(Json, "theme", true);
        Theme          theme  = loaded.GetResultOrThrow();

        // Exercise
        ThemeResolver reduced = SlugGenerator.ResolverFor(
            theme,
            GenerationOptions.Default.WithDefaultsOf(theme));

        // Verify
        Assert.Equal(1, theme.Defaults.MaxSegmentWords);
        Assert.Equal(["keen"], reduced.Pool(theme.Nouns[0]));
    }

    /// <summary>
    ///     Pins the chain of DEC0004 on this option: an explicit argument outranks the theme, so a
    ///     run widens a style the author narrowed rather than being stuck with it.
    /// </summary>
    [Fact]
    public void An_explicit_cap_outranks_the_one_the_theme_states() {
        // Setup
        const string Json = """
                            {
                              "defaults": { "maxSegmentWords": 1 },
                              "adjectives": { "common": ["keen", "long forgotten"] },
                              "nouns": [{ "value": "moon" }]
                            }
                            """;
        Theme theme = Themes.LoadFromJsonResult(Json, "theme", true).GetResultOrThrow();

        // Exercise
        GenerationOptions widened =
            GenerationOptions.Default.WithDefaultsOf(theme) with { MaxSegmentWords = 2 };

        // Verify
        Assert.Equal(
            ["keen", "long forgotten"],
            SlugGenerator.ResolverFor(theme, widened).Pool(theme.Nouns[0]));
    }

    /// <summary>
    ///     No cap narrows nothing, which is what lets the resolver hand a pool back untouched rather
    ///     than copy it to remove nothing from.
    /// </summary>
    [Fact]
    public void A_run_with_no_cap_narrows_nothing() {
        // Setup
        Theme theme = ThemeWith(["keen", "long forgotten"], ["harvest moon"]);

        // Exercise
        ThemeResolver resolver = SlugGenerator.ResolverFor(theme, GenerationOptions.Default);

        // Verify
        Assert.False(resolver.Narrows);
        Assert.Equal(["keen", "long forgotten"], resolver.Pool(theme.Nouns[0]));
    }

    /// <summary>
    ///     A cap below one asks for a value of no words at all, which no value can be. Refused where
    ///     it is built rather than silently emptying every pool.
    /// </summary>
    [Fact]
    public void A_cap_below_one_is_rejected() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ThemeResolver(ThemeWith(["keen"], ["moon"]), maxSegmentWords: 0));
    }

    /// <summary>
    ///     A style a theme states about itself is what its floors are measured against - the same
    ///     reading DEC0016 takes of "segmentMode". Loading the whole file instead would accept a
    ///     theme whose every draw is refused, which is to say the key would mean nothing.
    /// </summary>
    [Fact]
    public void A_themes_own_cap_is_what_its_floors_are_measured_against() {
        // Setup - every noun is written in two words, so a cap of one leaves nothing to draw on.
        const string Json = """
                            {
                              "defaults": { "maxSegmentWords": 1 },
                              "adjectives": { "common": ["keen"] },
                              "nouns": [{ "value": "harvest moon" }]
                            }
                            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", true);

        // Verify
        Assert.Contains(
            outcome.Error?.InnerErrors ?? [],
            reason => reason.Code == ThemeErrors.Codes.NoValueIsShortEnough);
    }

}