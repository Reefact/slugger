#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Domain;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

public sealed class ThemeInfoUseCaseTests {

    [Fact]
    public void Hands_back_the_theme_its_meta_block_belongs_to() {
        // Setup
        Theme theme = new(
            "cuisine",
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen", "gorgeous"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new NounOld("moon", []), new NounOld("river", [])]) {
            Metadata = new ThemeMetadata { Title = "Cuisine", Author = "Sylvain" }
        };
        ThemeInfoUseCase useCase = new(new FakeThemeDirectory(new FakeThemeCatalog(theme)), new FakeConfigStore());

        // Exercise
        Outcome<Theme> outcome = useCase.Execute("cuisine", SluggerOptions.Empty);

        // Verify
        Assert.True(outcome.IsSuccess);
        Assert.Equal("Cuisine", outcome.GetResultOrThrow().Metadata.Title);
    }

    [Fact]
    public void Refuses_a_name_nobody_carries() {
        // Setup
        ThemeInfoUseCase useCase = new(new FakeThemeDirectory(), new FakeConfigStore());

        // Exercise
        Outcome<Theme> outcome = useCase.Execute(Dummies.AnyThemeNameOtherThanTheBuiltInOnes(), SluggerOptions.Empty);

        // Verify - Rejected wraps the reason, the way a catalog reports any refusal to load.
        Assert.Equal(ThemeErrors.Codes.Rejected, outcome.Error!.Code);
        Assert.Equal(ThemeErrors.Codes.NotFound, Assert.Single(outcome.Error.InnerErrors).Code);
    }

}