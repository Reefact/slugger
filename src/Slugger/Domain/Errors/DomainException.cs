namespace Slugger.Domain.Errors;

/// <summary>
///     What a <see cref="DomainError" /> raises: thrown by the forms of <see cref="Themes" /> that load a
///     theme or throw. Its message only names the theme, so read the reasons from the inner errors of
///     <see cref="DiagnosableException.Error" />.
/// </summary>
public sealed class DomainException : DiagnosableException {

    #region Constructors & Destructor

    internal DomainException(DomainError error) : base(error) { }

    #endregion

}
