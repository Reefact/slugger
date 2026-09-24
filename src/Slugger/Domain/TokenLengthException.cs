#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TokenLengthError" /> raises, so a caller can catch this concept by its own name.</summary>
public sealed class TokenLengthException : DiagnosableException {

    #region Constructors & Destructor

    internal TokenLengthException(TokenLengthError error) : base(error) { }

    #endregion

}
