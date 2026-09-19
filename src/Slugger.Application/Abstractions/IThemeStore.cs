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

    void Save(string name, string json);

    void Delete(string name);
}
