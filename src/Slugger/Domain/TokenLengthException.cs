#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TokenLengthError" /> raises, so a caller can catch this concept by its own name.</summary>
/// <remarks>
///     <b>Not thrown by the engine yet.</b> It belongs to <see cref="TokenLength" />, part of an ongoing
///     refactoring of the library's vocabulary, and may change or disappear before loading or generation
///     use it. Do not build on it yet.
/// </remarks>
public sealed class TokenLengthException : DiagnosableException {

    #region Constructors & Destructor

    internal TokenLengthException(TokenLengthError error) : base(error) { }

    #endregion

}
