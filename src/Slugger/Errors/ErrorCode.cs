#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Errors;

/// <summary>
///     What kind of refusal an error is, such as <c>THEME_TOO_FEW_NOUNS</c>: the same for every
///     occurrence of it, and stable from one version to the next where a message is not.
/// </summary>
/// <remarks>
///     Every code a caller can receive is a constant of the catalogue that produces it -
///     <see cref="SluggerThemeError.Codes" /> and the <c>Codes</c> of each concept's error - so
///     a caller compares with one of those rather than with a string.
/// </remarks>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class ErrorCode : ValueType<ErrorCode> {

    #region Static members

    /// <summary>The code spelled that way.</summary>
    /// <param name="code">The spelling, such as <c>THEME_TOO_FEW_NOUNS</c>.</param>
    internal static ErrorCode Create(string code) {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new ErrorCode(code);
    }

    #endregion

    #region Fields

    private readonly string _code;

    #endregion

    #region Constructors & Destructor

    private ErrorCode(string code) {
        _code = code;
    }

    #endregion

    /// <summary>The spelling, for a caller that writes the code somewhere, such as a log.</summary>
    [DehydrationMethod]
    public string Dehydrate() {
        return _code;
    }

    /// <summary>The spelling, for a human reading a watch window.</summary>
    public override string ToString() {
        return _code;
    }

    /// <summary>Two codes spelled the same are the same kind of refusal.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _code;
    }

}
