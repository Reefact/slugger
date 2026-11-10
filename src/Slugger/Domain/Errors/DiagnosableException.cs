namespace Slugger.Domain.Errors;

/// <summary>
///     An exception that raises an <see cref="Errors.Error" />, for a caller that throws rather than
///     returns. Its <see cref="Exception.Message" /> is the error's diagnostic; everything else - the
///     code, the inner errors, the facts - is on <see cref="Error" />.
/// </summary>
public abstract class DiagnosableException : Exception {

    #region Constructors & Destructor

    private protected DiagnosableException(Error error) : base(error.DiagnosticMessage) {
        Error = error;
    }

    #endregion

    /// <summary>The error raised, with every reason it carries.</summary>
    public Error Error { get; }

}
