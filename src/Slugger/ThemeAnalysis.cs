#region Usings declarations

using Slugger.Domain.Analysis;

#endregion

namespace Slugger;

/// <summary>
///     A theme measured whether or not it would load: every reason it would be refused, what it may do
///     and probably did not mean to, and its measurements. <see cref="SluggerTheme" />'s <c>Analyze*</c>
///     methods return one, and never throw for the theme's sake.
///     <code>
/// ThemeAnalysis analysis = SluggerTheme.AnalyzeFromJson(json, "rivers");
///
/// foreach (ThemeMargin margin in analysis.Measurements?.Margins ?? []) {
///     Console.WriteLine($"{margin.Rule}: {margin.WorstCase} against {margin.Limit} ({margin.Margin:+#;-#;0})");
/// }
/// </code>
/// </summary>
/// <remarks>
///     <para>
///         It measures the theme as it describes itself - its own style, its own segment mode - which is
///         what a load holds it to. A refused theme is measured all the same, as long as it can be read:
///         that is the case an author needs most.
///     </para>
///     <para>
///         See decision records DEC0014 and DEC0038 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0014-rapport-d-analyse-d-un-theme.md
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0038-mesure-publique-d-un-theme.md
///     </para>
/// </remarks>
public sealed class ThemeAnalysis {

    #region Constructors & Destructor

    internal ThemeAnalysis(ThemeAnalysisFacts facts) {
        ArgumentNullException.ThrowIfNull(facts);

        Name         = facts.Name;
        Refusals     = [.. facts.Refusals.OfType<SluggerThemeError>()];
        Remarks      = facts.Remarks;
        FloorsWaived = facts.FloorsWaived;
        IsAccepted   = facts.Read && Refusals.Count == 0;
        Measurements = facts.Measurements is { } measured ? new ThemeMeasurements(measured) : null;
    }

    #endregion

    /// <summary>The theme's name: the file's, without its extension, or the one the caller gave.</summary>
    public string Name { get; }

    /// <summary>Whether a load would accept the theme, with the same floors waived or not.</summary>
    public bool IsAccepted { get; }

    /// <summary>
    ///     Every reason a load would refuse the theme, each with its code and its sentence - the same
    ///     errors a load reports in its <see cref="SluggerThemeError.Codes.Rejected" />. Empty when it
    ///     would be accepted.
    /// </summary>
    public IReadOnlyList<SluggerThemeError> Refusals { get; }

    /// <summary>
    ///     Whether the theme is accepted only because its size floors are waived - by <c>allowSmall</c> or
    ///     by its own <c>"allowSmall": true</c>. Its margins still compare it with the real floors.
    /// </summary>
    public bool FloorsWaived { get; }

    /// <summary>What the theme may do and probably did not mean to, as sentences. Never a refusal.</summary>
    public IReadOnlyList<string> Remarks { get; }

    /// <summary>
    ///     The measurements, or null when the theme left nothing to measure: a file that is not valid
    ///     JSON, a section of the wrong shape, or no noun at all.
    /// </summary>
    public ThemeMeasurements? Measurements { get; }

}
