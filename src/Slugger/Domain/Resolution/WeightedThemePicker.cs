using DiagnosticCatalog.Sonar;
using System.Diagnostics.CodeAnalysis;

namespace Slugger.Domain.Resolution;

/// <summary>
/// Picks one theme among several, proportionally to its noun count, without concatenating
/// their noun lists: a cumulative count array plus a binary search. Mathematically identical
/// to drawing uniformly from a flattened list (every noun keeps probability 1/total), but
/// O(D) extra memory for D themes instead of O(N) for N nouns.
/// </summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = SuppressionJustifications.ScaffoldedStub)]
public sealed class WeightedThemePicker
{
    /// <param name="themes">The themes in scope, each weighted by how many nouns it holds.</param>
    public WeightedThemePicker(IReadOnlyList<Theme> themes)
    {
        ArgumentNullException.ThrowIfNull(themes);
        Themes = themes;
    }

    /// <summary>The themes in scope.</summary>
    public IReadOnlyList<Theme> Themes { get; }

    /// <summary>Draws one theme, with a probability proportional to its share of the nouns.</summary>
    /// <param name="random">Where the draw comes from.</param>
    public Theme Pick(IRandomSource random) => throw new NotImplementedException();
}
