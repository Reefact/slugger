namespace Slugger.Domain.Errors;

/// <summary>
///     An error that has its code and its diagnostic but not yet what an end user is told. It is not
///     an <see cref="Error" />, so nothing can take it for one before its public messages are given.
/// </summary>
/// <typeparam name="TError">The error it becomes.</typeparam>
internal sealed class PublicMessageStage<TError> where TError : Error {

    #region Fields

    private readonly Func<string, string?, TError> _finish;

    #endregion

    #region Constructors & Destructor

    internal PublicMessageStage(Func<string, string?, TError> finish) {
        _finish = finish;
    }

    #endregion

    /// <summary>Gives the error what an end user is told, and with it the error itself.</summary>
    /// <param name="shortMessage">A plain sentence that can be shown to anyone.</param>
    /// <param name="detailedMessage">A longer public explanation, when there is one to give.</param>
    internal TError WithPublicMessage(string shortMessage, string? detailedMessage = null) {
        return _finish(shortMessage, detailedMessage);
    }

}
