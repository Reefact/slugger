using System.Reflection;
using Slugger.Application.Abstractions;
using Slugger.Domain;

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

    private static Assembly ResourceAssembly => typeof(EmbeddedThemeCatalog).Assembly;

    /// <inheritdoc />
    public Theme? Find(string name) => throw new NotImplementedException();

    /// <inheritdoc />
    public IReadOnlyList<string> ListNames() => ResourceAssembly
        .GetManifestResourceNames()
        .Where(resource => resource.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                           && resource.EndsWith(ResourceSuffix, StringComparison.Ordinal))
        .Select(resource => resource[ResourcePrefix.Length..^ResourceSuffix.Length])
        .Order(StringComparer.Ordinal)
        .ToArray();

    /// <summary>The raw JSON of a built-in theme, or null when no such theme is embedded.</summary>
    public static Stream? OpenStream(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ResourceAssembly.GetManifestResourceStream($"{ResourcePrefix}{name}{ResourceSuffix}");
    }
}
