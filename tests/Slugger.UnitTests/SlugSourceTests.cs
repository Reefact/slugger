#region Usings declarations

using Slugger.Domain;
using Slugger.Domain.Errors;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     DEC0031: a source settles a theme and its options once - reduced, checked against the floors of
///     what it draws - and then draws as often as asked, the command line's way and the library's alike.
/// </summary>
public sealed class SlugSourceTests {

    #region Static members

    private static int AnySeed() {
        return Any.Int32().Between(1, 100_000).Generate();
    }

    /// <summary>The check the library used to leave to you, written as its documentation showed it.</summary>
    private static IReadOnlyList<DomainError> WhatTheOptionsBreak(ThemeDocument theme, GenerationOptions options) {
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
        ThemeDocument     docker  = Themes.LoadEmbedded("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 20 };

        // Exercise
        Outcome<SlugSource> prepared = SlugSource.PrepareResult(docker, options);

        // Verify
        Error refusal = prepared.Error!;
        Assert.Equal(ThemeErrors.Codes.Rejected, refusal.Code);
        Assert.Equal(
            WhatTheOptionsBreak(docker, options).Select(reason => reason.DiagnosticMessage),
            refusal.InnerErrors.Select(reason => reason.DiagnosticMessage));
    }

    [Fact]
    public void Throws_what_it_refuses_when_asked_to_prepare_outright() {
        // Setup
        ThemeDocument     docker  = Themes.LoadEmbedded("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 20 };

        // Exercise
        DomainException thrown = Assert.Throws<DomainException>(() => SlugSource.Prepare(docker, options));

        // Verify
        Assert.Equal(ThemeErrors.Codes.Rejected, thrown.Error.Code);
    }

    /// <summary>A limit nothing fits under is refused when the source is prepared, never on a draw.</summary>
    [Fact]
    public void Refuses_a_limit_nothing_fits_under_when_it_is_prepared() {
        // Setup
        ThemeDocument docker = Themes.LoadEmbedded("docker");

        // Exercise
        Outcome<SlugSource> prepared = SlugSource.PrepareResult(docker, GenerationOptions.Default with { MaxLength = 5 }, allowSmall: true);

        // Verify
        Assert.Contains(prepared.Error!.InnerErrors, reason => reason.Code == ThemeErrors.Codes.NothingFitsTheLimit);
    }

    /// <summary>Waiving the floors is the caller's to ask for, as it is at load.</summary>
    [Fact]
    public void Draws_from_a_theme_its_options_leave_short_when_the_floors_are_waived() {
        // Setup
        ThemeDocument     docker  = Themes.LoadEmbedded("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 20 };

        // Exercise
        SlugSource source = SlugSource.Prepare(docker, options, allowSmall: true);

        // Verify
        Assert.True(source.Next().Length <= 20);
    }

    /// <summary>Issue #59's point: what it draws holds to the limit, slug after slug, from one reduction.</summary>
    [Fact]
    public void Keeps_every_slug_within_the_limit_it_was_prepared_with() {
        // Setup
        ThemeDocument     docker  = Themes.LoadEmbedded("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 25 };
        SlugSource        source  = SlugSource.Prepare(docker, options);
        DefaultRandomSource random = new(AnySeed());

        // Exercise
        string[] slugs = [.. Enumerable.Range(0, 500).Select(_ => source.Next(random))];

        // Verify
        Assert.All(slugs, slug => Assert.True(slug.Length <= 25, slug));
    }

    /// <summary>
    ///     A mode other than the theme's own is held to that mode's floors (DEC0016), as on the command
    ///     line: no built-in theme declares enough participles per noun to draw "participle".
    /// </summary>
    [Fact]
    public void Refuses_a_mode_the_theme_cannot_feed_although_nothing_narrows_it() {
        // Setup
        ThemeDocument slugger = Themes.LoadEmbedded("slugger");

        // Exercise
        Outcome<SlugSource> prepared = SlugSource.PrepareResult(slugger, GenerationOptions.Default with { SegmentMode = SegmentMode.Participle });

        // Verify
        Assert.Equal(ThemeErrors.Codes.Rejected, prepared.Error!.Code);
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
        ThemeDocument small = Themes.LoadFromJsonResult(Json, "small", allowSmall: true).GetResultOrThrow();

        // Exercise
        Outcome<SlugSource> prepared = SlugSource.PrepareResult(small, GenerationOptions.Default);

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
        ThemeDocument     docker  = Themes.LoadEmbedded("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 30 };
        int               seed    = AnySeed();
        DefaultRandomSource one   = new(seed);
        DefaultRandomSource other = new(seed);

        // Exercise
        SlugSource source   = SlugSource.Prepare(docker, options);
        string[]   prepared = [.. Enumerable.Range(0, 20).Select(_ => source.Next(one))];
        string[]   direct   = [.. Enumerable.Range(0, 20).Select(_ => SlugGenerator.Generate(docker, options, other))];

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
        ThemeDocument[]     themes = [Themes.LoadEmbedded("docker"), Themes.LoadEmbedded("heroku")];
        int                 seed   = AnySeed();
        DefaultRandomSource one    = new(seed);
        DefaultRandomSource other  = new(seed);
        WeightedThemePicker picker = new(themes);

        // Exercise
        SlugSource source   = SlugSource.Prepare(themes, theme => GenerationOptions.Default.WithDefaultsOf(theme));
        string[]   prepared = [.. Enumerable.Range(0, 30).Select(_ => source.Next(one))];
        string[]   byHand   = [
            .. Enumerable.Range(0, 30).Select(_ => {
                ThemeDocument drawn = picker.Pick(other);

                return SlugGenerator.Generate(drawn, GenerationOptions.Default.WithDefaultsOf(drawn), other);
            })
        ];

        // Verify
        Assert.Equal(byHand, prepared);
    }

    [Fact]
    public void Says_which_options_each_of_its_themes_is_drawn_with() {
        // Setup
        ThemeDocument docker = Themes.LoadEmbedded("docker");
        ThemeDocument heroku = Themes.LoadEmbedded("heroku");

        // Exercise
        SlugSource source = SlugSource.Prepare([docker, heroku], theme => GenerationOptions.Default.WithDefaultsOf(theme));

        // Verify
        Assert.Equal([docker, heroku], source.Themes);
        Assert.Equal(Casing.Snake, source.OptionsFor(docker).Casing);
        Assert.Equal(4, source.OptionsFor(heroku).TokenLength);
        Assert.Throws<ArgumentException>(() => source.OptionsFor(Themes.LoadEmbedded("slugger")));
    }

    /// <summary>
    ///     One source, built at startup, serves every thread: the reduced pools fill a cache several
    ///     threads may write at once. Each draw here comes from the shared, thread-safe random source.
    /// </summary>
    [Fact]
    public void Serves_several_threads_at_once() {
        // Setup
        ThemeDocument     docker  = Themes.LoadEmbedded("docker");
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 25 };
        SlugSource        source  = SlugSource.Prepare(docker, options);
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
        Outcome<SlugSource> prepared = SlugSource.PrepareResult(empty, GenerationOptions.Default);

        // Verify
        Assert.Contains(prepared.Error!.InnerErrors, reason => reason.Code == ThemeErrors.Codes.NoNounToDrawFrom);
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
        Outcome<SlugSource> held   = SlugSource.PrepareResult(small, GenerationOptions.Default);
        Outcome<SlugSource> waived = SlugSource.PrepareResult(small, GenerationOptions.Default, allowSmall: true);

        // Verify
        Assert.Equal(ThemeErrors.Codes.Rejected, held.Error!.Code);
        Assert.True(waived.IsSuccess, waived.Error?.DiagnosticMessage);
    }

    /// <summary>
    ///     A prepared source never changes, whatever becomes of the list it was prepared from: it draws
    ///     from a copy, so a theme replaced or removed afterwards is neither drawn nor missing.
    /// </summary>
    [Fact]
    public void Draws_from_the_themes_it_was_prepared_with_whatever_becomes_of_the_list_given() {
        // Setup
        ThemeDocument       docker = Themes.LoadEmbedded("docker");
        ThemeDocument       heroku = Themes.LoadEmbedded("heroku");
        List<ThemeDocument> given  = [docker, heroku];
        SlugSource          source = SlugSource.Prepare(given, theme => GenerationOptions.Default.WithDefaultsOf(theme));
        DefaultRandomSource random = new(AnySeed());

        // Exercise
        given[0] = Themes.LoadEmbedded("slugger");
        given.RemoveAt(1);
        string[] slugs = [.. Enumerable.Range(0, 200).Select(_ => source.Next(random))];

        // Verify
        Assert.Equal([docker, heroku], source.Themes);
        Assert.Equal(200, slugs.Length);
    }

}
