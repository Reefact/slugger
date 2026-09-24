#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="WordError" /> raises, so a caller can catch this concept by its own name.</summary>
public sealed class WordException : DiagnosableException {

    #region Constructors & Destructor

    internal WordException(WordError error) : base(error) { }

    #endregion

}
