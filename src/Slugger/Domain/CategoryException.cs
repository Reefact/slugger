#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="CategoryError" /> raises, so a caller can catch this concept by its own name.</summary>
public sealed class CategoryException : DiagnosableException {

    #region Constructors & Destructor

    internal CategoryException(CategoryError error) : base(error) { }

    #endregion

}
