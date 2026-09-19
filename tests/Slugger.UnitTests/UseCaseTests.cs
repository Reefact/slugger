using FirstClassErrors;
using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Domain;
using Slugger.Domain.Validation;

namespace Slugger.UnitTests;

public sealed class GenerateSlugsUseCaseTests
{
    [Fact]
    public void Draws_from_the_default_theme_when_none_is_named()
    {
        // Setup
        FakeThemeCatalog catalog = new(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName));
        GenerateSlugsUseCase useCase = new(catalog, new FakeConfigStore(), new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.Single(outcome.GetResultOrThrow());
    }

    [Fact]
    public void Draws_as_many_slugs_as_count_asks_for()
    {
        // Setup
        int count = Any.Int32().Between(2, 12).Generate();
        GenerateSlugsUseCase useCase = new(
            new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName)),
            new FakeConfigStore(),
            new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(new SluggerOptions { Count = count });

        // Verify
        Assert.Equal(count, outcome.GetResultOrThrow().Count);
    }

    /// <summary>
    /// One random source drives the whole batch, so a seeded run replays every slug of it -
    /// not just the first, which a fresh source per draw would give.
    /// </summary>
    [Fact]
    public void The_same_seed_replays_the_whole_batch()
    {
        // Setup
        GenerateSlugsUseCase useCase = new(
            new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName)),
            new FakeConfigStore(),
            new FakeClipboard());
        SluggerOptions options = new() { Count = 6, Seed = Any.Int32().Between(1, 100_000).Generate() };

        // Exercise
        IReadOnlyList<string> first = useCase.Execute(options).GetResultOrThrow();
        IReadOnlyList<string> second = useCase.Execute(options).GetResultOrThrow();

        // Verify
        Assert.Equal(first, second);
    }

    [Fact]
    public void Copies_nothing_unless_the_clipboard_was_asked_for()
    {
        // Setup
        FakeClipboard clipboard = new();
        GenerateSlugsUseCase useCase = new(
            new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName)),
            new FakeConfigStore(),
            clipboard);

        // Exercise
        useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.Equal(0, clipboard.Copies);
    }

    [Fact]
    public void Copies_the_last_slug_of_a_round_when_asked()
    {
        // Setup
        FakeClipboard clipboard = new();
        GenerateSlugsUseCase useCase = new(
            new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName)),
            new FakeConfigStore(),
            clipboard);

        // Exercise
        IReadOnlyList<string> slugs = useCase.Execute(new SluggerOptions { Count = 3, Clipboard = true }).GetResultOrThrow();

        // Verify
        Assert.Equal(slugs[^1], clipboard.LastCopied);
    }

    /// <summary>
    /// Generating from the themes that did load would hide the broken one, and the author would
    /// never learn their file is wrong.
    /// </summary>
    [Fact]
    public void One_refused_theme_fails_the_whole_batch()
    {
        // Setup
        FakeThemeCatalog catalog = new(ThemeNamed("good"));
        catalog.Broken.Add("bad");
        GenerateSlugsUseCase useCase = new(catalog, new FakeConfigStore(), new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(new SluggerOptions { Themes = ["good", "bad"] });

        // Verify
        Assert.True(outcome.IsFailure);
        Assert.Equal(ThemeErrors.Codes.Rejected, outcome.Error!.Code);
    }

    [Fact]
    public void A_saved_config_supplies_what_the_command_line_leaves_out()
    {
        // Setup
        FakeConfigStore config = new(new SluggerOptions { Count = 4 });
        GenerateSlugsUseCase useCase = new(
            new FakeThemeCatalog(ThemeNamed(GenerateSlugsUseCase.DefaultThemeName)),
            config,
            new FakeClipboard());

        // Exercise
        Outcome<IReadOnlyList<string>> outcome = useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.Equal(4, outcome.GetResultOrThrow().Count);
    }

    internal static Theme ThemeNamed(string name) =>
        new(name,
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen", "gorgeous"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", []), new Noun("river", [])]);
}

public sealed class RegisterThemeUseCaseTests
{
    [Fact]
    public void Copies_a_valid_theme_into_the_directory()
    {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/porno.json"] = GenerateSlugsUseCaseTests.ThemeNamed("porno");
        RegisterThemeUseCase useCase = new(new FakeThemeCatalog(), store);

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/porno.json", SluggerOptions.Empty);

        // Verify
        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal("porno", result.Name);
        Assert.True(store.Saved.ContainsKey("porno"));
    }

    [Fact]
    public void Refuses_rather_than_overwrite_a_theme_of_the_same_name()
    {
        // Setup
        FakeThemeStore store = new();
        store.Save("porno", "{}");
        store.Files["/tmp/porno.json"] = GenerateSlugsUseCaseTests.ThemeNamed("porno");
        RegisterThemeUseCase useCase = new(new FakeThemeCatalog(), store);

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/porno.json", SluggerOptions.Empty);

        // Verify
        Assert.Equal(ThemeErrors.Codes.AlreadyRegistered, result.Outcome.Error!.Code);
        Assert.Equal("{}", store.Saved["porno"]);
    }

    [Fact]
    public void Refuses_an_invalid_file_and_copies_nothing()
    {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/broken.json"] = null;
        RegisterThemeUseCase useCase = new(new FakeThemeCatalog(), store);

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/broken.json", SluggerOptions.Empty);

        // Verify
        Assert.True(result.Outcome.IsFailure);
        Assert.Empty(store.Saved);
    }

    /// <summary>Shadowing a built-in theme is allowed, but never silently - the caller is told.</summary>
    [Fact]
    public void Reports_that_the_name_now_shadows_a_built_in_theme()
    {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/docker.json"] = GenerateSlugsUseCaseTests.ThemeNamed("docker");
        RegisterThemeUseCase useCase = new(new FakeThemeCatalog(GenerateSlugsUseCaseTests.ThemeNamed("docker")), store);

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/docker.json", SluggerOptions.Empty);

        // Verify
        Assert.True(result.Outcome.IsSuccess);
        Assert.True(result.Shadows);
    }
}

public sealed class UnregisterThemeUseCaseTests
{
    [Fact]
    public void Deletes_a_custom_theme()
    {
        // Setup
        FakeThemeStore store = new();
        store.Save("porno", "{}");
        UnregisterThemeUseCase useCase = new(new FakeThemeCatalog(), store);

        // Exercise
        Outcome outcome = useCase.Execute("porno", SluggerOptions.Empty);

        // Verify
        Assert.True(outcome.IsSuccess);
        Assert.Empty(store.Saved);
    }

    /// <summary>One cannot unregister what is not a file - the message says so rather than "not found".</summary>
    [Fact]
    public void Refuses_a_built_in_theme_with_its_own_message()
    {
        // Setup
        UnregisterThemeUseCase useCase = new(new FakeThemeCatalog(GenerateSlugsUseCaseTests.ThemeNamed("docker")), new FakeThemeStore());

        // Exercise
        Outcome outcome = useCase.Execute("docker", SluggerOptions.Empty);

        // Verify
        Assert.Equal(ThemeErrors.Codes.NotAFile, outcome.Error!.Code);
    }

    [Fact]
    public void Refuses_a_name_nobody_carries()
    {
        // Setup
        UnregisterThemeUseCase useCase = new(new FakeThemeCatalog(), new FakeThemeStore());

        // Exercise
        Outcome outcome = useCase.Execute(Dummies.AnyThemeNameOtherThanTheBuiltInOnes(), SluggerOptions.Empty);

        // Verify
        Assert.Equal(ThemeErrors.Codes.NotFound, outcome.Error!.Code);
    }
}

public sealed class SaveDefaultsUseCaseTests
{
    [Fact]
    public void Persists_what_the_command_line_carried()
    {
        // Setup
        FakeConfigStore config = new();
        SaveDefaultsUseCase useCase = new(config);

        // Exercise
        useCase.Execute(new SluggerOptions { Count = 5, Oneshot = true });

        // Verify
        Assert.Equal(5, config.Stored!.Count);
        Assert.True(config.Stored.Oneshot);
    }

    /// <summary>A second --init adds to the config rather than wiping what it says nothing about.</summary>
    [Fact]
    public void Lays_over_what_was_already_saved()
    {
        // Setup
        FakeConfigStore config = new(new SluggerOptions { Seed = 42, Count = 2 });
        SaveDefaultsUseCase useCase = new(config);

        // Exercise
        useCase.Execute(new SluggerOptions { Count = 7 });

        // Verify
        Assert.Equal(7, config.Stored!.Count);
        Assert.Equal(42, config.Stored.Seed);
    }
}

public sealed class ListThemesUseCaseTests
{
    [Fact]
    public void Lists_what_the_catalog_carries()
    {
        // Setup
        ListThemesUseCase useCase = new(new FakeThemeCatalog(
            GenerateSlugsUseCaseTests.ThemeNamed("docker"),
            GenerateSlugsUseCaseTests.ThemeNamed("porno")));

        // Exercise
        IReadOnlyList<string> names = useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.Equal(["docker", "porno"], names);
    }
}
