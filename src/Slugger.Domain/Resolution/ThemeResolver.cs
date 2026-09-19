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
public sealed class ThemeResolver
{
    public ThemeResolver(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Theme = theme;
    }

    public Theme Theme { get; }

    /// <summary>The adjectives reachable from this noun. Empty when the noun carries no category.</summary>
    public IReadOnlyList<string> Pool(Noun noun) => throw new NotImplementedException();

    /// <summary>The participles reachable from this noun. Empty when the theme declares none for its categories.</summary>
    public IReadOnlyList<string> ParticiplePool(Noun noun) => throw new NotImplementedException();
}
