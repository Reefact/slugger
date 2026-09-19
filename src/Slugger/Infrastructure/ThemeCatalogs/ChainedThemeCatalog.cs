using Slugger.Application.Abstractions;
using Slugger.Domain;

namespace Slugger.Infrastructure.ThemeCatalogs;

/// <summary>
/// Resolution order for <c>--theme &lt;name&gt;</c>: the first catalog that has the name wins.
/// Composed as [theme directory, embedded] so that a custom file shadows the built-in theme
/// of the same name, which is the documented override rule.
/// </summary>
internal sealed class ChainedThemeCatalog : IThemeCatalog
{
    /// <param name="catalogs">The catalogs to consult, in order. The first that has a name wins.</param>
    public ChainedThemeCatalog(params IThemeCatalog[] catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        Catalogs = catalogs;
    }

    /// <summary>The catalogs consulted, in resolution order.</summary>
    public IReadOnlyList<IThemeCatalog> Catalogs { get; }

    /// <inheritdoc />
    public Theme? Find(string name)
    {
        foreach (IThemeCatalog catalog in Catalogs)
        {
            if (catalog.Find(name) is { } theme)
            {
                return theme;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListNames() => Catalogs
        .SelectMany(catalog => catalog.ListNames())
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();
}
