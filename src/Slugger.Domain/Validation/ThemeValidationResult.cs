namespace Slugger.Domain.Validation;

public sealed record ThemeValidationResult(IReadOnlyList<ThemeValidationError> Errors)
{
    public static ThemeValidationResult Valid { get; } = new([]);

    public bool IsValid => Errors.Count == 0;
}
