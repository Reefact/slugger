#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

public sealed class UnregisterThemeUseCaseTests {

    [Fact]
    public void Deletes_a_custom_theme() {
        // Setup
        FakeThemeStore store = new();
        store.Save("spices", "{}");
        UnregisterThemeUseCase useCase = new(new FakeThemeDirectory(store: store), new FakeConfigStore());

        // Exercise
        Outcome outcome = useCase.Execute("spices", SluggerOptions.Empty);

        // Verify
        Assert.True(outcome.IsSuccess);
        Assert.Empty(store.Saved);
    }

    /// <summary>One cannot unregister what is not a file - the message says so rather than "not found".</summary>
    [Fact]
    public void Refuses_a_built_in_theme_with_its_own_message() {
        // Setup
        UnregisterThemeUseCase useCase = new(new FakeThemeDirectory { Embedded = new FakeThemeCatalog(GenerateSlugsUseCaseTests.ThemeNamed("docker")) }, new FakeConfigStore());

        // Exercise
        Outcome outcome = useCase.Execute("docker", SluggerOptions.Empty);

        // Verify
        Assert.Equal(ThemeErrors.Codes.NotAFile, outcome.Error!.Code);
    }

    [Fact]
    public void Refuses_a_name_nobody_carries() {
        // Setup
        string                 name    = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();
        UnregisterThemeUseCase useCase = new(new FakeThemeDirectory(), new FakeConfigStore());

        // Exercise
        Outcome outcome = useCase.Execute(name, SluggerOptions.Empty);

        // Verify
        Assert.Equal(ThemeErrors.Codes.NotFound, outcome.Error!.Code);
        Assert.Equal($"Theme \"{name}\" could not be found, and no theme is available at all.", outcome.Error.DiagnosticMessage);
    }

}