#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain.Analysis;

/// <summary>
///     Everything measured about a theme, and nothing written about it. The prose belongs to
///     whoever renders this - the CLI writes markdown from it, exactly as it writes a refusal's
///     sentence from the facts an error carries (DEC0006).
/// </summary>
/// <param name="Name">The theme the analysis is about.</param>
/// <param name="Refusals">Every reason a load would refuse it, empty when it would not.</param>
/// <param name="Remarks">What it may do and probably did not mean to; never a refusal.</param>
/// <param name="Measurements">The numbers, or null when the document could not be read at all.</param>
internal sealed record ThemeAnalysis(
    string                Name,
    IReadOnlyList<Error>  Refusals,
    IReadOnlyList<string> Remarks,
    ThemeMeasurements?    Measurements);