using FirstClassErrors;
using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

namespace Slugger.UnitTests;

/// <summary>
/// DEC0018, from both sides: a theme promises a length and is refused when it cannot keep the
/// promise, and a run asks for one and draws from a surface narrowed to it. Neither ever
/// truncates a slug or cuts a word in the middle.
/// </summary>
public sealed class MaxLengthTests
{
    /// <summary>
    /// The promise, and what makes it worth declaring: the word that breaks it does so at load,
    /// where the author is, rather than at the destination that refuses the slug.
    /// </summary>
    [Fact]
    public void A_theme_that_can_exceed_its_own_promise_is_refused_and_shown_the_slug()
    {
        // Setup - "keen-moon" is nine characters and the promise is eight.
        const string Json = """
            {
              "maxLength": { "twoWords": 8 },
              "adjectives": { "common": ["keen"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Error refusal = Assert.Single(
            Reasons(outcome),
            reason => reason.Code == ThemeErrors.Codes.LongerThanPromised);
        Assert.Contains("keen-moon", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("promises 8 characters", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// allowSmall accepts a small theme, never an untrue declaration - a promise waived is a
    /// slug its destination refuses, which is the one thing the option exists to prevent.
    /// </summary>
    [Fact]
    public void Allow_small_does_not_waive_the_promise()
    {
        // Setup
        const string Json = """
            {
              "maxLength": { "twoWords": 8 },
              "adjectives": { "common": ["keen"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Assert.Contains(Reasons(outcome), reason => reason.Code == ThemeErrors.Codes.LongerThanPromised);
    }

    /// <summary>
    /// A key left out promises nothing: the same file passes under "threeWords" because it never
    /// draws three, and nothing is measured against a promise the theme did not make.
    /// </summary>
    [Fact]
    public void A_shape_the_theme_says_nothing_about_is_not_checked()
    {
        // Setup - nine characters, and the promise is about the other shape.
        const string Json = """
            {
              "maxLength": { "threeWords": 8 },
              "adjectives": { "common": ["keen"] },
              "nouns": [{ "value": "moon" }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    /// The promise is measured on pairs that can actually be drawn. The longest word of the file
    /// and its longest noun never meet here, so a theme keeps a promise that a global maximum -
    /// longest word plus longest noun, whether or not one reaches the other - would say it breaks.
    /// </summary>
    [Fact]
    public void The_promise_is_measured_per_noun_rather_than_on_the_longest_word_of_the_file()
    {
        // Setup - "colossal" is reached by "sky" alone, "constellation" reaches "keen" alone.
        const string Json = """
            {
              "maxLength": { "twoWords": 18 },
              "adjectives": { "common": ["keen"], "vast": ["colossal"] },
              "nouns": [{ "value": "constellation" }, { "value": "sky", "categories": ["vast"] }]
            }
            """;

        // Exercise
        Outcome<Theme> outcome = Themes.LoadFromJsonResult(Json, "theme", allowSmall: true);

        // Verify - the longest drawable is "keen-constellation" at eighteen, where "colossal"
        // plus "constellation" would have been twenty-two and is never drawn.
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    /// <summary>
    /// The run's side: a budget takes words out of the pool before the draw rather than trimming
    /// the slug after it, so what comes out is a whole word or nothing.
    /// </summary>
    [Fact]
    public void A_budget_removes_the_words_that_do_not_fit_rather_than_cutting_them()
    {
        // Setup - "moon" reaches both, and only the short one fits in nine characters.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen", "magnificent"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", [])]);
        GenerationOptions options = new() { Separator = '-', MaxLength = 9 };

        // Exercise
        ThemeResolver reduced = SlugGenerator.ResolverFor(theme, options);

        // Verify
        Assert.Equal(["keen"], reduced.Pool(new Noun("moon", [])));
        Assert.Equal("keen-moon", SlugGenerator.Generate(theme, options, new ScriptedRandomSource(0, 0)));
    }

    /// <summary>
    /// A noun no word can stand in front of leaves the surface entirely, so the draw never has to
    /// deal with it - which is what a budget emptying a pool would otherwise mean.
    /// </summary>
    [Fact]
    public void A_noun_no_word_still_fits_in_front_of_leaves_the_surface()
    {
        // Setup - "keen-constellation" is eighteen, "keen-moon" is nine.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", []), new Noun("constellation", [])]);

        // Exercise
        ThemeResolver reduced = SlugGenerator.ResolverFor(theme, new GenerationOptions { Separator = '-', MaxLength = 9 });

        // Verify
        Assert.Equal(["moon"], reduced.Nouns.Select(noun => noun.Value));
    }

    /// <summary>
    /// The hard case, and the only one: nothing fits at all. Known before the first draw rather
    /// than discovered by one, and said in those words rather than as an empty theme.
    /// </summary>
    [Fact]
    public void A_budget_nothing_fits_in_is_refused_by_name()
    {
        // Setup
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", [])]);
        ThemeResolver reduced = SlugGenerator.ResolverFor(theme, new GenerationOptions { Separator = '-', MaxLength = 4 });

        // Exercise
        IReadOnlyList<DomainError> refusals = ThemeValidator.Validate(reduced, allowSmall: true);

        // Verify
        Assert.Contains(refusals, reason => reason.Code == ThemeErrors.Codes.NothingFitsTheLimit);
    }

    /// <summary>
    /// A narrowed surface belongs to the run, so the mode its floors follow is the run's - not
    /// the one the theme would have chosen for itself. Reading the theme's would measure a shape
    /// nothing is drawing.
    /// </summary>
    [Fact]
    public void The_floors_of_a_narrowed_surface_follow_the_runs_mode_not_the_themes()
    {
        // Setup - the theme says nothing, so it would draw "both"; the run asks for one word.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"] },
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["waning"] },
            [new Noun("moon", [])]);
        GenerationOptions options = new() { Separator = '-', SegmentMode = SegmentMode.Either, MaxLength = 40 };

        // Exercise
        SegmentMode drawn = ThemeValidator.DrawnMode(SlugGenerator.ResolverFor(theme, options));

        // Verify
        Assert.Equal(SegmentMode.Either, drawn);
    }

    /// <summary>
    /// DEC0020 against DEC0018: "threeOrTwo" may draw one word, so no room is reserved in front
    /// of the noun and an adjective leaving none stays in the pool. Where "both" has to throw it
    /// away, here the participle pool behind it simply comes back empty and the absence is all
    /// there is left to draw - so the ceiling holds and the theme keeps the word.
    /// </summary>
    [Fact]
    public void Three_or_two_keeps_an_adjective_that_leaves_no_room_for_a_participle()
    {
        // Setup - "magnificent-moon" is sixteen exactly, "magnificent-waning-moon" twenty-three.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen", "magnificent"] },
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["waning"] },
            [new Noun("moon", [])]);
        GenerationOptions ceiling = new() { Separator = '-', MaxLength = 16 };

        // Exercise
        IReadOnlyList<string> underBoth = SlugGenerator
            .ResolverFor(theme, ceiling with { SegmentMode = SegmentMode.Both })
            .Pool(theme.Nouns[0]);
        IReadOnlyList<string> underThreeOrTwo = SlugGenerator
            .ResolverFor(theme, ceiling with { SegmentMode = SegmentMode.ThreeOrTwo })
            .Pool(theme.Nouns[0]);

        // Verify
        Assert.Equal(["keen"], underBoth);
        Assert.Equal(["keen", "magnificent"], underThreeOrTwo);
    }

    /// <summary>The other half: drawing that adjective produces a slug that fits, not one that is trimmed.</summary>
    [Fact]
    public void The_ceiling_holds_when_three_or_two_draws_that_adjective()
    {
        // Setup - the noun, then "magnificent"; nothing fits behind it, so no second draw is made.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen", "magnificent"] },
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["waning"] },
            [new Noun("moon", [])]);
        GenerationOptions options = new()
        {
            Separator = '-', MaxLength = 16, SegmentMode = SegmentMode.ThreeOrTwo,
        };
        ScriptedRandomSource random = new(0, 1);

        // Exercise
        string slug = SlugGenerator.Generate(theme, options, random);

        // Verify
        Assert.Equal("magnificent-moon", slug);
        Assert.Equal(0, random.Remaining);
    }

    /// <summary>
    /// The token is part of the slug the destination receives, so it is part of the budget - and
    /// counted as drawn even where TokenChance makes it rare, because a slug that only fits when
    /// the token does not show up does not fit.
    /// </summary>
    [Fact]
    public void The_token_counts_against_the_budget_even_when_it_is_rare()
    {
        // Setup - "keen-moon" is nine, plus a separator and four digits.
        SlugBudget budget = new(13, new GenerationOptions { Separator = '-', TokenLength = 4, TokenChance = 1 });

        // Verify
        Assert.False(budget.Fits("keen", "moon"));
        Assert.True(new SlugBudget(14, new GenerationOptions { Separator = '-', TokenLength = 4 }).Fits("keen", "moon"));
    }

    /// <summary>
    /// The case only a budget produces: no pair refuses anything, and a noun still loses its
    /// participles behind the one long adjective it can draw. It is refused with its own message,
    /// because raising a ceiling and dropping a pair are not the same repair.
    /// </summary>
    [Fact]
    public void A_limit_that_starves_a_noun_behind_one_adjective_is_refused_in_its_own_words()
    {
        // Setup - 100 short adjectives and one of twenty characters; 5 short participles and 20
        // long ones. Only the short participles still fit behind the long adjective.
        string[] nouns = [.. Enumerable.Range(0, 100).Select(index => $"n{index:00}")];
        string[] adjectives = [.. Enumerable.Range(0, 100).Select(index => $"a{index:00}"), new string('w', 20)];
        string[] participles =
        [
            .. Enumerable.Range(0, 5).Select(index => $"p{index:00}"),
            .. Enumerable.Range(0, 20).Select(index => $"q{index:00}{new string('z', 5)}"),
        ];
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = adjectives },
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = participles },
            [.. nouns.Select(value => new Noun(value, []))]);
        GenerationOptions options = new() { Separator = '-', SegmentMode = SegmentMode.Both, MaxLength = 32 };

        // Exercise
        IReadOnlyList<DomainError> refusals = ThemeValidator.Validate(SlugGenerator.ResolverFor(theme, options));

        // Verify
        DomainError refusal = Assert.Single(
            refusals,
            reason => reason.Code == ThemeErrors.Codes.TheLimitStarvesTheNoun && reason.DiagnosticMessage.Contains("n00", StringComparison.Ordinal));
        Assert.Contains("Under 32 characters", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("reaches 5 participles", refusal.DiagnosticMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("incompatibility", refusal.DiagnosticMessage, StringComparison.Ordinal);
    }

    private static IReadOnlyList<Error> Reasons(Outcome<Theme> outcome) => outcome.Error?.InnerErrors ?? [];
}
