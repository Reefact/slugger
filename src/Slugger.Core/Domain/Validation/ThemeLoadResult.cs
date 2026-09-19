namespace Slugger.Domain.Validation;

/// <summary>
/// What came back from trying to load a theme: the theme, or every reason it was refused.
/// </summary>
/// <remarks>
/// A deliberately small result type rather than a general-purpose one from a library. What
/// matters here is not the container but the pipeline behind it: parsing collects every
/// malformed section before giving up, and validation runs every rule over every noun and
/// every category, so one run reports everything wrong with a file instead of one thing per
/// run.
/// </remarks>
public sealed record ThemeLoadResult
{
    private ThemeLoadResult(string themeName, Theme? theme, IReadOnlyList<ThemeValidationError> errors)
    {
        ThemeName = themeName;
        Theme = theme;
        Errors = errors;
    }

    /// <summary>The name the theme was loaded under, which is the file name rather than a field inside it.</summary>
    public string ThemeName { get; }

    /// <summary>The theme, when it loaded. Null when it was refused.</summary>
    public Theme? Theme { get; }

    /// <summary>Every reason it was refused, in the order the stages ran. Empty when it loaded.</summary>
    public IReadOnlyList<ThemeValidationError> Errors { get; }

    /// <summary>Whether the theme may be used.</summary>
    public bool IsLoaded => Theme is not null;

    /// <summary>A theme that cleared parsing and every rule.</summary>
    /// <param name="theme">The loaded theme.</param>
    public static ThemeLoadResult Loaded(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return new ThemeLoadResult(theme.Name, theme, []);
    }

    /// <summary>A theme that was refused, with everything that is wrong with it.</summary>
    /// <param name="themeName">What the theme would have been called.</param>
    /// <param name="errors">Every reason, not just the first.</param>
    public static ThemeLoadResult Rejected(string themeName, IReadOnlyList<ThemeValidationError> errors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeName);
        ArgumentNullException.ThrowIfNull(errors);

        return new ThemeLoadResult(themeName, null, errors);
    }
}
