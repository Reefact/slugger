#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.UnitTests;

public sealed class GenerateSlugsUseCaseTests {

    #region Static members

    internal static Theme ThemeNamed(string name) {
        return new Theme(name,
                         new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen", "gorgeous"] },
                         new Dictionary<string, IReadOnlyList<string>>(),
                         [new NounEntry("moon", []), new NounEntry("river", [])]);
    }

    #endregion

    [Fact]
    public void Draws_from_the_default_theme_when_none_is_named() {
        // Setup
        FakeThemeCatalog     catalog = new(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName));
        GenerateSlugsUseCase useCase = new(new FakeThemeDirectory(catalog), new FakeConfigStore(), new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.Single(outcome.GetResultOrThrow());
    }

    [Fact]
    public void Draws_as_many_slugs_as_count_asks_for() {
        // Setup
        int count = Any.Int32().Between(2, 12).Generate();
        GenerateSlugsUseCase useCase = new(
            new FakeThemeDirectory(new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName))),
            new FakeConfigStore(),
            new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(new SluggerOptions { Count = count });

        // Verify
        Assert.Equal(count, outcome.GetResultOrThrow().Count);
    }

    /// <summary>
    ///     One random source drives the whole batch, so a seeded run replays every slug of it -
    ///     not just the first, which a fresh source per draw would give.
    /// </summary>
    [Fact]
    public void The_same_seed_replays_the_whole_batch() {
        // Setup
        GenerateSlugsUseCase useCase = new(
            new FakeThemeDirectory(new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName))),
            new FakeConfigStore(),
            new FakeClipboard());
        SluggerOptions options = new() { Count = 6, Seed = Any.Int32().Between(1, 100_000).Generate() };

        // Exercise
        IReadOnlyList<string> first  = useCase.Execute(options).GetResultOrThrow();
        IReadOnlyList<string> second = useCase.Execute(options).GetResultOrThrow();

        // Verify
        Assert.Equal(first, second);
    }

    [Fact]
    public void Copies_nothing_unless_the_clipboard_was_asked_for() {
        // Setup
        FakeClipboard clipboard = new();
        GenerateSlugsUseCase useCase = new(
            new FakeThemeDirectory(new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName))),
            new FakeConfigStore(),
            clipboard);

        // Exercise
        useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.Equal(0, clipboard.Copies);
    }

    [Fact]
    public void Copies_the_last_slug_of_a_round_when_asked() {
        // Setup
        FakeClipboard clipboard = new();
        GenerateSlugsUseCase useCase = new(
            new FakeThemeDirectory(new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName))),
            new FakeConfigStore(),
            clipboard);

        // Exercise
        IReadOnlyList<string> slugs = useCase.Execute(new SluggerOptions { Count = 3, Clipboard = true }).GetResultOrThrow();

        // Verify
        Assert.Equal(slugs[^1], clipboard.LastCopied);
    }

    /// <summary>
    ///     Generating from the themes that did load would hide the broken one, and the author would
    ///     never learn their file is wrong.
    /// </summary>
    [Fact]
    public void One_refused_theme_fails_the_whole_batch() {
        // Setup
        FakeThemeCatalog catalog = new(ThemeNamed("good"));
        catalog.Broken.Add("bad");
        GenerateSlugsUseCase useCase = new(new FakeThemeDirectory(catalog), new FakeConfigStore(), new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(new SluggerOptions { Themes = ["good", "bad"] });

        // Verify
        Assert.True(outcome.IsFailure);
        Assert.Equal(ThemeErrors.Codes.Rejected, outcome.Error!.Code);
    }

    [Fact]
    public void A_saved_config_supplies_what_the_command_line_leaves_out() {
        // Setup
        FakeConfigStore config = new(new SluggerOptions { Count = 4 });
        GenerateSlugsUseCase useCase = new(
            new FakeThemeDirectory(new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName))),
            config,
            new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.Equal(4, outcome.GetResultOrThrow().Count);
    }

    /// <summary>
    ///     DEC0016 reached through a run rather than through a file. <c>--segment</c> passes over the
    ///     theme's own mode (DEC0004), so what the run draws is not what the load measured, and the
    ///     theme is judged again against the floors that now apply. heroku ships 20 participles for
    ///     its poorest noun - all "either" ever asks of it, and a fifth of what "participle" does.
    /// </summary>
    [Fact]
    public void A_run_asking_for_a_mode_the_theme_cannot_sustain_is_refused_rather_than_drawn() {
        // Setup - the shipped file rather than a fixture: the point is what heroku really reaches.
        GenerateSlugsUseCase useCase = new(
            new FakeThemeDirectory(new EmbeddedThemeCatalog()),
            new FakeConfigStore(),
            new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(
            new SluggerOptions { Themes = ["heroku"], SegmentMode = SegmentMode.Participle });

        // Verify
        Assert.True(outcome.IsFailure);
        Assert.Contains(
            outcome.Error!.InnerErrors,
            reason => reason.Code == ThemeErrors.Codes.ParticiplePoolTooSmall);
    }

    /// <summary>
    ///     The counterpart, and what keeps the test above from passing for some other reason: the same
    ///     theme through the same use case, drawn in the mode it was written for. A run that changes
    ///     neither the surface nor the floors is not judged a second time - it was judged when it
    ///     loaded.
    /// </summary>
    [Fact]
    public void A_run_in_the_themes_own_mode_is_drawn_without_being_judged_again() {
        // Setup
        GenerateSlugsUseCase useCase = new(
            new FakeThemeDirectory(new EmbeddedThemeCatalog()),
            new FakeConfigStore(),
            new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(new SluggerOptions { Themes = ["heroku"] });

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.Single(outcome.GetResultOrThrow());
    }


    [Fact]
    public void The_wildcard_puts_every_theme_the_catalog_serves_in_scope() {
        // Setup - the broken one is what proves the wildcard reached it: a run that never loaded
        // it would succeed, and this one is refused by it.
        FakeThemeCatalog catalog = new(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName), ThemeNamed("heroku"));
        catalog.Broken.Add("docker");
        GenerateSlugsUseCase useCase = new(new FakeThemeDirectory(catalog), new FakeConfigStore(), new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome =
            useCase.Execute(new SluggerOptions { Themes = [GenerateSlugsUseCase.EveryThemeName] });

        // Verify
        Assert.True(outcome.IsFailure);
        Assert.Equal(ThemeErrors.Codes.Rejected, outcome.Error!.Code);
    }

    [Fact]
    public void The_wildcard_reaches_a_theme_the_default_name_never_would() {
        // Setup - no theme is called "slugger" here, so falling back to the default would refuse.
        FakeThemeCatalog     catalog = new(ThemeNamed("heroku"));
        GenerateSlugsUseCase useCase = new(new FakeThemeDirectory(catalog), new FakeConfigStore(), new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome =
            useCase.Execute(new SluggerOptions { Themes = [GenerateSlugsUseCase.EveryThemeName] });

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.Single(outcome.GetResultOrThrow());
    }

    [Fact]
    public void A_theme_named_beside_the_wildcard_is_put_in_scope_once() {
        // Setup - a theme listed twice would be weighed twice by WeightedThemePicker.
        FakeThemeCatalog catalog = new(ThemeNamed("docker"), ThemeNamed("heroku"));

        // Exercise
        string[] scope = GenerateSlugsUseCase.Expand(["heroku", GenerateSlugsUseCase.EveryThemeName], catalog);

        // Verify
        Assert.Equal(["heroku", "docker"], scope);
    }

    [Fact]
    public void Without_the_wildcard_the_names_are_left_exactly_as_asked() {
        // Setup
        FakeThemeCatalog catalog = new(ThemeNamed("docker"), ThemeNamed("heroku"));

        // Exercise
        string[] scope = GenerateSlugsUseCase.Expand(["heroku", "heroku"], catalog);

        // Verify
        Assert.Equal(["heroku", "heroku"], scope);
    }

}
