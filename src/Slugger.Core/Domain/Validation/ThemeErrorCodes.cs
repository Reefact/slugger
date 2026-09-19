using FirstClassErrors;

namespace Slugger.Domain.Validation;

/// <summary>
/// The identifier of every way a theme can be refused, declared once so that a caller
/// branching on a situation - or a test asserting one - references a constant the compiler
/// resolves rather than a string nothing validates.
/// </summary>
public static class ThemeErrorCodes
{
    /// <summary>See <see cref="ThemeErrors.Rejected"/>.</summary>
    public static readonly ErrorCode Rejected = ErrorCode.Create("THEME_REJECTED");

    /// <summary>See <see cref="ThemeErrors.MalformedJson"/>.</summary>
    public static readonly ErrorCode MalformedJson = ErrorCode.Create("THEME_MALFORMED_JSON");

    /// <summary>See <see cref="ThemeErrors.MalformedSection"/>.</summary>
    public static readonly ErrorCode MalformedSection = ErrorCode.Create("THEME_MALFORMED_SECTION");

    /// <summary>See <see cref="ThemeErrors.MalformedNoun"/>.</summary>
    public static readonly ErrorCode MalformedNoun = ErrorCode.Create("THEME_MALFORMED_NOUN");

    /// <summary>See <see cref="ThemeErrors.UnknownCategory"/>.</summary>
    public static readonly ErrorCode UnknownCategory = ErrorCode.Create("THEME_UNKNOWN_CATEGORY");

    /// <summary>See <see cref="ThemeErrors.TooFewNouns"/>.</summary>
    public static readonly ErrorCode TooFewNouns = ErrorCode.Create("THEME_TOO_FEW_NOUNS");

    /// <summary>See <see cref="ThemeErrors.PoolTooSmall"/>.</summary>
    public static readonly ErrorCode PoolTooSmall = ErrorCode.Create("THEME_POOL_TOO_SMALL");

    /// <summary>See <see cref="ThemeErrors.CategoryTooPoor"/>.</summary>
    public static readonly ErrorCode CategoryTooPoor = ErrorCode.Create("THEME_CATEGORY_TOO_POOR");

    /// <summary>See <see cref="ThemeErrors.ParticiplesRequestedButAbsent"/>.</summary>
    public static readonly ErrorCode ParticiplesRequestedButAbsent = ErrorCode.Create("THEME_PARTICIPLES_ABSENT");
}
