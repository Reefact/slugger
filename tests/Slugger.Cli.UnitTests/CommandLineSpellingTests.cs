#region Usings declarations

using Slugger.Cli.CommandLine;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The two spellings rewritten before Spectre reads the line, and everything that has to reach it
///     exactly as typed. Literals throughout: each token is the case, and an arbitrary one would say
///     nothing about which spelling is meant.
/// </summary>
public sealed class CommandLineSpellingTests {

    [Fact]
    public void Attaches_a_lone_dash_to_sep() {
        // Exercise
        IReadOnlyList<string> spelled = SluggerApp.Spelled(["--sep", "-", "--oneshot"]);

        // Verify
        Assert.Equal(["--sep=-", "--oneshot"], spelled);
    }

    [Fact]
    public void Attaches_a_lone_dash_to_word_sep() {
        // Exercise
        IReadOnlyList<string> spelled = SluggerApp.Spelled(["--word-sep", "-"]);

        // Verify
        Assert.Equal(["--word-sep=-"], spelled);
    }

    [Fact]
    public void Gives_word_sep_with_nothing_after_the_sign_an_empty_value() {
        // Exercise
        IReadOnlyList<string> spelled = SluggerApp.Spelled(["--word-sep=", "--oneshot"]);

        // Verify
        Assert.Equal(["--word-sep", "", "--oneshot"], spelled);
    }

    /// <summary>No other option takes a character, so a dash after one is left for Spectre to refuse.</summary>
    [Fact]
    public void Leaves_a_dash_after_any_other_option_as_typed() {
        // Exercise
        IReadOnlyList<string> spelled = SluggerApp.Spelled(["--theme", "-"]);

        // Verify
        Assert.Equal(["--theme", "-"], spelled);
    }

    /// <summary>A separator option with nothing after it is Spectre's to report as missing its value.</summary>
    [Fact]
    public void Leaves_a_separator_option_that_ends_the_line_as_typed() {
        // Exercise
        IReadOnlyList<string> spelled = SluggerApp.Spelled(["--oneshot", "--sep"]);

        // Verify
        Assert.Equal(["--oneshot", "--sep"], spelled);
    }

    /// <summary>The options stop at a bare "--", and so does the rewriting.</summary>
    [Fact]
    public void Leaves_everything_after_the_end_of_the_options_as_typed() {
        // Exercise
        IReadOnlyList<string> spelled = SluggerApp.Spelled(["--sep", "-", "--", "--sep", "-", "--word-sep="]);

        // Verify
        Assert.Equal(["--sep=-", "--", "--sep", "-", "--word-sep="], spelled);
    }

}