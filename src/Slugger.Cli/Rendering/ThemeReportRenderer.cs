using FirstClassErrors;
using Slugger.Domain;

namespace Slugger.Cli.Rendering;

/// <summary>
/// Turns a refused load into the report a theme author reads: every reason at once, in one
/// place, so the fix is one pass rather than one run per problem.
/// </summary>
/// <remarks>
/// It prints each reason's diagnostic message rather than describing the reason itself: the
/// wording is built once, in the factory that raises the error, which is what makes a theme
/// refused by <c>--register</c> and the same theme refused at runtime read identically.
/// </remarks>
public static class ThemeReportRenderer
{
    /// <summary>How many offenders of one kind are named before the rest are counted instead.</summary>
    public const int MaxNamedPerKind = 3;

    /// <summary>Renders the whole report, one line per element, ready to print.</summary>
    /// <param name="outcome">The load to report on.</param>
    public static IReadOnlyList<string> Render(Outcome<Theme> outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        if (outcome.Error is not { } rejection)
        {
            return [$"theme \"{outcome.GetResultOrThrow().Name}\" loaded."];
        }

        IReadOnlyList<Error> reasons = rejection.InnerErrors;
        List<string> lines =
        [
            reasons.Count == 1
                ? $"{rejection.DiagnosticMessage} for 1 reason:"
                : $"{rejection.DiagnosticMessage} for {reasons.Count} reasons:",
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
}
