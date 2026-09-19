using FirstClassErrors;

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

    /// <summary>The parent's own sentence, without the full stop a count is about to follow.</summary>
    private static string Headline(Error rejection) => rejection.DiagnosticMessage.TrimEnd('.');
}
