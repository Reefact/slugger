using FirstClassErrors;
using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Validation;

namespace Slugger.UnitTests;

public sealed class SlugGeneratorTests
{
    private static readonly GenerationOptions Plain = new() { Separator = '-', Casing = Casing.Kebab };

    [Fact]
    public void Draws_an_adjective_and_the_noun()
    {
        // Setup - one noun, two adjectives; the script takes the first noun then the second adjective.
        Theme theme = ThemeWith(adjectives: ["keen", "gorgeous"], participles: []);
        ScriptedRandomSource random = new(0, 1);

        // Exercise
        string slug = SlugGenerator.Generate(theme, Plain with { SegmentMode = SegmentMode.Adjective }, random);

        // Verify
        Assert.Equal("gorgeous-moon", slug);
        Assert.Equal(0, random.Remaining);
    }

    [Fact]
    public void Draws_a_participle_and_the_noun()
    {
        // Setup
        Theme theme = ThemeWith(adjectives: ["keen"], participles: ["waning", "rising"]);
        ScriptedRandomSource random = new(0, 1);

        // Exercise
        string slug = SlugGenerator.Generate(theme, Plain with { SegmentMode = SegmentMode.Participle }, random);

        // Verify
        Assert.Equal("rising-moon", slug);
    }

    /// <summary>
    /// Literal on purpose: "charming" is an adjective and a present participle alike, so a theme
    /// may legitimately declare it in both sections and "both" can then draw it twice. The slug
    /// carries it once - the same degradation as a noun that reaches no participle at all.
    /// </summary>
    [Fact]
    public void Both_writes_a_word_once_when_it_is_drawn_as_adjective_and_participle()
    {
        // Setup - one word in each pool, so the two draws cannot help but collide.
        Theme theme = ThemeWith(adjectives: ["charming"], participles: ["charming"]);
        ScriptedRandomSource random = new(0, 0, 0);

        // Exercise
        string slug = SlugGenerator.Generate(theme, Plain with { SegmentMode = SegmentMode.Both }, random);

        // Verify
        Assert.Equal("charming-moon", slug);
    }

    /// <summary>
    /// Both draws are still made when they collide, so the random source is consumed identically
    /// either way. Re-drawing until they differed would have been unbounded on a pool of one, and
    /// would have made a scripted draw unpredictable.
    /// </summary>
    [Fact]
    public void A_collision_consumes_the_same_draws_as_a_slug_that_keeps_both_words()
    {
        // Setup - the scripted source fails on a mismatch, so the count is asserted by using it.
        Theme theme = ThemeWith(adjectives: ["charming", "keen"], participles: ["charming", "waning"]);
        // The noun is drawn first, then the adjective, then the participle.
        ScriptedRandomSource collided = new(0, 0, 0);
        ScriptedRandomSource distinct = new(0, 1, 1);

        // Exercise
        string one = SlugGenerator.Generate(theme, Plain with { SegmentMode = SegmentMode.Both }, collided);
        string two = SlugGenerator.Generate(theme, Plain with { SegmentMode = SegmentMode.Both }, distinct);

        // Verify - three draws each, two segments against three.
        Assert.Equal("charming-moon", one);
        Assert.Equal("keen-waning-moon", two);
    }

    [Fact]
    public void Both_draws_an_adjective_then_a_participle_then_the_noun()
    {
        // Setup
        Theme theme = ThemeWith(adjectives: ["keen"], participles: ["waning"]);
        ScriptedRandomSource random = new(0, 0, 0);

        // Exercise
        string slug = SlugGenerator.Generate(theme, Plain with { SegmentMode = SegmentMode.Both }, random);

        // Verify
        Assert.Equal("keen-waning-moon", slug);
    }

    /// <summary>
    /// Either is one word before the noun, never two - it is what reproduces the shape of Docker
    /// and Heroku while drawing from the adjective and participle vocabularies together.
    /// </summary>
    [Theory]
    [InlineData(0, "waning-moon")]
    [InlineData(1, "keen-moon")]
    public void Either_draws_one_word_or_the_other_but_never_both(int coin, string expected)
    {
        // Setup
        Theme theme = ThemeWith(adjectives: ["keen"], participles: ["waning"]);
        ScriptedRandomSource random = new(0, coin, 0);

        // Exercise
        string slug = SlugGenerator.Generate(theme, Plain with { SegmentMode = SegmentMode.Either }, random);

        // Verify
        Assert.Equal(expected, slug);
    }

    /// <summary>
    /// Silent degradation: a noun that reaches no participle does not fail the
    /// generation, it falls back to the adjective alone for that draw.
    /// </summary>
    [Theory]
    [InlineData(SegmentMode.Participle)]
    [InlineData(SegmentMode.Either)]
    [InlineData(SegmentMode.Both)]
    public void Falls_back_to_the_adjective_when_the_noun_reaches_no_participle(SegmentMode mode)
    {
        // Setup
        Theme theme = ThemeWith(adjectives: ["keen"], participles: []);
        ScriptedRandomSource random = new(0, 0);

        // Exercise
        string slug = SlugGenerator.Generate(theme, Plain with { SegmentMode = mode }, random);

        // Verify
        Assert.Equal("keen-moon", slug);
    }

    /// <summary>
    /// Only reachable through allowSmall, since validation refuses a noun with an empty pool.
    /// The noun on its own beats an exception the author already said they accepted.
    /// </summary>
    [Fact]
    public void Yields_the_noun_alone_when_it_reaches_neither_adjective_nor_participle()
    {
        // Setup
        Theme theme = ThemeWith(adjectives: [], participles: []);
        ScriptedRandomSource random = new(0);

        // Exercise
        string slug = SlugGenerator.Generate(theme, Plain, random);

        // Verify
        Assert.Equal("moon", slug);
    }

    [Fact]
    public void A_noun_never_draws_an_adjective_from_a_category_it_does_not_carry()
    {
        // Setup - "moon" carries lumineux only; "roaring" sits in stadium and must stay out of reach.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["lumineux"] = ["waxing"],
                ["stadium"] = ["roaring"],
                ["common"] = ["keen"],
            },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", ["lumineux"])]);

        // Exercise - every adjective the noun can reach, drawn one after the other.
        string[] slugs =
        [
            SlugGenerator.Generate(theme, Plain, new ScriptedRandomSource(0, 0)),
            SlugGenerator.Generate(theme, Plain, new ScriptedRandomSource(0, 1)),
        ];

        // Verify - its own category and common, and nothing from stadium.
        Assert.Equal(["waxing-moon", "keen-moon"], slugs);
    }

    [Fact]
    public void The_same_seed_replays_the_same_slug()
    {
        // Setup
        Theme theme = Themes.LoadEmbedded("docker");
        int seed = Any.Int32().Between(1, 100_000).Generate();
        GenerationOptions options = Plain with { Seed = seed, TokenLength = 4 };

        // Exercise
        string first = SlugGenerator.Generate(theme, options);
        string second = SlugGenerator.Generate(theme, options);

        // Verify
        Assert.Equal(first, second);
    }

    [Fact]
    public void A_different_seed_reaches_a_different_slug()
    {
        // Setup - docker holds 236 nouns and 187 adjectives, so a collision between two given
        // seeds is possible but rare; a handful of seeds makes the assertion safe.
        Theme theme = Themes.LoadEmbedded("docker");

        // Exercise
        string[] slugs = Enumerable.Range(1, 8)
            .Select(seed => SlugGenerator.Generate(theme, Plain with { Seed = seed }))
            .ToArray();

        // Verify
        Assert.True(slugs.Distinct(StringComparer.Ordinal).Count() > 1, string.Join(", ", slugs));
    }

    /// <summary>
    /// Unreachable through any catalog now that the validator refuses it, but a consumer may
    /// build a Theme in memory. The same situation, named by the same factory,
    /// travelling as an exception because this overload promises a string.
    /// </summary>
    [Fact]
    public void A_theme_built_with_no_noun_raises_the_same_named_error()
    {
        // Setup
        Theme empty = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            []);

        // Exercise
        DomainException raised = Assert.Throws<DomainException>(
            () => SlugGenerator.Generate(empty, Plain, new ScriptedRandomSource()));

        // Verify
        Assert.Equal(ThemeErrors.Codes.NoNounToDrawFrom, raised.Error.Code);
    }

    private static Theme ThemeWith(IReadOnlyList<string> adjectives, IReadOnlyList<string> participles) =>
        new(Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            UnderCommon(adjectives),
            UnderCommon(participles),
            [new Noun("moon", [])]);

    /// <summary>The words as a single "common" category, or no category at all when there are none.</summary>
    private static Dictionary<string, IReadOnlyList<string>> UnderCommon(IReadOnlyList<string> words)
    {
        Dictionary<string, IReadOnlyList<string>> groups = [];
        if (words.Count > 0)
        {
            groups["common"] = words;
        }

        return groups;
    }
}
