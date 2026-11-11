namespace Slugger.Errors;

/// <summary>
///     A rule of the engine that something broke: a theme file that does not hold together, a value
///     that is not what it claims to be, a command line that cannot be run.
/// </summary>
internal sealed class DomainError : Error {

    #region Static members

    /// <summary>Begins an error that stands on its own; its public messages finish it.</summary>
    /// <param name="code">What kind of refusal it is.</param>
    /// <param name="diagnosticMessage">What happened, for whoever fixes the cause.</param>
    /// <param name="configureContext">The facts the refusal is about, when it has any.</param>
    internal static PublicMessageStage<DomainError> Create(ErrorCode code, string diagnosticMessage, Action<ErrorContextBuilder>? configureContext = null) {
        return new PublicMessageStage<DomainError>((shortMessage, detailedMessage) => new DomainError(code, diagnosticMessage, shortMessage, detailedMessage, [], configureContext));
    }

    /// <summary>Begins an error that gathers others, such as every reason a theme was refused.</summary>
    /// <param name="code">What kind of refusal it is.</param>
    /// <param name="diagnosticMessage">What happened, for whoever fixes the cause.</param>
    /// <param name="innerErrors">The errors it gathers, in the order they were found.</param>
    /// <param name="configureContext">The facts the refusal is about, when it has any.</param>
    internal static PublicMessageStage<DomainError> Create(ErrorCode                    code,
                                                           string                       diagnosticMessage,
                                                           IEnumerable<DomainError>     innerErrors,
                                                           Action<ErrorContextBuilder>? configureContext = null) {
        return new PublicMessageStage<DomainError>((shortMessage, detailedMessage) => new DomainError(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors, configureContext));
    }

    #endregion

    #region Constructors & Destructor

    private DomainError(ErrorCode                    code,
                        string                       diagnosticMessage,
                        string                       shortMessage,
                        string?                      detailedMessage,
                        IEnumerable<DomainError>     innerErrors,
                        Action<ErrorContextBuilder>? configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors, configureContext) { }

    #endregion

    /// <summary>Raised as a <see cref="DomainException" />, which a caller catches to reach this error.</summary>
    public override DiagnosableException ToException() {
        return new DomainException(this);
    }

}
