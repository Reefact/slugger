#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TokenAlphabetError" /> raises, so a caller can catch this concept by its own name.</summary>
public sealed class TokenAlphabetException : DiagnosableException {

    #region Constructors & Destructor

    internal TokenAlphabetException(TokenAlphabetError error) : base(error) { }

    #endregion

}
