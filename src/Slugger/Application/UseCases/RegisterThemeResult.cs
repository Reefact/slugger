#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Application.UseCases;

/// <summary>
///     What registering produced: the outcome, the name it went under, and whether that name now
///     shadows a built-in theme - allowed, but never silently, so the caller can say so.
/// </summary>
/// <param name="Outcome">Success, or every reason the theme was refused.</param>
/// <param name="Name">The theme name, taken from the file name.</param>
/// <param name="Shadows">Whether a built-in theme of the same name is now overridden.</param>
/// <param name="Remarks">What the theme may do and probably did not mean to; never a refusal.</param>
internal sealed record RegisterThemeResult(
    Outcome                Outcome,
    string                 Name,
    bool                   Shadows,
    IReadOnlyList<string>? Remarks = null);