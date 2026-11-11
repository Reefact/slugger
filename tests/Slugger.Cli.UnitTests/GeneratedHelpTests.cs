#region Usings declarations

using System.Globalization;

using Slugger.Cli.CommandLine;
using Slugger.Cli.Rendering;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The help is drawn from the declarations rather than written, so what these pin is that it
///     carries everything they declare, in the layout every terminal can show (DEC0019, DEC0032).
/// </summary>
public sealed class GeneratedHelpTests {

    #region Static members

    private static IReadOnlyList<string> Help() {
        return [.. HelpRenderer.Lines().Select(line => line.Text)];
    }

    private static string Options(IReadOnlyList<string> help) {
        int heading = help.ToList().IndexOf("OPTIONS:");
        Assert.True(heading >= 0, "the help carries no options section at all");

        return string.Join('\n', help.Skip(heading));
    }

    private static HashSet<string> Words(string text) {
        return [.. text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(word => word.TrimEnd(','))];
    }

    #endregion

    [Fact]
    public void Draws_the_usage_the_examples_and_the_options() {
        // Exercise
        IReadOnlyList<string> help = Help();

        // Verify
        Assert.Contains("USAGE:", help);
        Assert.Contains("EXAMPLES:", help);
        Assert.Contains("OPTIONS:", help);
    }

    [Fact]
    public void Shows_every_example_it_was_given() {
        // Setup
        string[] examples = [
            "--theme docker --count 3",
            "--ascii --max-length 63 --oneshot",
            "--analyze ./my-theme.json --max-length 63",
            "--register ./my-theme.json"
        ];

        // Exercise
        IReadOnlyList<string> help = Help();

        // Verify
        Assert.All(examples, example => Assert.Contains($"    slugger {example}", help));
    }

    [Fact]
    public void Names_every_option_the_settings_declare() {
        // Exercise
        HashSet<string> listed = Words(Options(Help()));

        // Verify
        Assert.NotEmpty(CommandLineParser.Options);
        Assert.All(CommandLineParser.Options, option => Assert.Contains(option.Name, listed));
        Assert.Contains("--help", listed);
        Assert.Contains("--version", listed);
    }

    [Fact]
    public void Prints_one_language_whatever_the_machine_is_set_to() {
        // Setup
        IReadOnlyList<string> help = [];
        Thread french = new(() => {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            help                         = Help();
        });

        // Exercise
        french.Start();
        french.Join();

        // Verify
        Assert.Contains("USAGE:", help);
    }

    [Fact]
    public void Lays_out_within_eighty_columns() {
        // Exercise
        IReadOnlyList<string> help = Help();

        // Verify
        Assert.All(help, line => Assert.True(line.Length <= HelpRenderer.Width, $"{line.Length} columns: {line}"));
    }

    [Fact]
    public void Folds_a_long_description_without_cutting_a_word() {
        // Setup
        string description = CommandLineParser.Options.Single(option => option.Name == "--segment").Description.TrimEnd('.');

        // Exercise - the words of the options section in order, whatever width they were folded to.
        string options = string.Join(' ', Options(Help()).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        // Verify
        Assert.Contains(description, options, StringComparison.Ordinal);
    }

    [Fact]
    public void Says_where_it_offers_participle_what_the_mode_needs_and_which_theme_has_it() {
        // Exercise
        string options = string.Join(' ', Options(Help()).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        // Verify
        Assert.Contains("participle needs a theme with at least 100 participles per noun, as docker has", options, StringComparison.Ordinal);
    }

    [Fact]
    public void Draws_its_headings_apart_from_the_rest() {
        // Exercise
        IReadOnlyList<DrawnLine> help = HelpRenderer.Lines();

        // Verify
        Assert.Equal(["USAGE:", "EXAMPLES:", "OPTIONS:"], help.Where(line => line.Ink == Ink.Heading).Select(line => line.Text));
    }

}