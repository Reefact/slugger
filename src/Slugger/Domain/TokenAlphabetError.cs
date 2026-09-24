#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a position can fail to name a character of an alphabet, which is one way: it lies
///     outside the alphabet's length.
/// </summary>
[ProvidesErrorsFor(
    "TokenAlphabet",
    Description = "Reaching a character of the alphabet a token is drawn from.")]
public sealed class TokenAlphabetError : Error {

    #region Static members

    /// <summary>The position that was asked for.</summary>
    public static readonly ErrorContextKey<long> Asked = ErrorContextKey.Create<long>("Asked", "The position the caller asked for.");

    /// <summary>An alphabet holds the positions it holds, and no draw reaches past them.</summary>
    /// <param name="index">The position that was asked for.</param>
    /// <param name="length">How many the alphabet holds.</param>
    [DocumentedBy(nameof(DescribePositionOutsideTheAlphabet))]
    public static TokenAlphabetError PositionOutsideTheAlphabet(int index, int length) {
        return new TokenAlphabetError(
            Codes.PositionOutsideTheAlphabet,
            $"Position {index} is outside an alphabet of {length}.",
            "A token digit was asked for outside its alphabet.",
            "A position runs from zero to the alphabet's length, exclusive.",
            context => context.Add(Asked, index));
    }

    private static ErrorDocumentation DescribePositionOutsideTheAlphabet() {
        return DescribeError
              .WithTitle("A token digit was asked for outside its alphabet")
              .WithDescription(
                   "An alphabet answers for the positions it holds and no others. A draw reaching past "
                 + "them asked its source for a number above the bound it gave.")
              .WithRule("A position runs from zero to the alphabet's length, exclusive.")
              .WithDiagnostic(
                   "A random source answered above the bound it was asked for, or a bound was taken from "
                 + "one alphabet and used against another.",
                   ErrorOrigin.Internal,
                   "Check that the bound handed to the source is the length of the alphabet being drawn "
                 + "from.")
              .WithExamples(() => PositionOutsideTheAlphabet(16, 10));
    }

    #endregion

    #region Constructors & Destructor

    private TokenAlphabetError(ErrorCode                   code,
                               string                      diagnosticMessage,
                               string                      shortMessage,
                               string                      detailedMessage,
                               Action<ErrorContextBuilder> configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    #endregion

    /// <summary>Raised as this concept's own exception, so a caller can catch it by name.</summary>
    public override DiagnosableException ToException() {
        return new TokenAlphabetException(this);
    }

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="TokenAlphabetError.PositionOutsideTheAlphabet" />.</summary>
        public static readonly ErrorCode PositionOutsideTheAlphabet = ErrorCode.Create("TOKEN_POSITION_OUTSIDE_ALPHABET");

        #endregion

    }

}
