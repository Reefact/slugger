using System.Reflection;
using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Serialization;

namespace Slugger.Infrastructure.ThemeCatalogs;

/// <summary>
/// The themes compiled into the assembly, so that slugger works the moment it is installed
/// with nothing to set up. They carry no privilege: a file of the same name in the theme
/// directory shadows one, and they take part in the weighted draw like any other theme.
/// </summary>
internal sealed class EmbeddedThemeCatalog : IThemeCatalog
{
    private const string ResourcePrefix = "Slugger.Themes.";
    private const string ResourceSuffix = ".json";

    private readonly StringInternPool? _pool;

    /// <param name="pool">The run's shared intern pool, when there is one.</param>
    internal EmbeddedThemeCatalog(StringInternPool? pool = null) => _pool = pool;

    private static Assembly ResourceAssembly => typeof(EmbeddedThemeCatalog).Assembly;

    /// <inheritdoc />
    public bool Contains(string name) => OpenStream(name) is { } stream && Closed(stream);

    /// <inheritdoc />
    public Outcome<Theme> Load(string name, bool allowSmall = false)
    {
        using Stream? stream = OpenStream(name);
        if (stream is null)
        {
            return ThemeLoader.Refuse(name, [ThemeErrors.NotFound(name, ListNames())]);
        }

        using StreamReader reader = new(stream);

        return ThemeLoader.Load(name, reader.ReadToEnd(), allowSmall, _pool);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListNames() => ResourceAssembly
        .GetManifestResourceNames()
        .Where(resource => resource.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                           && resource.EndsWith(ResourceSuffix, StringComparison.Ordinal))
        .Select(resource => resource[ResourcePrefix.Length..^ResourceSuffix.Length])
        .Order(StringComparer.Ordinal)
        .ToArray();

    /// <summary>The raw JSON of a built-in theme, or null when no such theme is embedded.</summary>
    /// <param name="name">The theme to open.</param>
    internal static Stream? OpenStream(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ResourceAssembly.GetManifestResourceStream($"{ResourcePrefix}{name}{ResourceSuffix}");
    }

    private static bool Closed(Stream stream)
    {
        stream.Dispose();

        return true;
    }
}
