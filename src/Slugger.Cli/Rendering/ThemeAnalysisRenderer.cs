using System.Globalization;
using System.Text;
using FirstClassErrors;
using Slugger.Domain.Analysis;

namespace Slugger.Cli.Rendering;

/// <summary>
/// Turns an analysis into the markdown an author reads. Every sentence in the report is written
/// here and nowhere else - the library measured, this writes (DEC0006).
/// </summary>
internal static class ThemeAnalysisRenderer
{
    /// <summary>How many offenders one section names before counting the rest.</summary>
    private const int MaxNamed = 10;

    /// <summary>Renders the whole report.</summary>
    /// <param name="analysis">What was measured.</param>
    internal static string Render(ThemeAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        StringBuilder report = new();
        report.Append(CultureInfo.InvariantCulture, $"# {analysis.Name} — theme analysis\n\n");
        report.Append(Verdict(analysis));

        if (analysis.Measurements is not { } measured)
        {
            // Nothing parsed, so every section below would be empty. Say why and stop.
            return report.Append("\nThe document could not be read, so there is nothing to measure.\n").ToString();
        }

        Margins(report, measured);
        Duplicates(report, measured);
        Unreachable(report, measured);
        Exposure(report, measured);
        Shape(report, measured);
        Combinations(report, measured);
        Remarks(report, analysis);

        return report.ToString();
    }

    private static string Verdict(ThemeAnalysis analysis) => analysis.Refusals.Count == 0
        ? "**Accepted.** It loads as it is.\n\n"
        : Refused(analysis);

    private static string Refused(ThemeAnalysis analysis)
    {
        StringBuilder refused = new();
        refused.Append(CultureInfo.InvariantCulture,
            $"**Refused**, for {Plural(analysis.Refusals.Count, "reason")}.\n\n");

        foreach (IGrouping<string, Error> kind in analysis.Refusals.GroupBy(reason => reason.Code.ToString()))
        {
            Error[] sameKind = [.. kind];
            foreach (Error reason in sameKind.Take(3))
            {
                refused.Append(CultureInfo.InvariantCulture, $"- {reason.DiagnosticMessage}\n");
            }

            if (sameKind.Length > 3)
            {
                refused.Append(CultureInfo.InvariantCulture, $"- *… and {sameKind.Length - 3} more of the same kind*\n");
            }
        }

        return refused.Append('\n').ToString();
    }

    /// <summary>
    /// The section the tool did not have: not whether a floor is cleared, but by how much. A
    /// theme two words above a floor reads as fine and breaks on the next edit.
    /// </summary>
    private static void Margins(StringBuilder report, ThemeMeasurements m)
    {
        report.Append("## Margins\n\n| Rule | Worst case | Floor | Margin |\n| --- | --- | --- | --- |\n");
        report.Append(CultureInfo.InvariantCulture,
            $"| Distinct nouns | {m.DistinctNouns} | 100 | {Margin(m.DistinctNouns, 100)} |\n");
        report.Append(CultureInfo.InvariantCulture,
            $"| Adjectives per noun | {m.Adjectives.Smallest} (`{m.Adjectives.Noun}`) | {m.Adjectives.Floor} | {Margin(m.Adjectives.Smallest, m.Adjectives.Floor)} |\n");

        report.Append(m.Participles is { } participles
            ? string.Create(CultureInfo.InvariantCulture,
                $"| Participles per noun | {participles.Smallest} (`{participles.Noun}`) | {participles.Floor} | {Margin(participles.Smallest, participles.Floor)} |\n")
            : "| Participles per noun | *the theme declares none* | — | — |\n");

        if (m.Combinations is { } combinations)
        {
            report.Append(CultureInfo.InvariantCulture,
                $"| Combinations per category | {combinations.Smallest:N0} (`{combinations.Category}`) | {combinations.Floor:N0} | {Margin(combinations.Smallest, combinations.Floor)} |\n");
        }

        report.Append('\n');
    }

    /// <summary>
    /// Three of the four ways to repeat a word are harmless, so the report says so rather than
    /// listing noise. The fourth is not: a value written twice is drawn twice as often.
    /// </summary>
    private static void Duplicates(StringBuilder report, ThemeMeasurements m)
    {
        report.Append("## Duplicates\n\n");

        if (m.DuplicatedNouns.Count == 0)
        {
            report.Append("No noun is declared twice. A word repeated across categories is harmless — the pool is a set.\n\n");

            return;
        }

        report.Append(CultureInfo.InvariantCulture,
            $"**{Plural(m.DuplicatedNouns.Count, "noun")} declared more than once.** The draw indexes the list while the size rule counts distinct values, so each of these is drawn more often than its neighbours:\n\n");
        Name(report, m.DuplicatedNouns);
    }

    private static void Unreachable(StringBuilder report, ThemeMeasurements m)
    {
        report.Append("## Categories nothing carries\n\n");

        if (m.UnreachableCategories.Count == 0)
        {
            report.Append("Every declared category is carried by at least one noun.\n\n");

            return;
        }

        report.Append(CultureInfo.InvariantCulture,
            $"{Plural(m.UnreachableCategories.Count, "category")} declared but carried by no noun, so their words never draw. Deliberate if you are keeping words aside; a typo otherwise:\n\n");
        Name(report, m.UnreachableCategories);
    }

    private static void Exposure(StringBuilder report, ThemeMeasurements m)
    {
        report.Append("## Exposure\n\n");
        report.Append(CultureInfo.InvariantCulture,
            $"How many nouns can reach one adjective, from `{m.LeastExposed.Word}` at {m.LeastExposed.Nouns} to `{m.MostExposed.Word}` at {m.MostExposed.Nouns}");

        report.Append(m.LeastExposed.Nouns > 0
            ? string.Create(CultureInfo.InvariantCulture, $" — a spread of {m.MostExposed.Nouns / (double)m.LeastExposed.Nouns:N0}×.\n\n")
            : ".\n\n");
        report.Append("A narrow category is decorative rather than wrong; this only says which ones are.\n\n");
    }

    private static void Shape(StringBuilder report, ThemeMeasurements m)
    {
        report.Append("## Shape of the slug\n\n");
        report.Append(CultureInfo.InvariantCulture,
            $"- {m.TwoWordAdjectives} of {m.TotalAdjectives} adjectives are written in more than one word\n");
        report.Append(CultureInfo.InvariantCulture,
            $"- {m.TwoWordNouns} of {m.Nouns} nouns are\n");
        report.Append(CultureInfo.InvariantCulture,
            $"- the longest slug this theme can produce carries {Plural(m.LongestSlugSegments, "segment")}, token aside\n\n");
        report.Append("`--segment either` draws one word before the noun instead of two, if that is long for where the slug goes.\n\n");
    }

    private static void Combinations(StringBuilder report, ThemeMeasurements m)
    {
        report.Append("## Combinations\n\n");
        report.Append(CultureInfo.InvariantCulture,
            $"{m.TotalCombinations:N0} distinct slugs, participle included.\n\n");
        report.Append(m.TotalCombinations < 40_000
            ? "Below 40,000 — the point where Docker and Heroku both added a numeric suffix. A `tokenLength` in `defaults` is worth considering.\n\n"
            : "Above 40,000, so a suffix is a style choice here rather than a collision defence.\n\n");
    }

    private static void Remarks(StringBuilder report, ThemeAnalysis analysis)
    {
        if (analysis.Remarks.Count == 0)
        {
            return;
        }

        report.Append("## Worth a second look\n\n");
        foreach (string remark in analysis.Remarks)
        {
            report.Append(CultureInfo.InvariantCulture, $"- {remark}\n");
        }

        report.Append('\n');
    }

    private static void Name(StringBuilder report, IReadOnlyList<string> offenders)
    {
        foreach (string offender in offenders.Take(MaxNamed))
        {
            report.Append(CultureInfo.InvariantCulture, $"- `{offender}`\n");
        }

        if (offenders.Count > MaxNamed)
        {
            report.Append(CultureInfo.InvariantCulture, $"- *… and {offenders.Count - MaxNamed} more*\n");
        }

        report.Append('\n');
    }

    private static string Margin(long reached, long floor) => reached < floor
        ? string.Create(CultureInfo.InvariantCulture, $"**{reached - floor:N0}**")
        : string.Create(CultureInfo.InvariantCulture, $"+{reached - floor:N0}");

    private static string Plural(int count, string noun) =>
        string.Create(CultureInfo.InvariantCulture, $"{count:N0} {noun}{(count == 1 ? string.Empty : "s")}");
}
