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
        store.Files["/tmp/porno.json"] = GenerateSlugsUseCaseTests.ThemeNamed("porno");
        RegisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/porno.json", SluggerOptions.Empty);

        // Verify
        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal("porno", result.Name);
        Assert.True(store.Saved.ContainsKey("porno"));
    }

    [Fact]
    public void Refuses_rather_than_overwrite_a_theme_of_the_same_name() {
        // Setup
        FakeThemeStore store = new();
        store.Save("porno", "{}");
        store.Files["/tmp/porno.json"] = GenerateSlugsUseCaseTests.ThemeNamed("porno");
        RegisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        RegisterThemeResult result = useCase.Execute("/tmp/porno.json", SluggerOptions.Empty);

        // Verify
        Assert.Equal(ThemeErrors.Codes.AlreadyRegistered, result.Outcome.Error!.Code);
        Assert.Equal("{}", store.Saved["porno"]);
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