#region Usings declarations

using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

public sealed class RegisterThemeUseCaseTests {

    [Fact]
    public void Copies_a_valid_theme_into_the_directory() {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/spices.json"] = GenerateSlugsUseCaseTests.ThemeNamed("spices");
        RegisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/spices.json", SluggerOptions.Empty);

        // Verify
        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal("spices", result.Name);
        Assert.True(store.Saved.ContainsKey("spices"));
    }

    [Fact]
    public void Refuses_rather_than_overwrite_a_theme_of_the_same_name() {
        // Setup
        FakeThemeStore store = new();
        store.Save("spices", "{}");
        store.Files["/tmp/spices.json"] = GenerateSlugsUseCaseTests.ThemeNamed("spices");
        RegisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/spices.json", SluggerOptions.Empty);

        // Verify
        Assert.Equal(ThemeErrors.Codes.AlreadyRegistered, result.Outcome.Error!.Code);
        Assert.Equal("{}", store.Saved["spices"]);
    }

    /// <summary>
    ///     --theme splits its value on commas, so "a,b" would be asked for as "a" and "b" and never
    ///     as itself: registered, it would sit in the directory out of reach.
    /// </summary>
    [Fact]
    public void Refuses_a_name_holding_a_comma_and_copies_nothing() {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/a,b.json"] = GenerateSlugsUseCaseTests.ThemeNamed("a,b");
        RegisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/a,b.json", SluggerOptions.Empty);

        // Verify
        Assert.Equal(ThemeErrors.Codes.NotSelectable, result.Outcome.Error!.Code);
        Assert.Equal(
            "Theme \"a,b\" cannot be registered: --theme splits its value on commas, so no --theme could ever select it. Rename the file.",
            result.Outcome.Error.DiagnosticMessage);
        Assert.Equal("That theme name cannot be selected.", result.Outcome.Error.ShortMessage);
        Assert.Empty(store.Saved);
    }

    /// <summary>"*" is every theme to --theme, so a theme of that name could never be asked for alone.</summary>
    [Fact]
    public void Refuses_a_name_that_is_the_wildcard() {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/*.json"] = GenerateSlugsUseCaseTests.ThemeNamed("*");
        RegisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/*.json", SluggerOptions.Empty);

        // Verify
        Assert.Equal(
            "Theme \"*\" cannot be registered: --theme reads \"*\" as every theme, so no --theme could ever select it. Rename the file.",
            result.Outcome.Error!.DiagnosticMessage);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public void Refuses_an_invalid_file_and_copies_nothing() {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/broken.json"] = null;
        RegisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/broken.json", SluggerOptions.Empty);

        // Verify
        Assert.True(result.Outcome.IsFailure);
        Assert.Empty(store.Saved);
    }

    /// <summary>Shadowing a built-in theme is allowed, but never silently - the caller is told.</summary>
    [Fact]
    public void Reports_that_the_name_now_shadows_a_built_in_theme() {
        // Setup
        FakeThemeStore store = new();
        store.Files["/tmp/docker.json"] = GenerateSlugsUseCaseTests.ThemeNamed("docker");
        RegisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store) { Embedded = new FakeThemeCatalog(GenerateSlugsUseCaseTests.ThemeNamed("docker")) }, new FakeConfigStore());

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/docker.json", SluggerOptions.Empty);

        // Verify
        Assert.True(result.Outcome.IsSuccess);
        Assert.True(result.Shadows);
    }

}