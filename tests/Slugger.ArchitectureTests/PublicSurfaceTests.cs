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
            "Slugger.Casing",
            "Slugger.DefaultRandomSource",
            "Slugger.Errors.DiagnosableException",
            "Slugger.Errors.Error",
            "Slugger.Errors.ErrorCode",
            "Slugger.Errors.ErrorContext",
            "Slugger.Errors.ErrorContextKey`1",
            "Slugger.Errors.Outcome`1",
            "Slugger.GenerationOptions",
            "Slugger.IRandomSource",
            "Slugger.MaxLength",
            "Slugger.SegmentMode",
            "Slugger.SlugGenerator",
            "Slugger.SluggerTheme",
            "Slugger.SluggerThemeError",
            "Slugger.SluggerThemeError+Codes",
            "Slugger.SluggerThemeException",
            "Slugger.ThemeDefaults",
            "Slugger.ThemeFloors",
            "Slugger.ThemeMetadata"
        ];

        // Exercise
        string[] published = typeof(SluggerTheme).Assembly
                                           .GetExportedTypes()
                                           .Select(type => type.FullName!)
                                           .Order(StringComparer.Ordinal)
                                           .ToArray();

        // Verify
        Assert.Equal(promised, published);
    }

}
