#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>What <see cref="ThemeNameError" /> raises, so a caller can catch this concept by its own name.</summary>
public sealed class ThemeNameException : DiagnosableException {

    #region Constructors & Destructor

    internal ThemeNameException(ThemeNameError error) : base(error) { }

    #endregion

}
