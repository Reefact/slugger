#region Usings declarations

using Slugger.Errors;

#endregion

namespace Slugger;

/// <summary>
///     What a refused theme raises: the <c>OrThrow</c> forms of <see cref="SluggerTheme" />'s loads and of
///     <see cref="SlugGenerator" />. Catch it by name; every reason is in the inner errors of
///     <see cref="DiagnosableException.Error" />.
/// </summary>
public sealed class SluggerThemeException : DiagnosableException {

    #region Constructors & Destructor

    internal SluggerThemeException(SluggerThemeError error) : base(error) { }

    #endregion

}
