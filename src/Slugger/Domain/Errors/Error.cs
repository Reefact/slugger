#region Usings declarations

using System.Diagnostics;

#endregion

namespace Slugger.Domain.Errors;

/// <summary>
///     Something the engine refused, described for whoever has to act on it: a stable
///     <see cref="Code" /> to branch on, a <see cref="DiagnosticMessage" /> for whoever fixes the
///     cause, and a <see cref="ShortMessage" /> to show an end user.
/// </summary>
/// <remarks>
///     <para>
///         Only the engine builds an error: the primitives and every catalogue's factories are
///         internal, so every error a caller meets - a <see cref="SluggerThemeError" />, for a
///         refused theme - describes a situation the engine knows.
///     </para>
///     <para>
///         An error can carry others as <see cref="InnerErrors" />. That is how one refusal reports
///         every reason rather than the first (DEC0006).
///     </para>
///     <para>
///         See decision record DEC0029 (in French), on why the engine carries its own error model:
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0029-modele-d-erreurs-propre-au-moteur.md
///     </para>
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public abstract class Error {

    #region Constructors & Destructor

    private protected Error(ErrorCode                    code,
                            string                       diagnosticMessage,
                            string                       shortMessage,
                            string?                      detailedMessage,
                            IEnumerable<Error>           innerErrors,
                            Action<ErrorContextBuilder>? configureContext) {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(shortMessage);
        ArgumentNullException.ThrowIfNull(innerErrors);

        Code              = code;
        DiagnosticMessage = diagnosticMessage.Trim();
        ShortMessage      = shortMessage.Trim();
        DetailedMessage   = OptionalMessage(detailedMessage);
        InnerErrors       = [.. innerErrors];
        Context           = ErrorContextBuilder.Build(configureContext);
    }

    private protected Error(ErrorCode                    code,
                            string                       diagnosticMessage,
                            string                       shortMessage,
                            string?                      detailedMessage,
                            Action<ErrorContextBuilder>? configureContext)
        : this(code, diagnosticMessage, shortMessage, detailedMessage, [], configureContext) { }

    #endregion

    /// <summary>
    ///     What kind of refusal this is, shared by every occurrence of it. Compare it with the
    ///     constants a catalogue such as <see cref="SluggerThemeError.Codes" /> declares, never
    ///     the messages: those may be reworded from one version to the next.
    /// </summary>
    public ErrorCode Code { get; }

    /// <summary>
    ///     What happened, precisely enough for whoever fixes the cause: the value that was read, the
    ///     rule it broke, what was counted. Meant for a log or a developer, and for the author of a
    ///     theme file.
    /// </summary>
    public string DiagnosticMessage { get; }

    /// <summary>A plain sentence that can be shown to an end user, such as "The theme cannot be used."</summary>
    public string ShortMessage { get; }

    /// <summary>A longer public explanation, when the error has one to give; otherwise <c>null</c>.</summary>
    public string? DetailedMessage { get; }

    /// <summary>
    ///     The errors this one gathers - every reason a theme or a command line was refused - or an
    ///     empty list when it stands on its own.
    /// </summary>
    public IReadOnlyList<Error> InnerErrors { get; }

    /// <summary>The facts the refusal is about, each read with the key that wrote it.</summary>
    public ErrorContext Context { get; }

    /// <summary>
    ///     An optional message holds something worth reading or nothing at all: a blank one is the
    ///     same as none, and a caller testing it for <c>null</c> should not have to test it for
    ///     whitespace too.
    /// </summary>
    /// <param name="message">The message as it was given.</param>
    private static string? OptionalMessage(string? message) {
        if (string.IsNullOrWhiteSpace(message)) { return null; }

        return message.Trim();
    }

    /// <summary>The exception that raises this error, for a caller that throws rather than returns.</summary>
    public abstract DiagnosableException ToException();

    /// <summary>The diagnostic and the code, for a human reading a watch window or a log line.</summary>
    public override string ToString() {
        return $"{DiagnosticMessage} ({Code})";
    }

}
