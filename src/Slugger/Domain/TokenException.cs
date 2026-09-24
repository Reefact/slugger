#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TokenError" /> raises, so a caller can catch this concept by its own name.</summary>
public sealed class TokenException : DiagnosableException {

    #region Constructors & Destructor

    internal TokenException(TokenError error) : base(error) { }

    #endregion

}
