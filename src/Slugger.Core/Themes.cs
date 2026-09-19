using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Serialization;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger;

/// <summary>
/// The loading entry points a library consumer calls, and the single reference it needs:
/// <code>
/// Theme  theme = Themes.LoadEmbedded("docker");
/// string slug  = SlugGenerator.Generate(theme, new GenerationOptions());
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// These deliberately do not hang off <see cref="Theme"/> itself. Loading means JSON and the
/// file system, and letting the domain entity reach for either is the one thing the layering
/// exists to prevent. The ergonomics live here, at the outermost edge of the library, and
/// Slugger.Domain stays free of I/O.
/// </para>
/// <para>
/// Each entry point comes in two shapes. <c>Load*</c> returns a
/// <see cref="ThemeLoadResult"/> carrying <b>every</b> reason a theme was refused, which is
/// what a CLI needs to print one complete report. The throwing shape is the convenience the
/// spec sketches, and its exception carries the same full report rather than only the first
/// complaint.
/// </para>
/// </remarks>
public static class Themes
{
    /// <summary>Loads one of the themes compiled into the library, reporting everything wrong with it.</summary>
    /// <param name="name">slugger, heroku or docker.</param>
    /// <param name="allowSmall">Waive the minimum size rules for this load.</param>
    public static ThemeLoadResult LoadEmbeddedResult(string name, bool allowSmall = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using Stream? stream = EmbeddedThemeCatalog.OpenStream(name);
        if (stream is null)
        {
            return ThemeLoadResult.Rejected(
                name,
                [new ThemeValidationError.MalformedSection("(file)", $"a theme embedded in the library; there is none called \"{name}\"")]);
        }

        using StreamReader reader = new(stream);

        return LoadFromJsonResult(reader.ReadToEnd(), name, allowSmall);
    }

    /// <summary>Loads a theme file, reporting everything wrong with it.</summary>
    /// <param name="path">The file to read. Its name without the extension becomes the theme name.</param>
    /// <param name="allowSmall">Waive the minimum size rules for this load.</param>
    public static ThemeLoadResult LoadFromFileResult(string path, bool allowSmall = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string name = Path.GetFileNameWithoutExtension(path.AsSpan()).ToString();
        if (name.Length == 0)
        {
            name = "(unnamed)";
        }

        if (!File.Exists(path))
        {
            return ThemeLoadResult.Rejected(
                name,
                [new ThemeValidationError.MalformedSection("(file)", $"a readable file; \"{path}\" does not exist")]);
        }

        return LoadFromJsonResult(File.ReadAllText(path), name, allowSmall);
    }

    /// <summary>Loads a theme document, reporting everything wrong with it.</summary>
    /// <param name="json">The theme document.</param>
    /// <param name="name">What to call it. Raw JSON has no file name to take it from, so the caller supplies one.</param>
    /// <param name="allowSmall">Waive the minimum size rules for this load.</param>
    public static ThemeLoadResult LoadFromJsonResult(string json, string name = "inline", bool allowSmall = false)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ThemeParseResult parsed = new JsonThemeSerializer().Deserialize(name, json);
        if (parsed.Theme is not { } theme)
        {
            return ThemeLoadResult.Rejected(name, parsed.ShapeErrors);
        }

        // The shape complaints and the rule failures are reported together: fixing four
        // malformed sections only to be told about twenty rule failures on the next run is
        // the same "stops at the first error" the pipeline exists to avoid, one stage up.
        List<ThemeValidationError> errors = [.. parsed.ShapeErrors];
        if (parsed.RulesCanRun)
        {
            errors.AddRange(ThemeValidator.Validate(theme, allowSmall).Errors);
        }

        return errors.Count == 0 ? ThemeLoadResult.Loaded(theme) : ThemeLoadResult.Rejected(name, errors);
    }

    /// <summary>Loads one of the themes compiled into the library: slugger, heroku or docker.</summary>
    /// <param name="name">The theme to load.</param>
    /// <exception cref="ThemeRejectedException">The theme was refused; the exception carries every reason.</exception>
    public static Theme LoadEmbedded(string name) => LoadEmbeddedResult(name).OrThrow();

    /// <summary>Loads a theme file. Its name is the file name without the extension.</summary>
    /// <param name="path">The file to read.</param>
    /// <exception cref="ThemeRejectedException">The theme was refused; the exception carries every reason.</exception>
    public static Theme LoadFromFile(string path) => LoadFromFileResult(path).OrThrow();

    /// <summary>Loads a theme document.</summary>
    /// <param name="json">The theme document.</param>
    /// <param name="name">What to call it.</param>
    /// <exception cref="ThemeRejectedException">The theme was refused; the exception carries every reason.</exception>
    public static Theme LoadFromJson(string json, string name = "inline") => LoadFromJsonResult(json, name).OrThrow();

    /// <summary>The names of the themes compiled into the library.</summary>
    public static IReadOnlyList<string> ListEmbedded() => new EmbeddedThemeCatalog().ListNames();

    private static Theme OrThrow(this ThemeLoadResult result) =>
        result.Theme ?? throw new ThemeRejectedException(result);
}
