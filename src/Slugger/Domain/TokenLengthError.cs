#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a number can fail to be a token's length, which is one way: it is below one.
/// </summary>
[ProvidesErrorsFor(
    "TokenLength",
    Description = "Reading a number as the length of a token, which is one character or more.")]
public sealed class TokenLengthError : Error {

    #region Static members

    /// <summary>The number that was read.</summary>
    public static readonly ErrorContextKey<long> Read = ErrorContextKey.Create<long>("Read", "The number that was read as a length.");

    /// <summary>A token of no characters is no token, so a length below one names nothing.</summary>
    /// <param name="value">The number that was read.</param>
    [DocumentedBy(nameof(DescribeBelowOne))]
    public static TokenLengthError BelowOne(int value) {
        return new TokenLengthError(
            Codes.BelowOne,
            $"A token carries at least one character, and a length of {value} was read.",
            "A token length carries no characters.",
            "A token's length is one character or more.",
            context => context.Add(Read, value));
    }

    private static ErrorDocumentation DescribeBelowOne() {
        return DescribeError
              .WithTitle("A token length carries no characters")
              .WithDescription(
                   "A token is the characters a slug ends with. Nought of them is not a short token, it "
                 + "is no token - which a caller says by drawing none rather than by drawing one of no "
                 + "length.")
              .WithRule("A token's length is one character or more.")
              .WithDiagnostic(
                   "A length read from a setting reached the domain without being read as one, where "
                 + "the setting's own zero means \"no token at all\".",
                   ErrorOrigin.Internal,
                   "Decide whether to draw before deciding how long: a zero is a decision, not a size.")
              .WithExamples(() => BelowOne(0));
    }

    #endregion

    #region Constructors & Destructor

    private TokenLengthError(ErrorCode                   code,
                             string                      diagnosticMessage,
                             string                      shortMessage,
                             string                      detailedMessage,
                             Action<ErrorContextBuilder> configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    #endregion

    /// <summary>Raised as this concept's own exception, so a caller can catch it by name.</summary>
    public override DiagnosableException ToException() {
        return new TokenLengthException(this);
    }

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="TokenLengthError.BelowOne" />.</summary>
        public static readonly ErrorCode BelowOne = ErrorCode.Create("TOKEN_LENGTH_BELOW_ONE");

        #endregion

    }

}
