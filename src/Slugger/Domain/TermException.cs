#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TermError" /> raises, so a caller can catch this concept by its own name.</summary>
/// <remarks>
///     Built by <c>TermError.ToException()</c> and by nothing else, which is why its constructor is
///     internal: an exception of this type carries an error of that one, always.
/// </remarks>
public sealed class TermException : DiagnosableException {

    #region Constructors & Destructor

    internal TermException(TermError error) : base(error) { }

    #endregion

}
