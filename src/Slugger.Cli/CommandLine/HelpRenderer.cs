#region Usings declarations

using System.Reflection;

using Slugger.Cli.Rendering;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     <c>--help</c>, drawn from the options <see cref="SluggerSettings" /> declares: the help cannot
///     describe a flag that no longer exists or miss one that does, because it is not written (DEC0019).
/// </summary>
internal static class HelpRenderer {

    /// <summary>
    ///     The width the help is laid out for, wherever it is printed: the width every terminal has
    ///     had since the VT100, and readable in a pipe or a CI log.
    /// </summary>
    internal const int Width = 80;

    /// <summary>What sets a column apart from the margin, and from the column beside it.</summary>
    private const string Gutter = "    ";

    #region Static members

    private static readonly string[] Examples = [
        "--theme docker --count 3",
        "--ascii --max-length 63 --oneshot",
        "--analyze ./my-theme.json --max-length 63",
        "--register ./my-theme.json"
    ];

    /// <summary>The help, line by line.</summary>
    internal static IReadOnlyList<DrawnLine> Lines() {
        List<DrawnLine> lines = [
            new("USAGE:", Ink.Heading),
            new($"{Gutter}slugger [OPTIONS]", Ink.Plain),
            new(string.Empty, Ink.Plain),
            new("EXAMPLES:", Ink.Heading),
            .. Examples.Select(example => new DrawnLine($"{Gutter}slugger {example}", Ink.Plain)),
            new(string.Empty, Ink.Plain),
            new("OPTIONS:", Ink.Heading)
        ];

        (string Label, string Description)[] rows = [
            ("-h, --help", "Prints help information"),
            ("-v, --version", "Prints version information"),
            .. CommandLineParser.Options.Select(option => (Gutter + Label(option), option.Description.TrimEnd('.')))
        ];

        int labelWidth = rows.Max(row => row.Label.Length);
        foreach ((string label, string description) in rows) {
            lines.AddRange(Row(label, description, labelWidth));
        }

        return lines;
    }

    /// <summary>The version the command was built from, as its informational version spells it.</summary>
    internal static string Version() {
        return typeof(HelpRenderer).Assembly
                                   .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                                   .InformationalVersion;
    }

    /// <summary>The option as the help names it, its values in capitals.</summary>
    private static string Label(DeclaredOption option) {
        return option.Template.Replace("[true|false]", "[TRUE|FALSE]", StringComparison.Ordinal);
    }

    /// <summary>One option: its label, then its description folded into the column beside it.</summary>
    private static IEnumerable<DrawnLine> Row(string label, string description, int labelWidth) {
        string       indent = Gutter + new string(' ', labelWidth) + Gutter;
        List<string> folded = Folded(description, Width - indent.Length);

        return folded.Select((part, index) => new DrawnLine(
                                 index == 0 ? Gutter + label.PadRight(labelWidth) + Gutter + part : indent + part,
                                 Ink.Plain));
    }

    /// <summary>The words of a text gathered into lines no wider than the column, a word never cut.</summary>
    private static List<string> Folded(string text, int width) {
        List<string> lines   = [];
        string       current = string.Empty;
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
            if (current.Length > 0 && current.Length + 1 + word.Length > width) {
                lines.Add(current);
                current = word;

                continue;
            }

            current = current.Length == 0 ? word : current + " " + word;
        }

        lines.Add(current);

        return lines;
    }

    #endregion

}