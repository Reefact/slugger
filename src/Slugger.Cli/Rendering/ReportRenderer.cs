#region Usings declarations

using Slugger.Domain.Errors;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Cli.Rendering;

/// <summary>
///     Turns a refusal into the report its reader acts on: every reason at once, in one place, so
///     the fix is one pass rather than one run per problem.
/// </summary>
/// <remarks>
///     It prints each reason's diagnostic message rather than describing the reason itself: the
///     wording is built once, in the factory that raised the error. A theme refused by --register
///     and the same theme refused at runtime therefore read identically, and so does a command line
///     with three typos in it - the tree is the same shape whatever raised it.
/// </remarks>
internal static class ReportRenderer {

    /// <summary>How many offenders of one kind are named before the rest are counted instead.</summary>
    internal const int MaxNamedPerKind = 3;

    #region Static members

    /// <summary>Renders the whole report, one line per element, ready to print.</summary>
    /// <param name="rejection">The error carrying every reason as its inner errors.</param>
    internal static IReadOnlyList<string> Render(Error rejection) {
        ArgumentNullException.ThrowIfNull(rejection);

        IReadOnlyList<Error> reasons = rejection.InnerErrors;
        if (reasons.Count == 0) { return [rejection.DiagnosticMessage]; }
        if (IsOnlyThatTheThemeIsNotThere(reasons)) { return [reasons[0].DiagnosticMessage]; }

        List<string> lines = [
            reasons.Count == 1
                ? $"{Headline(rejection)} for 1 reason:"
                : $"{Headline(rejection)} for {reasons.Count} reasons:",
            string.Empty
        ];

        lines.AddRange(Reasons(reasons));

        return lines;
    }

    /// <summary>
    ///     The same report, drawn rather than printed: the headline in the colour of what it is, and
    ///     every reason exactly as <see cref="Render" /> writes it.
    /// </summary>
    /// <param name="rejection">The error carrying every reason as its inner errors.</param>
    internal static IReadOnlyList<DrawnLine> Draw(Error rejection) {
        return Drawn(Render(rejection), Ink.Refusal);
    }

    /// <summary>One reason per line, the excess of a kind counted rather than listed.</summary>
    /// <param name="reasons">Every reason, grouped here by the code that raised it.</param>
    internal static IReadOnlyList<string> Reasons(IReadOnlyList<Error> reasons) {
        ArgumentNullException.ThrowIfNull(reasons);

        List<string> lines = [];
        foreach (IGrouping<string, Error> kind in reasons.GroupBy(reason => reason.Code.ToString())) {
            Error[] sameKind = [.. kind];
            lines.AddRange(sameKind.Take(MaxNamedPerKind).Select(reason => $"  - {reason.DiagnosticMessage}"));

            int unnamed = sameKind.Length - MaxNamedPerKind;
            if (unnamed > 0) {
                lines.Add($"    ... and {unnamed} more of the same kind");
            }
        }

        return lines;
    }

    /// <summary>
    ///     Lines already written, given a colour: the first carries the verdict, the rest are the
    ///     reasons for it.
    /// </summary>
    /// <param name="lines">What <see cref="Render" /> or a caller like it produced.</param>
    /// <param name="verdict">The ink of the headline.</param>
    internal static IReadOnlyList<DrawnLine> Drawn(IReadOnlyList<string> lines, Ink verdict) {
        ArgumentNullException.ThrowIfNull(lines);

        return [.. lines.Select((line, index) => new DrawnLine(line, InkOf(line, index, verdict)))];
    }

    private static Ink InkOf(string line, int index, Ink verdict) {
        if (index == 0) { return verdict; }

        // The count that stands in for the reasons not listed is not itself a reason, and reads
        // as one at the same weight.
        return line.TrimStart().StartsWith("...", StringComparison.Ordinal) ? Ink.Faint : Ink.Plain;
    }

    /// <summary>
    ///     Whether the one reason is that the theme is not there at all. Nothing was read, so nothing
    ///     was refused: the report is that sentence alone, rather than a headline announcing a
    ///     refusal for one reason.
    /// </summary>
    /// <param name="reasons">Every reason the error carries.</param>
    private static bool IsOnlyThatTheThemeIsNotThere(IReadOnlyList<Error> reasons) {
        if (reasons.Count != 1) { return false; }

        return reasons[0].Code == ThemeErrors.Codes.NotFound;
    }

    /// <summary>The parent's own sentence, without the full stop a count is about to follow.</summary>
    private static string Headline(Error rejection) {
        return rejection.DiagnosticMessage.TrimEnd('.');
    }

    #endregion

}