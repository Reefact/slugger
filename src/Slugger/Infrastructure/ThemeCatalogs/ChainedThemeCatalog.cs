using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Serialization;

namespace Slugger.Infrastructure.ThemeCatalogs;

/// <summary>
/// Resolution order for <c>--theme &lt;name&gt;</c>: the first catalog that carries the name
/// serves it. Composed as [theme directory, embedded] so that a custom file shadows the
/// built-in theme of the same name, which is the documented override rule.
/// </summary>
/// <remarks>
/// A catalog that carries the name serves it even when the theme turns out to be invalid - the
/// chain stops there rather than falling through. Otherwise a typo in a custom docker.json would
/// silently hand back the built-in one, and the author would never learn their file is broken.
/// </remarks>
internal sealed class ChainedThemeCatalog : IThemeCatalog
{
    /// <param name="catalogs">The catalogs to consult, in order.</param>
    internal ChainedThemeCatalog(params IThemeCatalog[] catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        Catalogs = catalogs;
    }

    /// <summary>The catalogs consulted, in resolution order.</summary>
    internal IReadOnlyList<IThemeCatalog> Catalogs { get; }

    /// <inheritdoc />
    public bool Contains(string name) => Catalogs.Any(catalog => catalog.Contains(name));

    /// <inheritdoc />
    public Outcome<Theme> Load(string name, bool allowSmall = false)
    {
        IThemeCatalog? holder = Catalogs.FirstOrDefault(catalog => catalog.Contains(name));

        return holder is null
            ? ThemeLoader.Refuse(name, [ThemeErrors.NotFound(name, ListNames())])
            : holder.Load(name, allowSmall);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListNames() => Catalogs
        .SelectMany(catalog => catalog.ListNames())
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();
}
