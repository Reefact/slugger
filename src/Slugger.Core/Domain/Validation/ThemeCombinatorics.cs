using DiagnosticCatalog.Sonar;
using System.Diagnostics.CodeAnalysis;

namespace Slugger.Domain.Validation;

/// <summary>
/// <code>
/// combos(noun)     = |pool(noun)| * max(1, |partPool(noun)|)
/// combos(category) = sum of combos(noun) over every noun carrying that category
/// total            = sum of combos(noun) over every noun
/// </code>
/// The max(1, ...) keeps a noun with no participle from zeroing the count. A noun in several
/// categories contributes to each: this is a coverage check per branch, not a partition.
/// The participle is counted whether or not segmentMode ends up using it, because
/// segmentMode changes behaviour, not the theme's real combinatorial space.
/// </summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = "Scaffolding: the three bodies still throw, so none reads Theme yet. Every one of them counts over its nouns.")]
public sealed class ThemeCombinatorics
{
    /// <param name="theme">The theme whose combinations are counted.</param>
    public ThemeCombinatorics(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Theme = theme;
    }

    /// <summary>The theme being counted.</summary>
    public Theme Theme { get; }

    /// <summary>How many distinct slugs this one noun can produce.</summary>
    /// <param name="noun">The noun to count for.</param>
    public long CombinationsFor(Noun noun) => throw new NotImplementedException();

    /// <summary>How many distinct slugs the nouns carrying this category can produce between them.</summary>
    /// <param name="category">The category to count for.</param>
    public long CombinationsForCategory(string category) => throw new NotImplementedException();

    /// <summary>The theme's whole combinatorial space, summed over every noun.</summary>
    public long Total() => throw new NotImplementedException();
}
