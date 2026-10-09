#region Usings declarations

using FirstClassErrors;

using Slugger.Cli.CommandLine;
using Slugger.Cli.Rendering;
using Slugger.Domain.Analysis;
using Slugger.Domain.Validation;

using Spectre.Console;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     What DEC0019's second half is answerable for: a refusal is drawn rather than printed, and a
///     drawn refusal has to say the same thing a printed one did.
/// </summary>
public sealed class DrawnReportTests {

    [Fact]
    public void Draws_word_for_word_what_it_would_have_printed() {
        // Setup
        Error rejection = ThemeErrors.Rejected(
            "broken", [ThemeErrors.TooFewNouns(3, 100), ThemeErrors.PoolTooSmall("willow", 2, 100)]);

        // Exercise
        FakeConsole console = new();
        console.WriteError(ReportRenderer.Draw(rejection));

        // Verify
        Assert.Equal(ReportRenderer.Render(rejection), console.Errors);
    }

    /// <summary>
    ///     The hazard a markup language brings with it. A square bracket reaches the report from a
    ///     theme name, a category or a value someone typed, and Spectre reads one as an instruction:
    ///     unescaped, this is a refusal that either throws or silently swallows what it is about.
    /// </summary>
    [Fact]
    public void Shows_a_square_bracket_that_was_typed_rather_than_obeying_it() {
        // Setup - what someone types after --casing reaches the message whole.
        Error rejection = CliErrors.Rejected([CliErrors.NotOneOf("--casing", "[red]shout[/]", ["kebab"])]);

        // Exercise
        FakeConsole console = new();
        console.WriteError(ReportRenderer.Draw(rejection));

        // Verify
        Assert.Contains(console.Errors, line => line.Contains("[red]shout[/]", StringComparison.Ordinal));
    }

    /// <summary>
    ///     A CI log or <c>2&gt;err.txt</c>: laid out for eighty columns, a refusal broke mid-sentence,
    ///     where a log viewer would have wrapped it itself and grep could still have found it whole.
    /// </summary>
    /// <remarks>
    ///     The colour is taken away after the terminal is built, for the reason the help's cases give:
    ///     on a GitHub runner Spectre turns ANSI back on, and the sentence would arrive in escape codes.
    /// </remarks>
    [Fact]
    public void Leaves_a_long_reason_on_one_line_when_standard_error_is_redirected() {
        // Setup - a sentence far wider than any terminal.
        string sentence = string.Join(
            ' ',
            Enumerable.Range(0, 60).Select(_ => Any.String().WithChars("abcdefghijklmnopqrstuvwxyz").WithLengthBetween(3, 10).Generate()));
        StringWriter written  = new();
        IAnsiConsole terminal = SluggerApp.ReportTerminal(written, true);
        terminal.Profile.Capabilities.Ansi        = false;
        terminal.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;

        // Exercise
        terminal.Write(ReportRenderer.Draw(CliErrors.Rejected([CliErrors.NotUnderstood(sentence)])));

        // Verify
        IEnumerable<string> lines = written.ToString().Split('\n').Select(line => line.TrimEnd('\r', ' '));
        Assert.Contains($"  - {sentence}.", lines, StringComparer.Ordinal);
    }

    /// <summary>
    ///     About the harness rather than the code, and worth a case because of how it fails: a
    ///     renderable that ends without a newline - a bare Markup does - used to lose its last line
    ///     here and nowhere else, so a test would assert on an empty list and pass (measured).
    /// </summary>
    [Fact]
    public void Reads_back_a_renderable_that_ends_without_a_newline() {
        // Exercise
        FakeConsole console = new();
        console.Write(new Markup("the last line"));

        // Verify
        Assert.Equal("the last line", Assert.Single(console.Output));
    }

    [Fact]
    public void Says_a_theme_is_accepted_without_making_anyone_open_the_report() {
        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(new ThemeAnalysis("cuisine", [], [], null)));

        // Verify - on standard output, because the analysis succeeded whatever it found.
        Assert.Equal("Theme \"cuisine\" is accepted as it is.", Assert.Single(console.Output));
        Assert.Empty(console.Errors);
    }

    /// <summary>
    ///     A remark is not a refusal and is still a reason to read the file, so the one line the
    ///     terminal gets says there are some rather than leaving them unmentioned.
    /// </summary>
    [Fact]
    public void Counts_the_remarks_of_a_theme_that_is_accepted_anyway() {
        // Setup
        ThemeAnalysis analysis = new("cuisine", [], ["one", "two"], null);

        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(analysis));

        // Verify
        Assert.Equal("Theme \"cuisine\" is accepted as it is, with 2 remarks in the report.", Assert.Single(console.Output));
    }

}