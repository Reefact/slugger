#region Usings declarations

using Slugger.Domain;
using Slugger.Errors;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     DEC0031 and DEC0036: a generator settles a theme and its options once - the theme's own style
///     first, reduced, checked against the floors of what it draws - and then draws as often as asked,
///     the command line's way and the library's alike.
/// </summary>
public sealed class SlugGeneratorTests {

    #region Static members

    private static int AnySeed() {
        return Any.Int32().Between(1, 100_000).Generate();
    }

    /// <summary>The check the library used to leave to you, written as its documentation showed it.</summary>
    private static IReadOnlyList<SluggerThemeError> WhatTheOptionsBreak(ThemeDocument theme, GenerationOptions options) {
        SlugBudget?   budget  = options.MaxLength is { } cap ? new SlugBudget(cap, options) : null;
        ThemeResolver reduced = new(theme, options.SegmentMode, budget, options.MaxSegmentWords);

        return ThemeValidator.Validate(reduced);
    }

    #endregion

    /// <summary>
    ///     Issue #60: docker's style under twenty characters leaves nouns short of the floor its mode
    ///     holds them to, and the library drew from it without a word. Prepared, it is refused - with
    ///     the very reasons the check at startup used to find.
    /// </summary>
    [Fact]
    public void Refuses_a_theme_its_options_leave_short_with_the_reasons_the_validator_gives() {
        // Setup
        SluggerTheme      docker  = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 20 };

        // Exercise
        Outcome<SlugGenerator> prepared = SlugGenerator.Prepare(docker, options);

        // Verify
        Error refusal = prepared.Error!;
        Assert.Equal(SluggerThemeError.Codes.Rejected, refusal.Code);
        Assert.Equal(
            WhatTheOptionsBreak(docker.Content, options).Select(reason => reason.DiagnosticMessage),
            refusal.InnerErrors.Select(reason => reason.DiagnosticMessage));
    }

    [Fact]
    public void Throws_what_it_refuses_when_asked_to_prepare_outright() {
        // Setup
        SluggerTheme      docker  = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 20 };

        // Exercise
        SluggerThemeException thrown = Assert.Throws<SluggerThemeException>(() => SlugGenerator.PrepareOrThrow(docker, options));

        // Verify
        Assert.Equal(SluggerThemeError.Codes.Rejected, thrown.Error.Code);
    }

    /// <summary>A limit nothing fits under is refused when the source is prepared, never on a draw.</summary>
    [Fact]
    public void Refuses_a_limit_nothing_fits_under_when_it_is_prepared() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");

        // Exercise
        Outcome<SlugGenerator> prepared = SlugGenerator.Prepare(docker, GenerationOptions.Default with { MaxLength = 5 }, allowSmall: true);

        // Verify
        Assert.Contains(prepared.Error!.InnerErrors, reason => reason.Code == SluggerThemeError.Codes.NothingFitsTheLimit);
    }

    /// <summary>Waiving the floors is the caller's to ask for, as it is at load.</summary>
    [Fact]
    public void Draws_from_a_theme_its_options_leave_short_when_the_floors_are_waived() {
        // Setup
        SluggerTheme      docker  = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 20 };

        // Exercise
        SlugGenerator source = SlugGenerator.PrepareOrThrow(docker, options, allowSmall: true);

        // Verify
        Assert.True(source.Next().Length <= 20);
    }

    /// <summary>Issue #59's point: what it draws holds to the limit, slug after slug, from one reduction.</summary>
    [Fact]
    public void Keeps_every_slug_within_the_limit_it_was_prepared_with() {
        // Setup
        SluggerTheme      docker  = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 25 };
        SlugGenerator        source  = SlugGenerator.PrepareOrThrow(docker, options);
        DefaultRandomSource random = new(AnySeed());

        // Exercise
        string[] slugs = [.. Enumerable.Range(0, 500).Select(_ => source.Next(random))];

        // Verify
        Assert.All(slugs, slug => Assert.True(slug.Length <= 25, slug));
    }

    /// <summary>
    ///     A mode other than the theme's own is held to that mode's floors (DEC0016), as on the command
    ///     line: slugger declares far fewer than the hundred participles per noun "participle" asks for.
    /// </summary>
    [Fact]
    public void Refuses_a_mode_the_theme_cannot_feed_although_nothing_narrows_it() {
        // Setup
        SluggerTheme slugger = SluggerTheme.LoadEmbeddedOrThrow("slugger");

        // Exercise
        Outcome<SlugGenerator> prepared = SlugGenerator.Prepare(slugger, GenerationOptions.Default with { SegmentMode = SegmentMode.Participle });

        // Verify
        Assert.Equal(SluggerThemeError.Codes.Rejected, prepared.Error!.Code);
    }

    /// <summary>
    ///     A theme drawn as it loaded is not checked again: the load already did. A theme loaded with
    ///     its floors waived would otherwise be refused by a source that was never asked to waive them.
    /// </summary>
    [Fact]
    public void Checks_nothing_again_when_the_options_draw_the_theme_as_it_loaded() {
        // Setup - two nouns, far under every floor, accepted at load because the load waived them.
        const string Json = """
                            {
                              "adjectives": { "common": ["keen", "bold"] },
                              "participles": { "common": ["waning"] },
                              "nouns": [{ "value": "moon" }, { "value": "sun" }]
                            }
                            """;
        SluggerTheme small = SluggerTheme.LoadFromJson(Json, "small", allowSmall: true).GetResultOrThrow();

        // Exercise
        Outcome<SlugGenerator> prepared = SlugGenerator.Prepare(small, GenerationOptions.Default);

        // Verify
        Assert.True(prepared.IsSuccess, prepared.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     Preparing changes what is paid and when, never what is drawn: the same seed gives the slugs
    ///     the one-call overload gives.
    /// </summary>
    [Fact]
    public void Draws_what_generating_slug_by_slug_draws_from_the_same_seed() {
        // Setup
        SluggerTheme      docker  = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 30 };
        int               seed    = AnySeed();
        DefaultRandomSource one   = new(seed);
        DefaultRandomSource other = new(seed);

        // Exercise
        SlugGenerator source   = SlugGenerator.PrepareOrThrow(docker, options);
        string[]   prepared = [.. Enumerable.Range(0, 20).Select(_ => source.Next(one))];
        string[]   direct   = [.. Enumerable.Range(0, 20).Select(_ => SlugDrawer.Generate(docker, options, other))];

        // Verify
        Assert.Equal(direct, prepared);
    }

    /// <summary>
    ///     Several themes, each in its own style - what --mimic-style does - drawn as the documented loop
    ///     drew them: the theme picked by its share of the nouns, then a slug inside it with its options.
    /// </summary>
    [Fact]
    public void Draws_several_themes_each_in_its_own_style_as_picking_them_by_hand_does() {
        // Setup
        SluggerTheme[]      themes = [SluggerTheme.LoadEmbeddedOrThrow("docker"), SluggerTheme.LoadEmbeddedOrThrow("heroku")];
        int                 seed   = AnySeed();
        DefaultRandomSource one    = new(seed);
        DefaultRandomSource other  = new(seed);
        WeightedThemePicker picker = new(themes);

        // Exercise
        SlugGenerator source   = SlugGenerator.PrepareOrThrow(themes, theme => GenerationOptions.Default.WithDefaultsOf(theme));
        string[]   prepared = [.. Enumerable.Range(0, 30).Select(_ => source.Next(one))];
        string[]   byHand   = [
            .. Enumerable.Range(0, 30).Select(_ => {
                SluggerTheme drawn = picker.Pick(other);

                return SlugDrawer.Generate(drawn, GenerationOptions.Default.WithDefaultsOf(drawn), other);
            })
        ];

        // Verify
        Assert.Equal(byHand, prepared);
    }

    [Fact]
    public void Says_which_options_each_of_its_themes_is_drawn_with() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");
        SluggerTheme heroku = SluggerTheme.LoadEmbeddedOrThrow("heroku");

        // Exercise
        SlugGenerator source = SlugGenerator.PrepareOrThrow([docker, heroku], theme => GenerationOptions.Default.WithDefaultsOf(theme));

        // Verify
        Assert.Equal([docker, heroku], source.Themes);
        Assert.Equal(Casing.Snake, source.OptionsFor(docker).Casing);
        Assert.Equal(4, source.OptionsFor(heroku).TokenLength);
        Assert.Throws<ArgumentException>(() => source.OptionsFor(SluggerTheme.LoadEmbeddedOrThrow("slugger")));
    }

    /// <summary>
    ///     One source, built at startup, serves every thread: the reduced pools fill a cache several
    ///     threads may write at once. Each draw here comes from the shared, thread-safe random source.
    /// </summary>
    [Fact]
    public void Serves_several_threads_at_once() {
        // Setup
        SluggerTheme      docker  = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 25 };
        SlugGenerator        source  = SlugGenerator.PrepareOrThrow(docker, options);
        string[]          slugs   = new string[4_000];

        // Exercise
        Parallel.For(0, slugs.Length, index => slugs[index] = source.Next());

        // Verify
        Assert.All(slugs, slug => Assert.InRange(slug.Length, 1, 25));
    }

    /// <summary>
    ///     A theme built in memory was never held to the rules - its constructor checks nothing - so a
    ///     source checks it, even drawn as it is: refused when it is prepared, never when it is drawn.
    /// </summary>
    [Fact]
    public void Refuses_a_theme_built_in_memory_that_no_load_ever_checked() {
        // Setup - not a single noun, which a load would have refused.
        ThemeDocument empty = new(Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
                                  new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"] },
                                  new Dictionary<string, IReadOnlyList<string>>(),
                                  []);

        // Exercise
        Outcome<SlugGenerator> prepared = SlugGenerator.Prepare(empty.HandedOut(), GenerationOptions.Default);

        // Verify
        Assert.Contains(prepared.Error!.InnerErrors, reason => reason.Code == SluggerThemeError.Codes.NoNounToDrawFrom);
    }

    /// <summary>The floors of a theme built in memory are waived the way a load waives them.</summary>
    [Fact]
    public void Holds_a_theme_built_in_memory_to_the_floors_unless_they_are_waived() {
        // Setup - two nouns, far under every floor.
        ThemeDocument small = new(Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
                                  new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen", "bold"] },
                                  new Dictionary<string, IReadOnlyList<string>>(),
                                  [new NounEntry("moon", []), new NounEntry("sun", [])]);

        // Exercise
        Outcome<SlugGenerator> held   = SlugGenerator.Prepare(small.HandedOut(), GenerationOptions.Default);
        Outcome<SlugGenerator> waived = SlugGenerator.Prepare(small.HandedOut(), GenerationOptions.Default, allowSmall: true);

        // Verify
        Assert.Equal(SluggerThemeError.Codes.Rejected, held.Error!.Code);
        Assert.True(waived.IsSuccess, waived.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     A prepared source never changes, whatever becomes of the list it was prepared from: it draws
    ///     from a copy, so a theme replaced or removed afterwards is neither drawn nor missing.
    /// </summary>
    [Fact]
    public void Draws_from_the_themes_it_was_prepared_with_whatever_becomes_of_the_list_given() {
        // Setup
        SluggerTheme        docker = SluggerTheme.LoadEmbeddedOrThrow("docker");
        SluggerTheme        heroku = SluggerTheme.LoadEmbeddedOrThrow("heroku");
        List<SluggerTheme>   given  = [docker, heroku];
        SlugGenerator          source = SlugGenerator.PrepareOrThrow(given, theme => GenerationOptions.Default.WithDefaultsOf(theme));
        DefaultRandomSource random = new(AnySeed());

        // Exercise
        given[0] = SluggerTheme.LoadEmbeddedOrThrow("slugger");
        given.RemoveAt(1);
        string[] slugs = [.. Enumerable.Range(0, 200).Select(_ => source.Next(random))];

        // Verify
        Assert.Equal([docker, heroku], source.Themes);
        Assert.Equal(200, slugs.Length);
    }

    /// <summary>
    ///     What it hands back cannot be written to either: an array behind Themes could be cast back and
    ///     a theme swapped for one the source has no drawing for.
    /// </summary>
    [Fact]
    public void Hands_back_its_themes_in_a_list_nothing_can_write_to() {
        // Setup
        SlugGenerator source = SlugGenerator.PrepareOrThrow([SluggerTheme.LoadEmbeddedOrThrow("docker"), SluggerTheme.LoadEmbeddedOrThrow("heroku")],
                                               theme => GenerationOptions.Default.WithDefaultsOf(theme));
        IList<ThemeDocument>? writable = source.Themes as IList<ThemeDocument>;

        // Verify
        Assert.IsNotType<ThemeDocument[]>(source.Themes);
        Assert.True(writable is null || writable.IsReadOnly);
    }

    [Fact]
    public void Draws_in_the_themes_own_style_when_nothing_adjusts_it() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");

        // Exercise
        SlugGenerator generator = SlugGenerator.ForOrThrow(docker);

        // Verify
        Assert.Equal(GenerationOptions.Default.WithStyle(docker.Defaults), generator.OptionsFor(docker));
    }

    [Fact]
    public void Hands_the_adjustment_the_themes_own_style_to_start_from() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");
        int          limit  = Any.Int32().Between(40, 80).Generate();

        // Exercise
        SlugGenerator generator = SlugGenerator.ForOrThrow(docker, style => style with { MaxLength = limit });

        // Verify
        Assert.Equal(GenerationOptions.Default.WithStyle(docker.Defaults) with { MaxLength = limit }, generator.OptionsFor(docker));
    }

    [Fact]
    public void Adjusts_each_of_several_themes_from_its_own_style() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");
        SluggerTheme heroku = SluggerTheme.LoadEmbeddedOrThrow("heroku");

        // Exercise
        SlugGenerator generator = SlugGenerator.ForOrThrow([docker, heroku], style => style with { TokenLength = 0 });

        // Verify
        Assert.Equal(GenerationOptions.Default.WithStyle(docker.Defaults) with { TokenLength = 0 }, generator.OptionsFor(docker));
        Assert.Equal(GenerationOptions.Default.WithStyle(heroku.Defaults) with { TokenLength = 0 }, generator.OptionsFor(heroku));
    }

    /// <summary>
    ///     Literal on purpose: docker's style under twenty characters is the refusal issue #60 was about,
    ///     and the name form reports it rather than raising it.
    /// </summary>
    [Fact]
    public void Reports_a_refusal_rather_than_throwing_it_under_its_plain_name() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");

        // Exercise
        Outcome<SlugGenerator> outcome = SlugGenerator.For(docker, style => style with { MaxLength = 20 });

        // Verify
        SluggerThemeError refusal = Assert.IsType<SluggerThemeError>(outcome.Error);
        Assert.Equal(SluggerThemeError.Codes.Rejected, refusal.Code);
        Assert.Throws<SluggerThemeException>(() => SlugGenerator.ForOrThrow(docker, style => style with { MaxLength = 20 }));
    }

    [Fact]
    public void Counts_fewer_distinct_slugs_under_a_length_limit_than_without_one() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");

        // Exercise
        long whole   = SlugGenerator.ForOrThrow(docker).Combinations;
        long reduced = SlugGenerator.ForOrThrow(docker, style => style with { MaxLength = 25 }).Combinations;

        // Verify
        Assert.InRange(reduced, 1, whole - 1);
    }

    [Fact]
    public void Counts_what_each_theme_leaves_added_up() {
        // Setup
        SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");
        SluggerTheme heroku = SluggerTheme.LoadEmbeddedOrThrow("heroku");

        // Exercise
        long both = SlugGenerator.ForOrThrow([docker, heroku]).Combinations;

        // Verify
        Assert.Equal(SlugGenerator.ForOrThrow(docker).Combinations + SlugGenerator.ForOrThrow(heroku).Combinations, both);
    }

    /// <summary>
    ///     Literal on purpose: a theme with no participle at all is the case - asked for participles,
    ///     it draws its adjectives, and the count has to say so rather than zero.
    /// </summary>
    [Fact]
    public void Counts_a_theme_without_participles_in_the_mode_it_actually_draws() {
        // Setup
        const string Json = """
                            {
                              "adjectives": { "common": ["calm", "bold", "keen"] },
                              "nouns": [{ "value": "otter" }, { "value": "heron" }]
                            }
                            """;
        SluggerTheme theme = SluggerTheme.LoadFromJsonOrThrow(Json, "small", allowSmall: true);

        // Exercise
        SlugGenerator generator = SlugGenerator.ForOrThrow(theme, style => style with { SegmentMode = SegmentMode.Participle }, allowSmall: true);

        // Verify
        Assert.Equal(6, generator.Combinations);
        Assert.Contains(generator.Next().Split('-')[0], new[] { "calm", "bold", "keen" });
    }

    /// <summary>
    ///     Literal on purpose: the theme has participles, but "sun" reaches none, so under "participle"
    ///     it is drawn with its adjective - "glowing-moon" and "calm-sun" both come out, and the count
    ///     has to say two, noun by noun.
    /// </summary>
    [Fact]
    public void Counts_a_noun_without_participles_with_the_adjective_it_falls_back_to() {
        // Setup
        const string Json = """
                            {
                              "adjectives": { "common": ["calm"] },
                              "participles": { "lit": ["glowing"] },
                              "nouns": [{ "value": "moon", "categories": ["lit"] }, { "value": "sun" }]
                            }
                            """;
        SluggerTheme theme = SluggerTheme.LoadFromJsonOrThrow(Json, "mixed", allowSmall: true);

        // Exercise
        SlugGenerator generator = SlugGenerator.ForOrThrow(theme, style => style with { SegmentMode = SegmentMode.Participle }, allowSmall: true);

        // Verify
        Assert.Equal(2, generator.Combinations);
    }

    /// <summary>
    ///     Literal on purpose: "sun" reaches neither an adjective nor a participle, so it is drawn alone,
    ///     and that is one slug more, not none.
    /// </summary>
    [Fact]
    public void Counts_a_noun_reaching_no_word_as_the_noun_alone() {
        // Setup
        const string Json = """
                            {
                              "adjectives": { "lit": ["bright"] },
                              "participles": { "lit": ["glowing"] },
                              "nouns": [{ "value": "moon", "categories": ["lit"] }, { "value": "sun" }]
                            }
                            """;
        SluggerTheme theme = SluggerTheme.LoadFromJsonOrThrow(Json, "lonely", allowSmall: true);

        // Exercise
        SlugGenerator generator = SlugGenerator.ForOrThrow(theme, style => style with { SegmentMode = SegmentMode.Both }, allowSmall: true);

        // Verify - "bright-glowing-moon" and "sun".
        Assert.Equal(2, generator.Combinations);
    }

    /// <summary>
    ///     Literal on purpose: "calm" refuses "glowing" beside it, so under "both" it can only stand
    ///     alone or with "shining" - the pair the theme forbids is not counted.
    /// </summary>
    [Fact]
    public void Leaves_out_of_the_count_a_pair_the_theme_forbids() {
        // Setup
        const string Json = """
                            {
                              "adjectives": { "common": ["calm", "bold"] },
                              "participles": { "common": ["glowing", "shining"] },
                              "incompatible": { "calm": ["glowing"] },
                              "nouns": [{ "value": "moon" }]
                            }
                            """;
        SluggerTheme theme = SluggerTheme.LoadFromJsonOrThrow(Json, "picky", allowSmall: true);

        // Exercise
        SlugGenerator generator = SlugGenerator.ForOrThrow(theme, style => style with { SegmentMode = SegmentMode.Both }, allowSmall: true);

        // Verify - calm-shining, bold-glowing, bold-shining.
        Assert.Equal(3, generator.Combinations);
    }

}
