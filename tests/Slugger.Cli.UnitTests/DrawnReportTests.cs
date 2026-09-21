using FirstClassErrors;
using Slugger.Cli.CommandLine;
using Slugger.Cli.Rendering;
using Slugger.Domain.Analysis;
using Slugger.Domain.Validation;

namespace Slugger.Cli.UnitTests;

/// <summary>
/// What DEC0019's second half is answerable for: a refusal is drawn rather than printed, and a
/// drawn refusal has to say the same thing a printed one did.
/// </summary>
public sealed class DrawnReportTests
{
    [Fact]
    public void Draws_word_for_word_what_it_would_have_printed()
    {
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
    /// The hazard a markup language brings with it. A square bracket reaches the report from a
    /// theme name, a category or a value someone typed, and Spectre reads one as an instruction:
    /// unescaped, this is a refusal that either throws or silently swallows what it is about.
    /// </summary>
    [Fact]
    public void Shows_a_square_bracket_that_was_typed_rather_than_obeying_it()
    {
        // Setup - what someone types after --casing reaches the message whole.
        Error rejection = CliErrors.Rejected([CliErrors.NotOneOf("--casing", "[red]shout[/]", ["kebab"])]);

        // Exercise
        FakeConsole console = new();
        console.WriteError(ReportRenderer.Draw(rejection));

        // Verify
        Assert.Contains(console.Errors, line => line.Contains("[red]shout[/]", StringComparison.Ordinal));
    }

    [Fact]
    public void Says_a_theme_is_accepted_without_making_anyone_open_the_report()
    {
        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(new ThemeAnalysis("cuisine", [], [], null)));

        // Verify - on standard output, because the analysis succeeded whatever it found.
        Assert.Contains("accepted", Assert.Single(console.Output), StringComparison.Ordinal);
        Assert.Empty(console.Errors);
    }

    /// <summary>
    /// A remark is not a refusal and is still a reason to read the file, so the one line the
    /// terminal gets says there are some rather than leaving them unmentioned.
    /// </summary>
    [Fact]
    public void Counts_the_remarks_of_a_theme_that_is_accepted_anyway()
    {
        // Setup
        ThemeAnalysis analysis = new("cuisine", [], ["one", "two"], null);

        // Exercise
        FakeConsole console = new();
        console.Write(ThemeAnalysisRenderer.Summary(analysis));

        // Verify
        Assert.Contains("2 remarks", Assert.Single(console.Output), StringComparison.Ordinal);
    }
}
