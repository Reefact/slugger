using Slugger.Domain;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger;

/// <summary>
/// The loading entry points a library consumer calls, and the single reference it needs:
/// <code>
/// var theme = Themes.LoadEmbedded("docker");
/// var slug  = SlugGenerator.Generate(theme, new GenerationOptions());
/// </code>
/// </summary>
/// <remarks>
/// These deliberately do not hang off <see cref="Theme"/> itself. Loading means JSON and the
/// file system, and letting the domain entity reach for either is the one thing the layering
/// exists to prevent. The ergonomics live here, at the outermost edge of the library, and
/// Slugger.Domain stays free of I/O.
/// </remarks>
public static class Themes
{
    /// <summary>Loads one of the themes compiled into the library: slugger, heroku or docker.</summary>
    public static Theme LoadEmbedded(string name) => throw new NotImplementedException();

    /// <summary>Loads a theme file. Its name is the file name without the extension.</summary>
    public static Theme LoadFromFile(string path) => throw new NotImplementedException();

    /// <param name="json">The theme document.</param>
    /// <param name="name">
    /// What to call it. Raw JSON has no file name to take it from, so the caller supplies one.
    /// </param>
    public static Theme LoadFromJson(string json, string name = "inline") => throw new NotImplementedException();

    /// <summary>The names of the themes compiled into the library.</summary>
    public static IReadOnlyList<string> ListEmbedded() => new EmbeddedThemeCatalog().ListNames();
}
