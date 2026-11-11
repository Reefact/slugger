#region Usings declarations

using System.Reflection;

using Slugger.Domain;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What a consumer holds of a theme (DEC0034): what the theme states about itself, never the words
///     it draws from, and nothing it could have built itself.
/// </summary>
public sealed class SluggerThemeTests {

    [Fact]
    public void Only_the_library_builds_one() {
        // Exercise
        ConstructorInfo[] constructors = typeof(SluggerTheme).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Verify
        Assert.Empty(constructors);
    }

    /// <summary>
    ///     Literal on purpose: the list is the promise. A member added to it is a decision to publish
    ///     something about a theme, and belongs in a decision record before it belongs here.
    /// </summary>
    [Fact]
    public void Says_what_the_theme_states_about_itself_and_nothing_more() {
        // Exercise
        string[] published = [
            .. typeof(SluggerTheme).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                   .Select(property => property.Name)
                                   .Order(StringComparer.Ordinal)
        ];

        // Verify
        Assert.Equal(["Defaults", "MaxLength", "Metadata", "Name"], published);
    }

    [Fact]
    public void Hands_out_what_the_file_states() {
        // Setup
        const string Json = """
                            {
                              "meta": { "title": "Moons" },
                              "defaults": { "sep": "_" },
                              "adjectives": { "common": ["keen"] },
                              "nouns": [{ "value": "moon" }]
                            }
                            """;
        string name = Dummies.AnyThemeNameOtherThanTheBuiltInOnes();

        // Exercise
        SluggerTheme theme = SluggerTheme.LoadFromJson(Json, name, true).GetResultOrThrow();

        // Verify
        Assert.Equal(name, theme.Name);
        Assert.Equal("Moons", theme.Metadata.Title);
        Assert.Equal("_", theme.Defaults.Separator);
    }

    [Fact]
    public void Counts_fewer_combinations_under_a_length_limit_than_without_one() {
        // Setup
        SluggerTheme      docker  = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions style   = GenerationOptions.Default.WithDefaultsOf(docker);
        GenerationOptions limited = style with { MaxLength = 20 };

        // Exercise
        long whole   = new ThemeCombinatorics(docker, style).Total(style.SegmentMode);
        long reduced = new ThemeCombinatorics(docker, limited).Total(limited.SegmentMode);

        // Verify
        Assert.InRange(reduced, 1, whole - 1);
    }

    [Fact]
    public void Counts_without_options_what_the_theme_draws_in_its_own_style() {
        // Setup
        SluggerTheme      docker = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions style  = GenerationOptions.Default.WithDefaultsOf(docker);

        // Exercise
        long declared = new ThemeCombinatorics(docker).Total(style.SegmentMode);
        long styled   = new ThemeCombinatorics(docker, style).Total(style.SegmentMode);

        // Verify
        Assert.Equal(declared, styled);
    }

}
