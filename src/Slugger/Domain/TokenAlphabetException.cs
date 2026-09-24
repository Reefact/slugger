#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TokenAlphabetError" /> raises, so a caller can catch this concept by its own name.</summary>
/// <remarks>
///     Built by <c>TokenAlphabetError.ToException()</c> and by nothing else, which is why its constructor is
///     internal: an exception of this type carries an error of that one, always.
/// </remarks>
public sealed class TokenAlphabetException : DiagnosableException {

    #region Constructors & Destructor

    internal TokenAlphabetException(TokenAlphabetError error) : base(error) { }

    #endregion

}
