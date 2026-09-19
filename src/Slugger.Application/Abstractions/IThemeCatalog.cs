using Slugger.Domain;

namespace Slugger.Application.Abstractions;

/// <summary>
/// Reads themes, whatever their origin. Satisfied by the embedded resources, by the theme
/// directory, and by the chain of the two where a custom file wins over the built-in theme
/// of the same name.
/// </summary>
public interface IThemeCatalog
{
    /// <summary>The theme with this name, or null when this catalog has none.</summary>
    Theme? Find(string name);

    /// <summary>Every theme name this catalog can serve, for <c>--list-themes</c>.</summary>
    IReadOnlyList<string> ListNames();
}
