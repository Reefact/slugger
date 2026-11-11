namespace Slugger.ArchitectureTests;

/// <summary>
///     What the package promises, written down. A stable version freezes every public type, so a type
///     published by accident is a promise made by accident; this list makes publishing one a visible
///     edit, the way <see cref="NamespaceDependencyTests" /> makes taking on a dependency one.
/// </summary>
/// <remarks>
///     The vocabulary of the ongoing refactoring - <c>Theme</c>, <c>Word</c>, <c>Term</c>, <c>Slug</c>
///     and the rest, with their errors and exceptions - is not on it: those types stay internal until
///     loading and generation use them, so that they can still change without breaking anybody.
/// </remarks>
public sealed class PublicSurfaceTests {

    [Fact]
    public void The_engine_publishes_exactly_the_types_it_promises() {
        // Setup
        string[] promised = [
            "Slugger.Domain.Casing",
            "Slugger.Domain.DefaultRandomSource",
            "Slugger.Domain.Errors.DiagnosableException",
            "Slugger.Domain.Errors.DomainError",
            "Slugger.Domain.Errors.DomainException",
            "Slugger.Domain.Errors.Error",
            "Slugger.Domain.Errors.ErrorCode",
            "Slugger.Domain.Errors.ErrorContext",
            "Slugger.Domain.Errors.ErrorContextKey`1",
            "Slugger.Domain.Errors.Outcome`1",
            "Slugger.Domain.Generation.SlugBudget",
            "Slugger.Domain.Generation.SlugFormatter",
            "Slugger.Domain.Generation.SlugGenerator",
            "Slugger.Domain.Generation.SlugSource",
            "Slugger.Domain.GenerationOptions",
            "Slugger.Domain.IRandomSource",
            "Slugger.Domain.MaxLength",
            "Slugger.Domain.Normalization.WordNormalizer",
            "Slugger.Domain.NounEntry",
            "Slugger.Domain.Resolution.ThemeResolver",
            "Slugger.Domain.Resolution.WeightedThemePicker",
            "Slugger.Domain.SegmentMode",
            "Slugger.Domain.ThemeDefaults",
            "Slugger.Domain.ThemeDocument",
            "Slugger.Domain.ThemeMetadata",
            "Slugger.Domain.Validation.ThemeCombinatorics",
            "Slugger.Domain.Validation.ThemeErrors",
            "Slugger.Domain.Validation.ThemeErrors+Codes",
            "Slugger.Domain.Validation.ThemeValidator",
            "Slugger.Themes"
        ];

        // Exercise
        string[] published = typeof(Themes).Assembly
                                           .GetExportedTypes()
                                           .Select(type => type.FullName!)
                                           .Order(StringComparer.Ordinal)
                                           .ToArray();

        // Verify
        Assert.Equal(promised, published);
    }

}
