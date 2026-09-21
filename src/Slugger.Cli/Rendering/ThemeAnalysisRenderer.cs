using System.Globalization;
using System.Text;
using FirstClassErrors;
using Slugger.Domain;
using Slugger.Domain.Analysis;
using Spectre.Console;
using Spectre.Console.Rendering;

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

    /// <summary>
    /// The verdict alone, drawn for the terminal. The measurements are the file's job; what the
    /// terminal owes whoever ran the command is whether they have to open it, and why.
    /// </summary>
    /// <param name="analysis">What was measured.</param>
    internal static IRenderable Summary(ThemeAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        if (analysis.Refusals.Count == 0)
        {
            return ReportRenderer.Drawn([Accepted(analysis)], Color.Green);
        }

        List<string> lines =
        [
            string.Create(CultureInfo.InvariantCulture,
                $"theme \"{analysis.Name}\" would be refused, for {Plural(analysis.Refusals.Count, "reason")}:"),
            string.Empty,
            .. ReportRenderer.Reasons(analysis.Refusals),
        ];

        return ReportRenderer.Drawn(lines, Color.Red);
    }

    /// <summary>A remark is not a refusal, and is still a reason to open the report.</summary>
    private static string Accepted(ThemeAnalysis analysis) => analysis.Remarks.Count == 0
        ? $"theme \"{analysis.Name}\" is accepted as it is."
        : string.Create(CultureInfo.InvariantCulture,
            $"theme \"{analysis.Name}\" is accepted as it is, with {Plural(analysis.Remarks.Count, "remark")} in the report.");

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

        if (m.WordsBeforeTheNoun is { } combined)
        {
            report.Append(Row("Words before the noun", combined));
        }

        report.Append(Row("Adjectives per noun", m.Adjectives));
        report.Append(m.Participles is { } participles
            ? Row("Participles per noun", participles)
            : "| Participles per noun | *the theme declares none* | — | — |\n");

        if (m.ParticiplesBesideAnAdjective is { } couple)
        {
            report.Append(CultureInfo.InvariantCulture,
                $"| Participles beside an adjective | {couple.Smallest} (`{couple.Noun}` beside `{couple.Adjective}`) | {couple.Floor} | {Margin(couple.Smallest, couple.Floor)} |\n");
        }

        report.Append(m.CharacterCeiling is { } ceiling
            ? string.Create(CultureInfo.InvariantCulture,
                $"| Characters | {m.LongestSlug.Length} (`{m.LongestSlug}`) | {ceiling} | {Headroom(m.LongestSlug.Length, ceiling)} |\n")
            : string.Create(CultureInfo.InvariantCulture,
                $"| Characters | {m.LongestSlug.Length} (`{m.LongestSlug}`) | — | — |\n"));

        if (m.Combinations is { } combinations)
        {
            report.Append(CultureInfo.InvariantCulture,
                $"| Combinations per category | {combinations.Smallest:N0} (`{combinations.Category}`) | {combinations.Floor:N0} | {Margin(combinations.Smallest, combinations.Floor)} |\n");
        }

        report.Append(CultureInfo.InvariantCulture, $"\n{FloorsFollowTheMode(m.Drawn)}");
        report.Append(m.Combinations is not null && m.Drawn != SegmentMode.Both
            ? " The last row is the exception: it counts the two pools multiplied whatever the mode, "
              + "because `--segment both` reaches that space from any theme.\n\n"
            : "\n\n");
    }

    /// <summary>
    /// A row with no floor is still a row: the count is worth reading next to the one that is
    /// floored, and a blank threshold says plainly that this mode asks nothing of that pool.
    /// </summary>
    private static string Row(string rule, PoolFloor floor) => floor.Floor is { } required
        ? string.Create(CultureInfo.InvariantCulture,
            $"| {rule} | {floor.Smallest} (`{floor.Noun}`) | {required} | {Margin(floor.Smallest, required)} |\n")
        : string.Create(CultureInfo.InvariantCulture, $"| {rule} | {floor.Smallest} (`{floor.Noun}`) | — | — |\n");

    /// <summary>Why the per-noun floors are the ones they are, which is the first thing an author asks.</summary>
    private static string FloorsFollowTheMode(SegmentMode drawn) => drawn switch
    {
        SegmentMode.Either =>
            "Floors follow `segmentMode: either`: one word is drawn in front of the noun, from the two "
            + "pools at once, so it is their sum that has to be rich and neither pool has a floor of its own.",
        SegmentMode.Participle =>
            "Floors follow `segmentMode: participle`: the word in front of the noun is always a participle, "
            + "so the participle pool carries the whole floor and the adjectives are never drawn.",
        SegmentMode.Adjective =>
            "Floors follow `segmentMode: adjective`: the word in front of the noun is always an adjective, "
            + "so any participle the theme declares is never drawn.",
        SegmentMode.ThreeOrTwo =>
            "Floors follow `segmentMode: threeOrTwo`: an adjective is drawn in front of the noun and a "
            + "participle usually joins it, so each pool carries the floor it carries under `both` - the "
            + "absence of a participle takes a share of the draws, never a share of the pool.",
        _ =>
            "Floors follow `segmentMode: both`: an adjective and a participle are drawn in front of the noun, "
            + "so each pool carries its own floor."
    };

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
            $"{Plural(m.UnreachableCategories.Count, "category", "categories")} declared but carried by no noun, so their words never draw. Deliberate if you are keeping words aside; a typo otherwise:\n\n");
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
        report.Append(m.Drawn.PutsAParticipleBesideAnAdjective()
            ? "`--segment either` draws one word before the noun instead of two, if that is long for where the slug goes.\n\n"
            : "That is the upper bound over every mode; this theme draws fewer words than it left alone.\n\n");
    }

    private static void Combinations(StringBuilder report, ThemeMeasurements m)
    {
        report.Append("## Combinations\n\n");
        report.Append(CultureInfo.InvariantCulture,
            $"{m.TotalCombinations:N0} distinct slugs with an adjective and a participle in front, which is what `--segment both` reaches.\n\n");

        if (m.Drawn != SegmentMode.Both)
        {
            // Not a subset of the line above: one word in front of the noun makes a different
            // slug from two, so these are other slugs rather than fewer of the same.
            report.Append(CultureInfo.InvariantCulture,
                $"Left alone it draws `{Spelled(m.Drawn)}`: a different shape of slug, and {m.CombinationsDrawn:N0} of them rather than a subset of the figure above.\n\n");
        }

        report.Append(m.CombinationsDrawn < 40_000
            ? "Below 40,000 — the point where Docker and Heroku both added a numeric suffix. A `tokenLength` in `defaults` is worth considering.\n\n"
            : "Above 40,000, so a suffix is a style choice here rather than a collision defence.\n\n");
    }

    /// <summary>A mode as a theme file spells it, which is how the report must name it.</summary>
    private static string Spelled(SegmentMode mode) => Spelling.Of(mode);

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

    /// <summary>
    /// The inverse of <see cref="Margin"/>, for the one row that is a ceiling rather than a
    /// floor: under it is the good side, so the sign flips and the bold goes to going over.
    /// </summary>
    private static string Headroom(long reached, long ceiling) => reached > ceiling
        ? string.Create(CultureInfo.InvariantCulture, $"**{ceiling - reached:N0}**")
        : string.Create(CultureInfo.InvariantCulture, $"+{ceiling - reached:N0}");

    private static string Margin(long reached, long floor) => reached < floor
        ? string.Create(CultureInfo.InvariantCulture, $"**{reached - floor:N0}**")
        : string.Create(CultureInfo.InvariantCulture, $"+{reached - floor:N0}");

    /// <param name="count">How many there are, which is what decides the form.</param>
    /// <param name="noun">The singular.</param>
    /// <param name="plural">
    /// Written out where adding an "s" does not give it - "categorys" was printed for four
    /// months. The irregular form belongs at the call site, where the word is chosen.
    /// </param>
    private static string Plural(int count, string noun, string? plural = null) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{count:N0} {(count == 1 ? noun : plural ?? noun + "s")}");
}
