#region Usings declarations

using System.Text.RegularExpressions;

using Slugger.Domain;
using Slugger.Domain.Generation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     The three built-in themes, generated from end to end with their own defaults applied, against
///     the shape each one's own defaults describe. Everything below this runs on the real files rather
///     than a fixture, so a theme edited into a different style fails here.
/// </summary>
public sealed class BuiltInThemeStyleTests {

    #region Static members

    private static GenerationOptions StyleOf(string name) {
        return GenerationOptions.Default.WithDefaultsOf(Themes.LoadEmbedded(name));
    }

    private static string[] Generate(string name, GenerationOptions options, int count) {
        Theme               theme  = Themes.LoadEmbedded(name);
        DefaultRandomSource random = new(20260919);

        return [.. Enumerable.Range(0, count).Select(_ => SlugGenerator.Generate(theme, options, random))];
    }

    #endregion

    /// <summary>One word before the noun, snake_case, and a single decimal digit glued on 1% of draws.</summary>
    [Fact]
    public void Docker_reads_like_docker() {
        // Setup
        GenerationOptions options = StyleOf("docker");

        // Verify the style the theme declares, then the slugs it produces.
        Assert.Equal('_', options.Separator);
        Assert.Equal(Casing.Snake, options.Casing);
        Assert.Equal(SegmentMode.Either, options.SegmentMode);
        Assert.Equal(1, options.TokenLength);
        Assert.True(options.TokenGlued);
        Assert.Equal(1, options.TokenChance);

        // Exercise
        string[] slugs = Generate("docker", options, 200);

        // Verify
        Assert.All(slugs, slug => Assert.Matches(@"^\p{Ll}+_\p{Ll}+[0-9]?$", slug));
    }

    /// <summary>One word before the noun, kebab-case, and a four digit suffix every time.</summary>
    [Fact]
    public void Heroku_reads_like_haikunator() {
        // Setup
        GenerationOptions options = StyleOf("heroku");

        // Verify
        Assert.Equal('-', options.Separator);
        Assert.Equal(SegmentMode.Either, options.SegmentMode);
        Assert.Equal(4, options.TokenLength);
        Assert.Equal(100, options.TokenChance);

        // Exercise
        string[] slugs = Generate("heroku", options, 200);

        // Verify
        Assert.All(slugs, slug => Assert.Matches(@"^\p{Ll}+-\p{Ll}+-[0-9]{4}$", slug));
    }

    /// <summary>
    ///     Three segments and no token. slugger declares no defaults of its own - the program's
    ///     format already is that, and a theme that restated it would have silenced a saved config
    ///     for nothing - so what this pins is that the file still leaves the format alone. Its nouns
    ///     are ballplayers, half of them two words, so the noun contributes more than one
    ///     hyphen-separated part.
    /// </summary>
    [Fact]
    public void Slugger_reads_as_three_segments_with_no_suffix() {
        // Setup
        GenerationOptions options = StyleOf("slugger");

        // Verify
        Assert.Equal(SegmentMode.Both, options.SegmentMode);
        Assert.Equal(0, options.TokenLength);

        // Exercise
        string[] slugs = Generate("slugger", options, 200);

        // Verify
        Assert.All(slugs, slug => Assert.Matches(@"^\p{Ll}+-\p{Ll}+-\p{Ll}+(-\p{Ll}+)*$", slug));
        Assert.DoesNotContain(slugs, slug => Regex.IsMatch(slug, "[0-9]"));
    }

    /// <summary>
    ///     The one lever a theme's defaults must not touch, because it is a session preference and
    ///     not part of any style: a seed asked for by the caller survives applying them.
    /// </summary>
    [Fact]
    public void Applying_a_themes_defaults_leaves_the_callers_seed_alone() {
        // Setup
        int seed = Any.Int32().Between(1, 100_000).Generate();

        // Exercise
        GenerationOptions options = new GenerationOptions { Seed = seed }.WithDefaultsOf(Themes.LoadEmbedded("docker"));

        // Verify
        Assert.Equal(seed, options.Seed);
    }

    [Fact]
    public void A_theme_stating_no_opinion_changes_nothing() {
        // Setup
        Theme silent = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>>(),
            new Dictionary<string, IReadOnlyList<string>>(),
            [new NounEntry("moon", [])]);

        // Exercise
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(silent);

        // Verify
        Assert.Equal(GenerationOptions.Default, options);
    }

}