#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using Slugger.Domain.Errors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a value can fail to be a term, which is one way: it spells no word at all.
///     Anything else a theme writes reduces to words, and is a term of as many.
/// </summary>
/// <remarks>
///     <b>Not raised by the engine yet.</b> It belongs to <see cref="Term" />, part of an ongoing
///     refactoring of the library's vocabulary, and may change or disappear before loading or generation
///     use it. Do not build on it yet.
/// </remarks>
public sealed class TermError : Error {

    #region Static members

    /// <summary>The value a refusal is about.</summary>
    public static readonly ErrorContextKey<string> Value = new("Value");

    /// <summary>The value spells no word, so there is no term to draw.</summary>
    /// <param name="value">What was read, as written, so that a report can point at the line.</param>
    /// <remarks>
    ///     <para>
    ///         A term is one or more words, and a word is one or more letters or digits. A value
    ///         holding neither - empty, whitespace, or punctuation that DEC0008 reduces to nothing
    ///         - leaves no word behind and so is no term.
    ///     </para>
    ///     <para>
    ///         <b>The rule:</b> A term holds at least one word.
    ///     </para>
    ///     <para>
    ///         <b>In what was read:</b> A theme file declares an entry written entirely of
    ///         characters that are not letters or digits, such as "!!!" or "---". Read the entry as
    ///         it stands in the file: normalization has emptied it, so the refusal names a value
    ///         the file still shows in full.
    ///     </para>
    /// </remarks>
    public static TermError Empty(string value) {
        return new TermError(
            Codes.Empty,
            "A term carries at least one word.",
            "A term is missing.",
            "A term carries at least one word.",
            context => context.Add(Value, value));
    }

    #endregion

    #region Constructors & Destructor

    private TermError(ErrorCode                   code,
                      string                      diagnosticMessage,
                      string                      shortMessage,
                      string                      detailedMessage,
                      Action<ErrorContextBuilder> configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    #endregion

    /// <summary>Raised as this concept's own exception, so a caller can catch it by name.</summary>
    public override DiagnosableException ToException() {
        return new TermException(this);
    }

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="TermError.Empty" />.</summary>
        public static readonly ErrorCode Empty = ErrorCode.Create("TERM_EMPTY");

        #endregion

    }

}
