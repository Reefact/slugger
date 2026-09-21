using FirstClassErrors;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Slugger.Cli.Rendering;

/// <summary>
/// Turns a refusal into the report its reader acts on: every reason at once, in one place, so
/// the fix is one pass rather than one run per problem.
/// </summary>
/// <remarks>
/// It prints each reason's diagnostic message rather than describing the reason itself: the
/// wording is built once, in the factory that raised the error. A theme refused by --register
/// and the same theme refused at runtime therefore read identically, and so does a command line
/// with three typos in it - the tree is the same shape whatever raised it.
/// </remarks>
internal static class ReportRenderer
{
    /// <summary>How many offenders of one kind are named before the rest are counted instead.</summary>
    internal const int MaxNamedPerKind = 3;

    /// <summary>Renders the whole report, one line per element, ready to print.</summary>
    /// <param name="rejection">The error carrying every reason as its inner errors.</param>
    internal static IReadOnlyList<string> Render(Error rejection)
    {
        ArgumentNullException.ThrowIfNull(rejection);

        IReadOnlyList<Error> reasons = rejection.InnerErrors;
        if (reasons.Count == 0)
        {
            return [rejection.DiagnosticMessage];
        }

        List<string> lines =
        [
            reasons.Count == 1
                ? $"{Headline(rejection)} for 1 reason:"
                : $"{Headline(rejection)} for {reasons.Count} reasons:",
            string.Empty,
        ];

        lines.AddRange(Reasons(reasons));

        return lines;
    }

    /// <summary>
    /// The same report, drawn rather than printed: the headline in the colour of what it is, and
    /// every reason exactly as <see cref="Render"/> writes it.
    /// </summary>
    /// <param name="rejection">The error carrying every reason as its inner errors.</param>
    internal static IRenderable Draw(Error rejection) => Drawn(Render(rejection), Color.Red);

    /// <summary>One reason per line, the excess of a kind counted rather than listed.</summary>
    /// <param name="reasons">Every reason, grouped here by the code that raised it.</param>
    internal static IReadOnlyList<string> Reasons(IReadOnlyList<Error> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);

        List<string> lines = [];
        foreach (IGrouping<string, Error> kind in reasons.GroupBy(reason => reason.Code.ToString()))
        {
            Error[] sameKind = [.. kind];
            lines.AddRange(sameKind.Take(MaxNamedPerKind).Select(reason => $"  - {reason.DiagnosticMessage}"));

            int unnamed = sameKind.Length - MaxNamedPerKind;
            if (unnamed > 0)
            {
                lines.Add($"    ... and {unnamed} more of the same kind");
            }
        }

        return lines;
    }

    /// <summary>
    /// Lines already written, given a colour: the first carries the verdict, the rest are the
    /// reasons for it. Every one of them is escaped, because a theme name or a value that was
    /// typed may hold a square bracket and Spectre reads that as markup.
    /// </summary>
    /// <param name="lines">What <see cref="Render"/> or a caller like it produced.</param>
    /// <param name="verdict">The colour of the headline.</param>
    internal static IRenderable Drawn(IReadOnlyList<string> lines, Color verdict)
    {
        ArgumentNullException.ThrowIfNull(lines);

        return new Rows(lines.Select((line, index) => Draw(line, index, verdict)));
    }

    private static Markup Draw(string line, int index, Color verdict)
    {
        if (line.Length == 0)
        {
            // A space rather than nothing: a row with nothing in it is dropped, and the blank
            // line under the headline disappears with it (measured).
            return new Markup(" ");
        }

        string escaped = Markup.Escape(line);

        if (index == 0)
        {
            return new Markup(escaped, new Style(verdict, decoration: Decoration.Bold));
        }

        // The count that stands in for the reasons not listed is not itself a reason, and reads
        // as one at the same weight.
        return line.TrimStart().StartsWith("...", StringComparison.Ordinal)
            ? new Markup(escaped, new Style(decoration: Decoration.Dim))
            : new Markup(escaped);
    }

    /// <summary>The parent's own sentence, without the full stop a count is about to follow.</summary>
    private static string Headline(Error rejection) => rejection.DiagnosticMessage.TrimEnd('.');
}
