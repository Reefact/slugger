#region Usings declarations

using Slugger.Application.Options;
using Slugger.Application.UseCases;

#endregion

namespace Slugger.UnitTests;

public sealed class ListThemesUseCaseTests {

    [Fact]
    public void Lists_what_the_catalog_carries() {
        // Setup
        ListThemesUseCase useCase = new(
            new FakeThemeDirectory(new FakeThemeCatalog(
                                       GenerateSlugsUseCaseTests.ThemeNamed("docker"),
                                       GenerateSlugsUseCaseTests.ThemeNamed("spices"))),
            new FakeConfigStore());

        // Exercise
        IReadOnlyList<string> names = useCase.Execute(SluggerOptions.Empty);

        // Verify
        Assert.Equal(["docker", "spices"], names);
    }

}