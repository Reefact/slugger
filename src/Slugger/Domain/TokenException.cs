#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TokenError" /> raises, so a caller can catch this concept by its own name.</summary>
/// <remarks>
///     Built by <c>TokenError.ToException()</c> and by nothing else, which is why its constructor is
///     internal: an exception of this type carries an error of that one, always.
/// </remarks>
public sealed class TokenException : DiagnosableException {

    #region Constructors & Destructor

    internal TokenException(TokenError error) : base(error) { }

    #endregion

}
