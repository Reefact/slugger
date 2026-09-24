#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a request to draw a token can be wrong, which is one way: its length.
/// </summary>
[ProvidesErrorsFor(
    "Token",
    Description = "Drawing a token: the characters a slug ends with, produced rather than read.")]
public sealed class TokenError : Error {

    #region Static members

    /// <summary>What was asked for.</summary>
    public static readonly ErrorContextKey<long> Asked = ErrorContextKey.Create<long>("Asked", "The figure the caller asked for.");

    /// <summary>A token of no characters is no token, so a length below one is a request for nothing.</summary>
    /// <param name="length">The length that was asked for.</param>
    [DocumentedBy(nameof(DescribeLengthBelowOne))]
    public static TokenError LengthBelowOne(int length) {
        return new TokenError(
            Codes.LengthBelowOne,
            $"A token carries at least one character, and {length} were asked for.",
            "A token was asked for with no characters.",
            "A token carries at least one character.",
            context => context.Add(Asked, length));
    }

    private static ErrorDocumentation DescribeLengthBelowOne() {
        return DescribeError
              .WithTitle("A token was asked for with no characters")
              .WithDescription(
                   "A token is the characters a slug ends with. Nought of them is not a short token, it "
                 + "is no token - which a caller says by not drawing one rather than by drawing one of "
                 + "no length.")
              .WithRule("A token carries at least one character.")
              .WithDiagnostic(
                   "A length read from a setting reached the draw without being checked, where the "
                 + "setting's own zero means \"no token at all\".",
                   ErrorOrigin.Internal,
                   "Decide whether to draw before deciding how long: a zero is a decision, not a size.")
              .WithExamples(() => LengthBelowOne(0));
    }

    #endregion

    #region Constructors & Destructor

    private TokenError(ErrorCode                   code,
                       string                      diagnosticMessage,
                       string                      shortMessage,
                       string                      detailedMessage,
                       Action<ErrorContextBuilder> configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    #endregion

    /// <summary>Raised as this concept's own exception, so a caller can catch it by name.</summary>
    public override DiagnosableException ToException() {
        return new TokenException(this);
    }

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="TokenError.LengthBelowOne" />.</summary>
        public static readonly ErrorCode LengthBelowOne = ErrorCode.Create("TOKEN_LENGTH_BELOW_ONE");

        #endregion

    }

}
