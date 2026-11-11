#region Usings declarations

using Slugger.Cli.CommandLine;
using Slugger.Cli.Rendering;
using Slugger.Domain;
using Slugger.Domain.Analysis;
using Slugger.Errors;

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
        Error rejection = SluggerThemeError.Rejected(
            "broken", [SluggerThemeError.TooFewNouns(3, 100), SluggerThemeError.PoolTooSmall("willow", 2, 100)]);

        // Exercise
        FakeConsole console = new();
        console.WriteError(ReportRenderer.Draw(rejection));

        // Verify
        Assert.Equal(ReportRenderer.Render(rejection), console.Errors);
    }

    /// <summary>
    ///     A square bracket reaches the report from a theme name, a category or a value someone typed.
    ///     Spectre read one as markup, and an unescaped one threw or swallowed what it was about; the
    ///     report is written as it is since DEC0032, and this keeps it that way.
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
    [Fact]
    public void Leaves_a_long_reason_on_one_line() {
        // Setup - a sentence far wider than any terminal.
        string sentence = string.Join(
            ' ',
            Enumerable.Range(0, 60).Select(_ => Any.String().WithChars("abcdefghijklmnopqrstuvwxyz").WithLengthBetween(3, 10).Generate()));
        StringWriter written = new();

        // Exercise
        new Terminal(written, false).Write(ReportRenderer.Draw(CliErrors.Rejected([CliErrors.NotUnderstood(sentence)])));

        // Verify
        IEnumerable<string> lines = written.ToString().Split(Environment.NewLine);
        Assert.Contains($"  - {sentence}.", lines, StringComparer.Ordinal);
    }

    [Fact]
    public void Draws_the_verdict_in_its_ink_and_the_count_of_reasons_left_out_fainter() {
        // Setup
        const string Reason = "  - \"noun0\" reaches 0 adjectives";
        const string Count  = "    ... and 7 more of the same kind";

        // Exercise
        IReadOnlyList<DrawnLine> lines = ReportRenderer.Drawn(["Refused.", Reason, Count], Ink.Refusal);

        // Verify
        Assert.Equal([Ink.Refusal, Ink.Plain, Ink.Faint], lines.Select(line => line.Ink));
    }

    [Fact]
    public void Says_a_theme_is_accepted_without_making_anyone_open_the_report() {
        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(new ThemeAnalysisFacts("cuisine", [], [], null)));

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
        ThemeAnalysisFacts analysis = new("cuisine", [], ["one", "two"], null);

        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(analysis));

        // Verify
        Assert.Equal("Theme \"cuisine\" is accepted as it is, with 2 remarks in the report.", Assert.Single(console.Output));
    }

    /// <summary>
    ///     "As it is" said of a theme that passes only because its floors were waived would send its
    ///     author off believing it clears them (DEC0026).
    /// </summary>
    [Fact]
    public void Says_a_theme_accepted_with_its_floors_waived_is_not_accepted_as_it_is() {
        // Setup
        ThemeAnalysisFacts analysis = new("cuisine", [], [], null, FloorsWaived: true);

        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(analysis));

        // Verify
        Assert.Equal("Theme \"cuisine\" is accepted with the size floors waived.", Assert.Single(console.Output));
    }

}