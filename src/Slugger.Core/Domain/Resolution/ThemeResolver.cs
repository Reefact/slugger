using DiagnosticCatalog.Sonar;
using System.Diagnostics.CodeAnalysis;

namespace Slugger.Domain.Resolution;

/// <summary>
/// The category algebra of a single theme.
/// <code>
/// pool(noun)     = { adj | adj.categories inter noun.categories is not empty }
/// partPool(noun) = union of participles[c] for c in noun.categories
/// </code>
/// Both are resolved strictly inside one theme, never across files, even when two files
/// happen to use the same category name.
/// </summary>
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = "Scaffolding: both bodies still throw, so neither reads Theme yet. Resolving a pool is exactly a walk over Theme.Adjectives and Theme.Participles.")]
public sealed class ThemeResolver
{
    /// <param name="theme">The single theme every resolution stays inside.</param>
    public ThemeResolver(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Theme = theme;
    }

    /// <summary>The theme being resolved.</summary>
    public Theme Theme { get; }

    /// <summary>The adjectives reachable from this noun. Empty when the noun carries no category.</summary>
    public IReadOnlyList<string> Pool(Noun noun) => throw new NotImplementedException();

    /// <summary>The participles reachable from this noun. Empty when the theme declares none for its categories.</summary>
    public IReadOnlyList<string> ParticiplePool(Noun noun) => throw new NotImplementedException();
}
