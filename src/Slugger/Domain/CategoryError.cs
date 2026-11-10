#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using Slugger.Domain.Errors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a value can fail to name a category, which is one way: it names nothing.
/// </summary>
/// <remarks>
///     <b>Not raised by the engine yet.</b> It belongs to <see cref="Category" />, part of an ongoing
///     refactoring of the library's vocabulary, and may change or disappear before loading or generation
///     use it. Do not build on it yet.
/// </remarks>
public sealed class CategoryError : Error {

    #region Static members

    /// <summary>The value a refusal is about.</summary>
    public static readonly ErrorContextKey<string> Value = new("Value");

    /// <summary>Nothing was written where a category name was expected.</summary>
    /// <param name="value">What was read, as written, so that a report can point at the line.</param>
    /// <remarks>
    ///     <para>
    ///         A category is the label a noun and a section of adjectives share. A value that
    ///         spells nothing labels nothing, so it can link neither side to the other.
    ///     </para>
    ///     <para>
    ///         <b>The rule:</b> A category carries at least one character that is not whitespace.
    ///     </para>
    ///     <para>
    ///         <b>In what was read:</b> A theme file declares an empty string in a noun's
    ///         "categories", or an empty key in its "adjectives". Read the entry as it stands in
    ///         the file: a category that names nothing is usually a comma too many.
    ///     </para>
    /// </remarks>
    public static CategoryError Empty(string value) {
        return new CategoryError(
            Codes.Empty,
            "A category cannot be empty.",
            "A category has no name.",
            "A category carries at least one character that is not whitespace.",
            context => context.Add(Value, value));
    }

    #endregion

    #region Constructors & Destructor

    private CategoryError(ErrorCode                   code,
                          string                      diagnosticMessage,
                          string                      shortMessage,
                          string                      detailedMessage,
                          Action<ErrorContextBuilder> configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    #endregion

    /// <summary>Raised as this concept's own exception, so a caller can catch it by name.</summary>
    public override DiagnosableException ToException() {
        return new CategoryException(this);
    }

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="CategoryError.Empty" />.</summary>
        public static readonly ErrorCode Empty = ErrorCode.Create("CATEGORY_EMPTY");

        #endregion

    }

}
