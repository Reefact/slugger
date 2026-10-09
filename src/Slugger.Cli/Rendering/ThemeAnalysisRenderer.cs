#region Usings declarations

using System.Globalization;
using System.Text;

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Analysis;
using Slugger.Domain.Validation;

using Spectre.Console;
using Spectre.Console.Rendering;

#endregion

namespace Slugger.Cli.Rendering;

/// <summary>
///     Turns an analysis into the markdown an author reads. Every sentence in the report is written
///     here and nowhere else - the library measured, this writes (DEC0006).
/// </summary>
internal static class ThemeAnalysisRenderer {

    /// <summary>How many offenders one section names before counting the rest.</summary>
    private const int MaxNamed = 10;

    #region Static members

    /// <summary>Renders the whole report.</summary>
    /// <param name="analysis">What was measured.</param>
    internal static string Render(ThemeAnalysis analysis) {
        ArgumentNullException.ThrowIfNull(analysis);

        StringBuilder report = new();
        report.Append(CultureInfo.InvariantCulture, $"# {analysis.Name} — theme analysis\n\n");
        report.Append(Verdict(analysis));

        if (analysis.Measurements is not { } measured) {
            // Nothing measured, so every section below would be empty. Say why and stop.
            return report.Append(analysis.Read
                                     ? "The file was read, but these errors leave nothing that can be measured. Fix them and run `--analyze` again to see the margins.\n"
                                     : "The document could not be read, so there is nothing to measure.\n")
                         .ToString();
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
    ///     The verdict alone, drawn for the terminal. The measurements are the file's job; what the
    ///     terminal owes whoever ran the command is whether they have to open it, and why.
    /// </summary>
    /// <param name="analysis">What was measured.</param>
    internal static IRenderable Summary(ThemeAnalysis analysis) {
        ArgumentNullException.ThrowIfNull(analysis);

        if (analysis.Refusals.Count == 0) { return ReportRenderer.Drawn([Accepted(analysis)], Color.Green); }

        List<string> lines = [
            string.Create(CultureInfo.InvariantCulture,
                          $"Theme \"{analysis.Name}\" would be refused for {Plural(analysis.Refusals.Count, "reason")}:"),
            string.Empty,
            .. ReportRenderer.Reasons(analysis.Refusals)
        ];

        // The report holds no numbers then, and whoever reads only the terminal should not go looking.
        if (analysis.Measurements is null) {
            lines.AddRange([string.Empty, NothingMeasured(analysis)]);
        }

        return ReportRenderer.Drawn(lines, Color.Red);
    }

    /// <summary>
    ///     Why the report stops at its verdict. A file that was read is not called unreadable: what
    ///     stands between its author and the margins is the errors listed, and fixing them is enough.
    /// </summary>
    private static string NothingMeasured(ThemeAnalysis analysis) {
        return analysis.Read
            ? "The file was read, but these errors leave nothing that can be measured: fix them and run --analyze again to see the margins."
            : "The file could not be read, so nothing was measured.";
    }

    /// <summary>A remark is not a refusal, and is still a reason to open the report.</summary>
    private static string Accepted(ThemeAnalysis analysis) {
        return analysis.Remarks.Count == 0
            ? $"Theme \"{analysis.Name}\" is accepted as it is."
            : string.Create(CultureInfo.InvariantCulture,
                            $"Theme \"{analysis.Name}\" is accepted as it is, with {Plural(analysis.Remarks.Count, "remark")} in the report.");
    }

    private static string Verdict(ThemeAnalysis analysis) {
        return analysis.Refusals.Count == 0
            ? "**Accepted.** It loads as it is.\n\n"
            : Refused(analysis);
    }

    private static string Refused(ThemeAnalysis analysis) {
        StringBuilder refused = new();
        refused.Append(CultureInfo.InvariantCulture,
                       $"**Refused**, for {Plural(analysis.Refusals.Count, "reason")}.\n\n");

        foreach (IGrouping<string, Error> kind in analysis.Refusals.GroupBy(reason => reason.Code.ToString())) {
            Error[] sameKind = [.. kind];
            foreach (Error reason in sameKind.Take(3)) {
                refused.Append(CultureInfo.InvariantCulture, $"- {reason.DiagnosticMessage}\n");
            }

            if (sameKind.Length > 3) {
                refused.Append(CultureInfo.InvariantCulture, $"- *… and {sameKind.Length - 3} more of the same kind*\n");
            }
        }

        return refused.Append('\n').ToString();
    }

    /// <summary>
    ///     The section the tool did not have: not whether a floor is cleared, but by how much. A
    ///     theme two words above a floor reads as fine and breaks on the next edit.
    /// </summary>
    private static void Margins(StringBuilder report, ThemeMeasurements m) {
        report.Append("## Margins\n\n| Rule | Worst case | Floor | Margin |\n| --- | --- | --- | --- |\n");
        report.Append(CultureInfo.InvariantCulture,
                      $"| Distinct nouns | {m.DistinctNouns} | {ThemeValidator.MinimumNouns} | {Margin(m.DistinctNouns, ThemeValidator.MinimumNouns)} |\n");

        if (m.WordsBeforeTheNoun is { } combined) {
            report.Append(Row("Words before the noun", combined));
        }

        report.Append(Row("Adjectives per noun", m.Adjectives));
        report.Append(m.Participles is { } participles
                          ? Row("Participles per noun", participles)
                          : "| Participles per noun | *the theme declares none* | — | — |\n");

        if (m.ParticiplesBesideAnAdjective is { } couple) {
            report.Append(CultureInfo.InvariantCulture,
                          $"| Participles beside an adjective | {couple.Smallest} (`{couple.Noun}` beside `{couple.Adjective}`) | {couple.Floor} | {Margin(couple.Smallest, couple.Floor)} |\n");
        }

        report.Append(m.CharacterCeiling is { } ceiling
                          ? string.Create(CultureInfo.InvariantCulture,
                                          $"| Characters | {m.LongestSlug.Length} (`{m.LongestSlug}`) | {ceiling} | {Headroom(m.LongestSlug.Length, ceiling)} |\n")
                          : string.Create(CultureInfo.InvariantCulture,
                                          $"| Characters | {m.LongestSlug.Length} (`{m.LongestSlug}`) | — | — |\n"));

        if (m.Combinations is { } combinations) {
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
    ///     A row with no floor is still a row: the count is worth reading next to the one that is
    ///     floored, and a blank threshold says plainly that this mode asks nothing of that pool.
    /// </summary>
    private static string Row(string rule, PoolFloor floor) {
        return floor.Floor is { } required
            ? string.Create(CultureInfo.InvariantCulture,
                            $"| {rule} | {floor.Smallest} (`{floor.Noun}`) | {required} | {Margin(floor.Smallest, required)} |\n")
            : string.Create(CultureInfo.InvariantCulture, $"| {rule} | {floor.Smallest} (`{floor.Noun}`) | — | — |\n");
    }

    /// <summary>Why the per-noun floors are the ones they are, which is the first thing an author asks.</summary>
    private static string FloorsFollowTheMode(SegmentMode drawn) {
        return drawn switch {
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
    }

    /// <summary>
    ///     Three of the four ways to repeat a word are harmless, so the report says so rather than
    ///     listing noise. The fourth is not: a value written twice is drawn twice as often.
    /// </summary>
    private static void Duplicates(StringBuilder report, ThemeMeasurements m) {
        report.Append("## Duplicates\n\n");

        if (m.DuplicatedNouns.Count == 0) {
            report.Append("No noun is declared twice. A word repeated across categories is harmless — the pool is a set.\n\n");

            return;
        }

        report.Append(CultureInfo.InvariantCulture,
                      $"**{Plural(m.DuplicatedNouns.Count, "noun")} declared more than once.** The draw indexes the list while the size rule counts distinct values, so each of these is drawn more often than its neighbours:\n\n");
        Name(report, m.DuplicatedNouns);
    }

    private static void Unreachable(StringBuilder report, ThemeMeasurements m) {
        report.Append("## Categories nothing carries\n\n");

        if (m.UnreachableCategories.Count == 0) {
            report.Append("Every declared category is carried by at least one noun.\n\n");

            return;
        }

        report.Append(CultureInfo.InvariantCulture,
                      $"{Plural(m.UnreachableCategories.Count, "category", "categories")} declared but carried by no noun, so their words never draw. Deliberate if you are keeping words aside; a typo otherwise:\n\n");
        Name(report, m.UnreachableCategories);
    }

    private static void Exposure(StringBuilder report, ThemeMeasurements m) {
        report.Append("## Exposure\n\n");

        if (m.LeastExposed.Nouns == m.MostExposed.Nouns) {
            report.Append(EvenExposure(m));

            return;
        }

        report.Append(CultureInfo.InvariantCulture,
                      $"How many nouns can reach one adjective, from `{m.LeastExposed.Word}` at {m.LeastExposed.Nouns} to `{m.MostExposed.Word}` at {m.MostExposed.Nouns} — a spread of {m.MostExposed.Nouns / (double)m.LeastExposed.Nouns:N0}×.\n\n");
        report.Append("A wide spread is not a fault: an adjective declared in a category that few nouns carry is drawn "
                    + "beside those nouns only, which is usually why it was put there. This measures how uneven the reach "
                    + "is; it does not ask for it to be even.\n\n");
    }

    /// <summary>
    ///     No spread to measure: naming the rarest and the commonest adjective would name one word
    ///     twice - "from `affable` at 236 to `affable` at 236" - and say nothing.
    /// </summary>
    private static string EvenExposure(ThemeMeasurements m) {
        int reached = m.MostExposed.Nouns;
        if (reached == 0) { return "No noun can reach an adjective.\n\n"; }
        if (reached == m.Nouns) { return $"Every adjective reaches all {Plural(m.Nouns, "noun")}.\n\n"; }

        return string.Create(CultureInfo.InvariantCulture,
                             $"Every adjective reaches {reached:N0} of the {Plural(m.Nouns, "noun")}, no more and no fewer.\n\n");
    }

    private static void Shape(StringBuilder report, ThemeMeasurements m) {
        report.Append("## Shape of the slug\n\n");
        report.Append(CultureInfo.InvariantCulture,
                      $"- {m.TwoWordAdjectives} of {m.TotalAdjectives} adjectives are written in more than one word\n");
        report.Append(CultureInfo.InvariantCulture,
                      $"- {m.TwoWordNouns} of {m.Nouns} nouns are\n");
        report.Append(CultureInfo.InvariantCulture,
                      $"- the longest slug this theme can produce carries {Plural(m.LongestSlugSegments, "segment")}, token aside\n\n");
        report.Append(WhatTheSegmentCountAssumes(m));
    }

    /// <summary>
    ///     The longest slug is counted with an adjective and a participle in front of the noun, which
    ///     is not what every theme draws: one left to a mode putting a single word there never
    ///     reaches that count, and one declaring no participle never assumed it.
    /// </summary>
    private static string WhatTheSegmentCountAssumes(ThemeMeasurements m) {
        if (m.Drawn.PutsAParticipleBesideAnAdjective()) { return "`--segment either` draws one word before the noun instead of two, if that is long for where the slug goes.\n\n"; }
        if (DeclaresNoParticiple(m)) { return "With no participle declared, that is the shape every mode draws: an adjective, then the noun.\n\n"; }

        return string.Create(CultureInfo.InvariantCulture,
                             $"That count puts an adjective and a participle in front of the noun, as `--segment both` does. Left to its own `segmentMode: {Spelled(m.Drawn)}`, this theme puts one word there, so its slugs carry fewer segments than that.\n\n");
    }

    /// <summary>Whether the theme declares no participle at all, in which case the report has none to speak of.</summary>
    private static bool DeclaresNoParticiple(ThemeMeasurements m) {
        return m.Participles is null;
    }

    /// <summary>
    ///     Whether the theme left to its own mode draws another shape of slug than the total counts.
    ///     Not a subset of it: one word in front of the noun makes a different slug from two, so
    ///     those are other slugs rather than fewer of the same. A theme declaring no participle draws
    ///     the one shape whatever the mode, so it never does.
    /// </summary>
    private static bool DrawsAnotherShapeLeftAlone(ThemeMeasurements m) {
        if (DeclaresNoParticiple(m)) { return false; }

        return m.Drawn != SegmentMode.Both;
    }

    private static void Combinations(StringBuilder report, ThemeMeasurements m) {
        report.Append("## Combinations\n\n");
        report.Append(EverySlugItCanProduce(m));

        if (DrawsAnotherShapeLeftAlone(m)) {
            report.Append(CultureInfo.InvariantCulture,
                          $"Left alone it draws `{Spelled(m.Drawn)}`: a different shape of slug, and {m.CombinationsDrawn:N0} of them rather than a subset of the figure above.\n\n");
        }

        // The same threshold as the per-category floor, which was set where Docker and Heroku
        // both reached for a suffix (DEC0003).
        const long suffixThreshold = ThemeValidator.MinimumCombinationsPerCategory;
        report.Append(m.CombinationsDrawn < suffixThreshold
                          ? string.Create(CultureInfo.InvariantCulture,
                                          $"Below {suffixThreshold:N0} — the point where Docker and Heroku both added a numeric suffix. A `tokenLength` in `defaults` is worth considering.\n\n")
                          : string.Create(CultureInfo.InvariantCulture,
                                          $"Above {suffixThreshold:N0}, so a suffix is a style choice here rather than a collision defence.\n\n"));
    }

    /// <summary>
    ///     The total names the words that make it up, and a theme declaring no participle has only
    ///     the adjective: every mode draws the same shape from it, so the total is all it produces.
    /// </summary>
    private static string EverySlugItCanProduce(ThemeMeasurements m) {
        if (DeclaresNoParticiple(m)) { return string.Create(CultureInfo.InvariantCulture, $"{m.TotalCombinations:N0} distinct slugs with an adjective in front. The theme declares no participle, so that is every slug it can produce, whatever `--segment` asks for.\n\n"); }

        return string.Create(CultureInfo.InvariantCulture,
                             $"{m.TotalCombinations:N0} distinct slugs with an adjective and a participle in front, which is what `--segment both` reaches.\n\n");
    }

    /// <summary>A mode as a theme file spells it, which is how the report must name it.</summary>
    private static string Spelled(SegmentMode mode) {
        return Spelling.Of(mode);
    }

    private static void Remarks(StringBuilder report, ThemeAnalysis analysis) {
        if (analysis.Remarks.Count == 0) { return; }

        report.Append("## Worth a second look\n\n");
        foreach (string remark in analysis.Remarks) {
            report.Append(CultureInfo.InvariantCulture, $"- {remark}\n");
        }

        report.Append('\n');
    }

    private static void Name(StringBuilder report, IReadOnlyList<string> offenders) {
        foreach (string offender in offenders.Take(MaxNamed)) {
            report.Append(CultureInfo.InvariantCulture, $"- `{offender}`\n");
        }

        if (offenders.Count > MaxNamed) {
            report.Append(CultureInfo.InvariantCulture, $"- *… and {offenders.Count - MaxNamed} more*\n");
        }

        report.Append('\n');
    }

    /// <summary>
    ///     The inverse of <see cref="Margin" />, for the one row that is a ceiling rather than a
    ///     floor: under it is the good side, so the sign flips and the bold goes to going over.
    /// </summary>
    private static string Headroom(long reached, long ceiling) {
        return reached > ceiling
            ? string.Create(CultureInfo.InvariantCulture, $"**{ceiling - reached:N0}**")
            : string.Create(CultureInfo.InvariantCulture, $"+{ceiling  - reached:N0}");
    }

    private static string Margin(long reached, long floor) {
        return reached < floor
            ? string.Create(CultureInfo.InvariantCulture, $"**{reached - floor:N0}**")
            : string.Create(CultureInfo.InvariantCulture, $"+{reached  - floor:N0}");
    }

    /// <param name="count">How many there are, which is what decides the form.</param>
    /// <param name="noun">The singular.</param>
    /// <param name="plural">
    ///     Written out where adding an "s" does not give it - "categorys" was printed for four
    ///     months. The irregular form belongs at the call site, where the word is chosen.
    /// </param>
    private static string Plural(int count, string noun, string? plural = null) {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{count:N0} {(count == 1 ? noun : plural ?? noun + "s")}");
    }

    #endregion

}