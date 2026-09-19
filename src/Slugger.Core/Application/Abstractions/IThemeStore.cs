namespace Slugger.Application.Abstractions;

/// <summary>
/// Writes to the theme directory: the file side of <c>--register</c> and <c>--unregister</c>.
/// Split from <see cref="IThemeCatalog"/> because reading is needed everywhere and writing
/// only by two commands.
/// </summary>
public interface IThemeStore
{
    /// <summary>Whether a custom file of that name already exists. Nothing is ever overwritten silently.</summary>
    bool Contains(string name);

    /// <summary>Writes a validated theme into the theme directory.</summary>
    /// <param name="name">The theme name, which becomes the file name.</param>
    /// <param name="json">The theme document, exactly as it was validated.</param>
    void Save(string name, string json);

    /// <summary>Removes a custom theme.</summary>
    /// <param name="name">The theme to remove. Only a file can be removed, never a built-in theme.</param>
    void Delete(string name);
}
