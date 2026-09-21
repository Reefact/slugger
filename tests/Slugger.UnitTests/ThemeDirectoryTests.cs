using Slugger.Application.Options;
using Slugger.Application.UseCases;

namespace Slugger.UnitTests;

/// <summary>
/// <c>--theme-dir</c> takes part in the precedence chain like any other option, which means a
/// use case cannot be handed a catalog built before the config was read. These pin that it is
/// resolved per call rather than baked in at composition time.
/// </summary>
public sealed class ThemeDirectoryResolutionTests
{
    [Fact]
    public void An_explicit_theme_dir_reaches_the_catalog()
    {
        // Setup
        FakeThemeDirectory directories = new(new FakeThemeCatalog(GenerateSlugsUseCaseTests.ThemeNamed("porno")));
        ListThemesUseCase useCase = new(directories, new FakeConfigStore());

        // Exercise
        useCase.Execute(new SluggerOptions { ThemeDirectory = "/elsewhere" });

        // Verify
        Assert.Equal(["/elsewhere"], directories.Asked);
    }

    /// <summary>
    /// The case that was impossible before: --init saves a theme directory, and a later run with
    /// no --theme-dir has to honour it. A catalog built at composition time never could.
    /// </summary>
    [Fact]
    public void A_theme_dir_saved_by_init_is_honoured_on_a_later_run()
    {
        // Setup
        FakeThemeDirectory directories = new();
        FakeConfigStore config = new(new SluggerOptions { ThemeDirectory = "/saved" });
        ListThemesUseCase useCase = new(directories, config);

        // Exercise
        useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.Equal(["/saved"], directories.Asked);
    }

    [Fact]
    public void An_explicit_theme_dir_beats_the_saved_one()
    {
        // Setup
        FakeThemeDirectory directories = new();
        FakeConfigStore config = new(new SluggerOptions { ThemeDirectory = "/saved" });
        ListThemesUseCase useCase = new(directories, config);

        // Exercise
        useCase.Execute(new SluggerOptions { ThemeDirectory = "/explicit" });

        // Verify
        Assert.Equal(["/explicit"], directories.Asked);
    }

    [Fact]
    public void Generating_reads_the_theme_dir_too()
    {
        // Setup
        FakeThemeDirectory directories = new(new FakeThemeCatalog(
            GenerateSlugsUseCaseTests.ThemeNamed(GenerateSlugsUseCase.DefaultThemeName)));
        GenerateSlugsUseCase useCase = new(directories, new FakeConfigStore(), new FakeClipboard());

        // Exercise
        useCase.Execute(new SluggerOptions { ThemeDirectory = "/elsewhere" });

        // Verify
        Assert.Contains("/elsewhere", directories.Asked);
    }

    [Fact]
    public void Registering_writes_into_the_theme_dir_it_was_given()
    {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/porno.json"] = GenerateSlugsUseCaseTests.ThemeNamed("porno");
        FakeThemeDirectory directories = new(store: store);
        RegisterThemeUseCase useCase = new(directories, new FakeConfigStore());

        // Exercise
        useCase.Execute("/tmp/porno.json", new SluggerOptions { ThemeDirectory = "/elsewhere" });

        // Verify
        Assert.Contains("/elsewhere", directories.Asked);
    }

    [Fact]
    public void Unregistering_reads_the_theme_dir_too()
    {
        // Setup
        FakeThemeDirectory directories = new();
        UnregisterThemeUseCase useCase = new(directories, new FakeConfigStore());

        // Exercise
        useCase.Execute("porno", new SluggerOptions { ThemeDirectory = "/elsewhere" });

        // Verify
        Assert.Contains("/elsewhere", directories.Asked);
    }

    [Fact]
    public void Theme_info_reads_the_theme_dir_too()
    {
        // Setup
        FakeThemeDirectory directories = new(new FakeThemeCatalog(GenerateSlugsUseCaseTests.ThemeNamed("porno")));
        ThemeInfoUseCase useCase = new(directories, new FakeConfigStore());

        // Exercise
        useCase.Execute("porno", new SluggerOptions { ThemeDirectory = "/elsewhere" });

        // Verify
        Assert.Equal(["/elsewhere"], directories.Asked);
    }
}
