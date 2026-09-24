#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain.Validation;

/// <summary>
///     Every way a value can fail to be a term, which is one way: it spells no word at all.
///     Anything else a theme writes is reducible to words by DEC0008 and is a term of as many.
/// </summary>
[ProvidesErrorsFor(
    "Term",
    Description = "Reading a value as a term: what a theme draws, made of one or more words.")]
public static class TermErrors {

    #region Static members

    /// <summary>The value a refusal is about.</summary>
    public static readonly ErrorContextKey<string> Value = ErrorContextKey.Create<string>("Value", "The value that was read as a term.");

    /// <summary>The value spells no word, so there is no term to draw.</summary>
    /// <param name="value">What was read, as written, so that a report can point at the line.</param>
    [DocumentedBy(nameof(DescribeEmpty))]
    public static DomainError Empty(string value) {
        return DomainError.Create(
                               Codes.Empty,
                               "A term carries at least one word.",
                               context => context.Add(Value, value))
                          .WithPublicMessage("A term is missing.", "A term carries at least one word.");
    }

    private static ErrorDocumentation DescribeEmpty() {
        return DescribeError
              .WithTitle("A value spells no term")
              .WithDescription(
                   "A term is one or more words, and a word is one or more letters or digits. A value "
                 + "holding neither - empty, whitespace, or punctuation that DEC0008 reduces to nothing - "
                 + "leaves no word behind and so is no term.")
              .WithRule("A term holds at least one word.")
              .WithDiagnostic(
                   "A theme file declares an entry written entirely of characters that are not letters "
                 + "or digits, such as \"!!!\" or \"---\".",
                   ErrorOrigin.External,
                   "Read the entry as it stands in the file: normalization has emptied it, so the "
                 + "refusal names a value the file still shows in full.")
              .WithExamples(() => Empty("!!!"));
    }

    #endregion

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="TermErrors.Empty" />.</summary>
        public static readonly ErrorCode Empty = ErrorCode.Create("TERM_EMPTY");

        #endregion

    }

}
