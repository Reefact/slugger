using FirstClassErrors;
using Slugger.Domain;

namespace Slugger.Application.Abstractions;

/// <summary>
/// Reads themes, whatever their origin. Satisfied by the embedded resources, by the theme
/// directory, and by the chain of the two where a custom file wins over the built-in theme
/// of the same name.
/// </summary>
/// <remarks>
/// Loading is split from having, because the two answer different questions and a caller needs
/// both: a theme this catalog does not carry is somebody else's to serve, while one it carries
/// and refuses has a report to hand back rather than a null.
/// </remarks>
internal interface IThemeCatalog
{
    /// <summary>Whether this catalog carries a theme of that name, valid or not.</summary>
    /// <param name="name">The theme to look for.</param>
    bool Contains(string name);

    /// <summary>Loads the theme, with every reason it was refused when it was.</summary>
    /// <param name="name">The theme to load.</param>
    /// <param name="allowSmall">Waive the minimum size rules for this load.</param>
    Outcome<Theme> Load(string name, bool allowSmall = false);

    /// <summary>Every theme name this catalog can serve, for <c>--list-themes</c>.</summary>
    IReadOnlyList<string> ListNames();
}
