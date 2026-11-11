namespace Slugger.Errors;

/// <summary>
///     What a <see cref="DomainError" /> raises: the command line's own refusals, read through the inner
///     errors of <see cref="DiagnosableException.Error" />.
/// </summary>
internal sealed class DomainException : DiagnosableException {

    #region Constructors & Destructor

    internal DomainException(DomainError error) : base(error) { }

    #endregion

}
