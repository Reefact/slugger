#region Usings declarations

using Slugger.Domain.Errors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     What a refused theme raises: the throwing forms of <see cref="Themes" />, and of
///     <see cref="Generation.SlugSource" /> and <see cref="Generation.SlugGenerator" /> when the options
///     leave a theme nothing to draw. Catch it by name; every reason is in the inner errors of
///     <see cref="DiagnosableException.Error" />.
/// </summary>
public sealed class SluggerThemeException : DiagnosableException {

    #region Constructors & Destructor

    internal SluggerThemeException(SluggerThemeError error) : base(error) { }

    #endregion

}
