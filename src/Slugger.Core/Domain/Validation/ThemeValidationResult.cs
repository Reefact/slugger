namespace Slugger.Domain.Validation;

/// <summary>
/// What a theme was refused for, or nothing at all. Several errors are reported together
/// rather than one at a time, so that fixing a theme file does not mean rebuilding it one
/// rejection per run.
/// </summary>
/// <param name="Errors">Every reason the theme was refused, in the order the rules ran.</param>
public sealed record ThemeValidationResult(IReadOnlyList<ThemeValidationError> Errors)
{
    /// <summary>A theme that cleared every rule.</summary>
    public static ThemeValidationResult Valid { get; } = new([]);

    /// <summary>Whether the theme may be used.</summary>
    public bool IsValid => Errors.Count == 0;
}
