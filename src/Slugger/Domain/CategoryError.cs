#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a value can fail to name a category, which is one way: it names nothing.
/// </summary>
[ProvidesErrorsFor(
    "Category",
    Description = "Reading a value as a category: the label that links a noun to a group of adjectives.")]
public sealed class CategoryError : Error {

    #region Static members

    /// <summary>The value a refusal is about.</summary>
    public static readonly ErrorContextKey<string> Value = ErrorContextKey.Create<string>("Value", "The value that was read as a category.");

    /// <summary>Nothing was written where a category name was expected.</summary>
    /// <param name="value">What was read, as written, so that a report can point at the line.</param>
    [DocumentedBy(nameof(DescribeEmpty))]
    public static CategoryError Empty(string value) {
        return new CategoryError(
            Codes.Empty,
            "A category cannot be empty.",
            "A category has no name.",
            "A category carries at least one character that is not whitespace.",
            context => context.Add(Value, value));
    }

    private static ErrorDocumentation DescribeEmpty() {
        return DescribeError
              .WithTitle("A category has no name")
              .WithDescription(
                   "A category is the label a noun and a section of adjectives share. A value that "
                 + "spells nothing labels nothing, so it can link neither side to the other.")
              .WithRule("A category carries at least one character that is not whitespace.")
              .WithDiagnostic(
                   "A theme file declares an empty string in a noun's \"categories\", or an empty key in "
                 + "its \"adjectives\".",
                   ErrorOrigin.External,
                   "Read the entry as it stands in the file: a category that names nothing is usually a "
                 + "comma too many.")
              .WithExamples(() => Empty("  "));
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
