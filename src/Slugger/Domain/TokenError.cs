#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a request to draw a token can be wrong. None of them comes from a theme, so none
///     is a finding a report would carry - but each states a rule of the domain, so each travels as
///     a <see cref="TokenException" /> rather than as a bare argument exception a caller catching
///     the domain's failures would miss.
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

    /// <summary>The chance a token appears is a percentage, so it lies between zero and a hundred.</summary>
    /// <param name="chance">The chance that was asked for.</param>
    [DocumentedBy(nameof(DescribeChanceOutsideAPercentage))]
    public static TokenError ChanceOutsideAPercentage(int chance) {
        return new TokenError(
            Codes.ChanceOutsideAPercentage,
            $"A token's chance runs from 0 to 100, and {chance} was asked for.",
            "A token was asked for with a chance that is not a percentage.",
            "A token's chance runs from 0 to 100 inclusive.",
            context => context.Add(Asked, chance));
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

    private static ErrorDocumentation DescribeChanceOutsideAPercentage() {
        return DescribeError
              .WithTitle("A token's chance is not a percentage")
              .WithDescription(
                   "How often a token appears is a percentage: nought never draws one, a hundred always "
                 + "does, and the figures between decide by a roll. A number outside that range names no "
                 + "frequency at all.")
              .WithRule("A token's chance runs from 0 to 100 inclusive.")
              .WithDiagnostic(
                   "A chance was computed rather than read - a ratio left as a fraction, or a count "
                 + "scaled by the wrong factor.",
                   ErrorOrigin.Internal,
                   "Read the figure the context carries: it is the number as the draw received it.")
              .WithExamples(() => ChanceOutsideAPercentage(101));
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

        /// <summary>See <see cref="TokenError.ChanceOutsideAPercentage" />.</summary>
        public static readonly ErrorCode ChanceOutsideAPercentage = ErrorCode.Create("TOKEN_CHANCE_NOT_A_PERCENTAGE");

        #endregion

    }

}
