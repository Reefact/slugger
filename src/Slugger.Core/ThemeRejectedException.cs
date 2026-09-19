using Slugger.Domain.Validation;

namespace Slugger;

/// <summary>
/// Thrown by the convenience loaders when a theme is refused. It carries the whole report, not
/// just the complaint that happened to come first, so a caller that prefers exceptions loses
/// none of the detail a caller using <see cref="ThemeLoadResult"/> would have had.
/// </summary>
public sealed class ThemeRejectedException : Exception
{
    /// <param name="result">The refused load, with every reason.</param>
    public ThemeRejectedException(ThemeLoadResult result)
        : base(Describe(result))
    {
        ArgumentNullException.ThrowIfNull(result);
        Result = result;
    }

    /// <summary>The refused load, with every reason it was refused.</summary>
    public ThemeLoadResult Result { get; }

    /// <summary>Every reason the theme was refused.</summary>
    public IReadOnlyList<ThemeValidationError> Errors => Result.Errors;

    private static string Describe(ThemeLoadResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Errors.Count == 1
            ? $"Theme \"{result.ThemeName}\" was refused for 1 reason."
            : $"Theme \"{result.ThemeName}\" was refused for {result.Errors.Count} reasons.";
    }
}
