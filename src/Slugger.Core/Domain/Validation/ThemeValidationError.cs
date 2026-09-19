namespace Slugger.Domain.Validation;

/// <summary>
/// One reason a theme was refused. The message is the same whether the theme was hit at
/// runtime or through --register: the spec asks for one error vocabulary, not two.
/// </summary>
public sealed record ThemeValidationError(ThemeValidationErrorCode Code, string Message);
