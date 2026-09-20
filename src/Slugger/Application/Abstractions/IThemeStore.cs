using FirstClassErrors;
using Slugger.Domain;

namespace Slugger.Application.Abstractions;

/// <summary>
/// The theme directory as a place on disk: what <c>--register</c> and <c>--unregister</c> read
/// and write. Split from <see cref="IThemeCatalog"/>, which resolves a theme by name wherever
/// it lives; this one deals in files, including one handed to it by path.
/// </summary>
internal interface IThemeStore
{
    /// <summary>Whether a custom file of that name already exists. Nothing is ever overwritten silently.</summary>
    /// <param name="name">The theme to look for.</param>
    bool Contains(string name);

    /// <summary>Loads and validates a theme file at an arbitrary path, the way a runtime load would.</summary>
    /// <param name="path">The file to read.</param>
    /// <param name="allowSmall">Waive the minimum size rules for this load.</param>
    Outcome<Theme> LoadFile(string path, bool allowSmall = false);

    /// <summary>The raw text of a file, so --register copies what it validated rather than re-serialising it.</summary>
    /// <param name="path">The file to read.</param>
    string ReadFileText(string path);

    /// <summary>
    /// Writes a file at an arbitrary path, the mirror of <see cref="ReadFileText"/>. Used for a
    /// derived file that belongs beside its source rather than in the theme directory - an
    /// analysis report next to the theme it measured.
    /// </summary>
    /// <param name="path">Where to write.</param>
    /// <param name="content">What to write. An existing file is replaced: a report is computed,
    /// not authored, so there is nothing of the author's to lose.</param>
    void WriteFileText(string path, string content);

    /// <summary>Writes a validated theme into the theme directory.</summary>
    /// <param name="name">The theme name, which becomes the file name.</param>
    /// <param name="json">The theme document, exactly as it was validated.</param>
    void Save(string name, string json);

    /// <summary>Removes a custom theme.</summary>
    /// <param name="name">The theme to remove. Only a file can be removed, never a built-in theme.</param>
    void Delete(string name);
}
