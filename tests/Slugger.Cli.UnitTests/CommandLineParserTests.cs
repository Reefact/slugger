#region Usings declarations

using Slugger.Cli.CommandLine;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     How a line is cut into options and values, which Spectre.Console.Cli decided until DEC0032 and
///     this parser decides since. Each case was measured on the Spectre-bound command first, and is
///     kept where it was right. Literals throughout: each token is the case.
/// </summary>
public sealed class CommandLineParserTests {

    [Fact]
    public void Takes_the_word_after_an_option_as_its_value() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--count", "3"]);

        // Verify
        Assert.Equal("3", line.Settings.Count);
        Assert.Empty(line.Leftovers.Unattached);
        Assert.Empty(line.Leftovers.UnknownOptions);
        Assert.Empty(line.Leftovers.Unreadable);
    }

    [Fact]
    public void Takes_the_value_written_after_an_equals_sign() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--count=3"]);

        // Verify
        Assert.Equal("3", line.Settings.Count);
    }

    /// <summary>The spelling that glues the terms: nothing after the sign is a value, and an empty one.</summary>
    [Fact]
    public void Gives_an_option_with_nothing_after_the_sign_an_empty_value() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--word-sep=", "--oneshot"]);

        // Verify
        Assert.Equal(string.Empty, line.Settings.WordSeparator);
        Assert.True(line.Settings.Oneshot?.IsSet);
    }

    [Fact]
    public void Takes_a_lone_dash_as_a_value() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--sep", "-", "--oneshot"]);

        // Verify
        Assert.Equal("-", line.Settings.Separator);
        Assert.True(line.Settings.Oneshot?.IsSet);
    }

    [Fact]
    public void Takes_a_negative_number_as_a_value() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--seed", "-3"]);

        // Verify
        Assert.Equal("-3", line.Settings.Seed);
    }

    [Fact]
    public void Says_an_option_followed_by_another_was_given_no_value() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--count", "--ascii"]);

        // Verify
        Assert.Null(line.Settings.Count);
        Assert.Equal(["\"--count\" was given no value"], line.Leftovers.Unreadable);
        Assert.True(line.Settings.Ascii?.IsSet);
    }

    [Fact]
    public void Says_an_option_that_ends_the_line_was_given_no_value() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--oneshot", "--sep"]);

        // Verify
        Assert.Equal(["\"--sep\" was given no value"], line.Leftovers.Unreadable);
    }

    [Fact]
    public void Keeps_the_last_value_of_an_option_given_twice() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--count", "2", "--count", "3"]);

        // Verify
        Assert.Equal("3", line.Settings.Count);
    }

    [Fact]
    public void Keeps_every_value_of_a_repeatable_option() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--theme", "spices,rivers", "--theme", "docker"]);

        // Verify
        string[] themes = Assert.IsType<string[]>(line.Settings.Themes);
        Assert.Equal(["spices,rivers", "docker"], themes);
    }

    [Fact]
    public void Leaves_a_switch_not_given_unset() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse([]);

        // Verify
        Assert.Null(line.Settings.Ascii);
        Assert.False(line.Settings.ListThemes);
    }

    [Fact]
    public void Gives_a_bare_switch_no_value() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--ascii", "--count", "1"]);

        // Verify
        Assert.True(line.Settings.Ascii?.IsSet);
        Assert.Null(line.Settings.Ascii?.Value);
        Assert.Equal("1", line.Settings.Count);
    }

    [Fact]
    public void Gives_a_switch_the_word_after_it() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--ascii", "false"]);

        // Verify
        Assert.Equal("false", line.Settings.Ascii?.Value);
    }

    [Fact]
    public void Leaves_a_word_attached_to_no_option_over() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--ascii", "true", "false"]);

        // Verify
        Assert.Equal(["false"], line.Leftovers.Unattached);
    }

    [Fact]
    public void Leaves_everything_after_the_end_of_the_options_over_as_typed() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--sep", "-", "--", "--sep", "-", "--word-sep="]);

        // Verify
        Assert.Equal("-", line.Settings.Separator);
        Assert.Equal(["--sep", "-", "--word-sep="], line.Leftovers.Unattached);
        Assert.Empty(line.Leftovers.UnknownOptions);
    }

    [Fact]
    public void Names_an_option_slugger_does_not_have_once_without_its_value() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--nope=x", "--nope", "-x"]);

        // Verify
        Assert.Equal(["--nope", "-x"], line.Leftovers.UnknownOptions);
    }

    /// <summary>
    ///     Measured on the Spectre-bound command: "--Count 2" was one complaint. The unknown option
    ///     takes the word after it, rather than leaving "2" over as a second complaint about nothing.
    /// </summary>
    [Fact]
    public void Lets_an_option_slugger_does_not_have_take_the_word_after_it() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--Count", "2"]);

        // Verify
        Assert.Equal(["--Count"], line.Leftovers.UnknownOptions);
        Assert.Empty(line.Leftovers.Unattached);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Answers_the_help_wherever_it_is_asked_and_whatever_else_is_wrong(string flag) {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--nope", "--count", flag]);

        // Verify
        Assert.Equal(LineIntent.Help, line.Intent);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-v")]
    public void Answers_the_version_wherever_it_is_asked(string flag) {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--count", "2", flag]);

        // Verify
        Assert.Equal(LineIntent.Version, line.Intent);
    }

    [Fact]
    public void Leaves_a_help_flag_after_the_end_of_the_options_as_a_word() {
        // Exercise
        ParsedLine line = CommandLineParser.Parse(["--", "--help"]);

        // Verify
        Assert.Equal(LineIntent.Run, line.Intent);
        Assert.Equal(["--help"], line.Leftovers.Unattached);
    }

    [Theory]
    [InlineData("--count", true)]
    [InlineData("-x", true)]
    [InlineData("-", false)]
    [InlineData("-3", false)]
    [InlineData("docker", false)]
    public void Tells_an_option_from_a_value(string token, bool isOption) {
        // Verify
        Assert.Equal(isOption, CommandLineParser.IsOption(token));
    }

}