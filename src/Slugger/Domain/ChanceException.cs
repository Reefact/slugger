#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="ChanceError" /> raises, so a caller can catch this concept by its own name.</summary>
public sealed class ChanceException : DiagnosableException {

    #region Constructors & Destructor

    internal ChanceException(ChanceError error) : base(error) { }

    #endregion

}
