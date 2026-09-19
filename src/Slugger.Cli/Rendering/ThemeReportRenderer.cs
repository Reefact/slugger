using FirstClassErrors;
using Slugger.Domain;

namespace Slugger.Cli.Rendering;

/// <summary>Reports on a theme that was loaded, or on every reason it was refused.</summary>
internal static class ThemeReportRenderer
{
    /// <summary>How many offenders of one kind are named before the rest are counted instead.</summary>
    internal const int MaxNamedPerKind = ReportRenderer.MaxNamedPerKind;

    /// <summary>Renders the whole report, one line per element, ready to print.</summary>
    /// <param name="outcome">The load to report on.</param>
    internal static IReadOnlyList<string> Render(Outcome<Theme> outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return outcome.Error is { } rejection
            ? ReportRenderer.Render(rejection)
            : [$"theme \"{outcome.GetResultOrThrow().Name}\" loaded."];
    }
}
