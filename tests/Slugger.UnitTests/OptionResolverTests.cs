using Slugger.Application.Options;
using Slugger.Domain;

namespace Slugger.UnitTests;

/// <summary>
/// The chain of ADR 0004: explicit argument, then the theme's defaults, then the config
/// saved by --init, then the program's own default. Each test knocks out one layer to show the
/// next one speaking.
/// </summary>
public sealed class OptionResolverTests
{
    private static readonly Theme Styled = ThemeWithDefaults(new ThemeDefaults { Separator = '_', TokenLength = 1 });

    [Fact]
    public void Falls_back_to_the_programs_own_default_when_nobody_says_anything()
    {
        // Exercise
        GenerationOptions options = OptionResolver.Resolve(SluggerOptions.Empty, saved: null, ThemeWithDefaults(ThemeDefaults.Empty), themesInScope: 1);

        // Verify
        Assert.Equal(GenerationOptions.Default, options);
    }

    [Fact]
    public void The_saved_config_beats_the_programs_default()
    {
        // Setup
        SluggerOptions saved = new() { Separator = '+' };

        // Exercise
        GenerationOptions options = OptionResolver.Resolve(SluggerOptions.Empty, saved, ThemeWithDefaults(ThemeDefaults.Empty), themesInScope: 1);

        // Verify
        Assert.Equal('+', options.Separator);
    }

    [Fact]
    public void The_themes_defaults_beat_the_saved_config()
    {
        // Setup
        SluggerOptions saved = new() { Separator = '+' };

        // Exercise
        GenerationOptions options = OptionResolver.Resolve(SluggerOptions.Empty, saved, Styled, themesInScope: 1);

        // Verify
        Assert.Equal('_', options.Separator);
    }

    [Fact]
    public void An_explicit_argument_beats_everything()
    {
        // Setup
        SluggerOptions commandLine = new() { Separator = '=' };
        SluggerOptions saved = new() { Separator = '+' };

        // Exercise
        GenerationOptions options = OptionResolver.Resolve(commandLine, saved, Styled, themesInScope: 1);

        // Verify
        Assert.Equal('=', options.Separator);
    }

    /// <summary>
    /// The word separator rides the same chain, and its empty value is where that is worth
    /// checking: "" is a theme asking for glued words, not a theme saying nothing, so a layer
    /// that treated it as absent would hand back the config's separator instead.
    /// </summary>
    [Fact]
    public void An_empty_word_separator_is_an_answer_and_outranks_the_layer_below()
    {
        // Setup - the theme glues its compound values, the saved config would not.
        Theme glues = ThemeWithDefaults(new ThemeDefaults { WordSeparator = "" });
        SluggerOptions saved = new() { WordSeparator = "+" };

        // Exercise
        GenerationOptions fromTheme = OptionResolver.Resolve(SluggerOptions.Empty, saved, glues, themesInScope: 1);
        GenerationOptions overruled = OptionResolver.Resolve(new SluggerOptions { WordSeparator = "_" }, saved, glues, themesInScope: 1);

        // Verify
        Assert.Equal("", fromTheme.WordSeparator);
        Assert.Equal("_", overruled.WordSeparator);
    }

    /// <summary>
    /// Said by nobody, the word separator is not a value of its own: the separator does the job,
    /// which is what every slug looked like before the option existed.
    /// </summary>
    [Fact]
    public void Nobody_saying_anything_leaves_the_separator_to_join_the_words_too()
    {
        // Exercise
        GenerationOptions options = OptionResolver.Resolve(SluggerOptions.Empty, saved: null, Styled, themesInScope: 1);

        // Verify
        Assert.Null(options.WordSeparator);
    }

    /// <summary>
    /// What arms the theme's defaults is the number of active themes, not the flag - a single
    /// --theme heroku already reproduces heroku's style (ADR 0004).
    /// </summary>
    [Fact]
    public void Absent_the_flag_a_single_theme_applies_its_own_style()
    {
        Assert.True(OptionResolver.AppliesTheStyleOf(null, themesInScope: 1));
    }

    [Fact]
    public void Absent_the_flag_several_themes_keep_one_consistent_format()
    {
        Assert.False(OptionResolver.AppliesTheStyleOf(null, themesInScope: 3));
    }

    [Fact]
    public void Auto_is_the_same_as_not_passing_the_flag()
    {
        Assert.True(OptionResolver.AppliesTheStyleOf(MimicStyle.Auto, themesInScope: 1));
        Assert.False(OptionResolver.AppliesTheStyleOf(MimicStyle.Auto, themesInScope: 3));
    }

    [Fact]
    public void Force_applies_the_drawn_themes_style_even_among_several()
    {
        Assert.True(OptionResolver.AppliesTheStyleOf(MimicStyle.Force, themesInScope: 3));
    }

    [Fact]
    public void Off_refuses_the_style_even_for_a_single_theme()
    {
        Assert.False(OptionResolver.AppliesTheStyleOf(MimicStyle.Off, themesInScope: 1));
    }

    [Fact]
    public void An_explicit_argument_still_wins_when_mimic_style_is_forced()
    {
        // Setup - the worked example of ADR 0004: --mimic-style false --sep = ignores the theme, then forces =.
        SluggerOptions commandLine = new() { Separator = '=', MimicStyle = MimicStyle.Off };

        // Exercise
        GenerationOptions options = OptionResolver.Resolve(commandLine, saved: null, Styled, themesInScope: 1);

        // Verify
        Assert.Equal('=', options.Separator);
        Assert.Equal(GenerationOptions.Default.TokenLength, options.TokenLength);
    }

    [Fact]
    public void Merging_lets_the_command_line_win_option_by_option()
    {
        // Setup
        SluggerOptions commandLine = new() { Count = 5 };
        SluggerOptions saved = new() { Count = 2, Seed = 42, Oneshot = true };

        // Exercise
        SluggerOptions merged = OptionResolver.Merge(commandLine, saved);

        // Verify - Count comes from the command line, the rest survives from the config.
        Assert.Equal(5, merged.Count);
        Assert.Equal(42, merged.Seed);
        Assert.True(merged.Oneshot);
    }

    [Fact]
    public void Merging_with_no_saved_config_changes_nothing()
    {
        // Setup
        SluggerOptions commandLine = new() { Count = Any.Int32().Between(1, 20).Generate() };

        // Exercise
        SluggerOptions merged = OptionResolver.Merge(commandLine, saved: null);

        // Verify
        Assert.Same(commandLine, merged);
    }

    private static Theme ThemeWithDefaults(ThemeDefaults defaults) =>
        new(Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", [])],
            defaults);
}
