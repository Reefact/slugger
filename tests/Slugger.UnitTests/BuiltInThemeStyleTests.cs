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
        return GenerationOptions.Default.WithDefaultsOf(SluggerTheme.LoadEmbeddedOrThrow(name));
    }

    private static string[] Generate(string name, GenerationOptions options, int count) {
        SluggerTheme                theme  = SluggerTheme.LoadEmbeddedOrThrow(name);
        DefaultRandomSource random = new(20260919);

        return [.. Enumerable.Range(0, count).Select(_ => SlugDrawer.Generate(theme, options, random))];
    }

    #endregion

    /// <summary>One word before the noun, snake_case, and a single decimal digit glued on 1% of draws.</summary>
    [Fact]
    public void Docker_reads_like_docker() {
        // Setup
        GenerationOptions options = StyleOf("docker");

        // Verify the style the theme declares, then the slugs it produces - no separator of its own:
        // snake joins with "_" (DEC0030), and a --casing on the command line can still change it.
        Assert.Null(options.Separator);
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

        // Verify - kebab joins with "-" on its own (DEC0030).
        Assert.Null(options.Separator);
        Assert.Equal(Casing.Kebab, options.Casing);
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

    [Fact]
    public void A_theme_stating_no_opinion_changes_nothing() {
        // Setup
        ThemeDocument silent = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>>(),
            new Dictionary<string, IReadOnlyList<string>>(),
            [new NounEntry("moon", [])]);

        // Exercise
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(silent.HandedOut());

        // Verify
        Assert.Equal(GenerationOptions.Default, options);
    }

    /// <summary>
    ///     Issue #47: a casing asked for over docker's style used to keep docker's "_", because the
    ///     theme declared its separator apart from its casing. It now declares the casing alone, and the
    ///     casing brings its own joiner (DEC0030).
    /// </summary>
    [Fact]
    public void Kebab_over_dockers_style_joins_with_a_dash() {
        // Setup
        GenerationOptions options = StyleOf("docker") with { Casing = Casing.Kebab };

        // Exercise
        string[] slugs = Generate("docker", options, 50);

        // Verify
        Assert.All(slugs, slug => Assert.Matches(@"^\p{Ll}+-\p{Ll}+[0-9]?$", slug));
    }

    /// <summary>
    ///     Issue #49: the command offers --segment participle, and every shipped theme used to refuse it.
    ///     Docker declares the hundred participles per noun the mode holds a theme to (DEC0016), so it
    ///     is the working example - and every slug it draws there opens on one of them.
    /// </summary>
    [Fact]
    public void Docker_draws_one_participle_before_the_noun_when_asked_to() {
        // Setup
        SluggerTheme      docker      = SluggerTheme.LoadEmbeddedOrThrow("docker");
        GenerationOptions options     = StyleOf("docker") with { SegmentMode = SegmentMode.Participle, TokenLength = 0 };
        HashSet<string>   participles = [.. docker.Content.Participles.Values.SelectMany(words => words)];

        // Exercise
        SlugGenerator source = SlugGenerator.PrepareOrThrow(docker, options);
        string[]   slugs  = [.. Enumerable.Range(0, 200).Select(_ => source.Next())];

        // Verify
        Assert.All(slugs, slug => Assert.Contains(slug.Split('_')[0], participles));
    }

}
