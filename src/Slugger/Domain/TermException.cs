#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="TermError" /> raises, so a caller can catch this concept by its own name.</summary>
public sealed class TermException : DiagnosableException {

    #region Constructors & Destructor

    internal TermException(TermError error) : base(error) { }

    #endregion

}
