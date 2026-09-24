#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a number can fail to be a percentage, which is one way: it lies outside nought to
///     a hundred.
/// </summary>
[ProvidesErrorsFor(
    "Chance",
    Description = "Reading a number as a percentage, from nought to a hundred inclusive.")]
public sealed class ChanceError : Error {

    #region Static members

    /// <summary>The number that was read.</summary>
    public static readonly ErrorContextKey<long> Read = ErrorContextKey.Create<long>("Read", "The number that was read as a percentage.");

    /// <summary>The number lies outside nought to a hundred, so it names no proportion at all.</summary>
    /// <param name="value">The number that was read.</param>
    [DocumentedBy(nameof(DescribeOutsideTheRange))]
    public static ChanceError OutsideTheRange(int value) {
        return new ChanceError(
            Codes.OutsideTheRange,
            $"A percentage runs from 0 to 100, and {value} was read.",
            "A number is not a percentage.",
            "A percentage runs from 0 to 100 inclusive.",
            context => context.Add(Read, value));
    }

    private static ErrorDocumentation DescribeOutsideTheRange() {
        return DescribeError
              .WithTitle("A number is not a percentage")
              .WithDescription(
                   "A percentage is a proportion of a hundred: nought is none of it, a hundred is all "
                 + "of it, and the figures between are the proportions. A number outside that range "
                 + "names no proportion, so there is nothing for it to mean.")
              .WithRule("A percentage runs from 0 to 100 inclusive.")
              .WithDiagnostic(
                   "A proportion was computed rather than read - a ratio left as a fraction, or a count "
                 + "scaled by the wrong factor.",
                   ErrorOrigin.Internal,
                   "Read the figure the context carries: it is the number as it arrived.")
              .AndDiagnostic(
                   "A setting written by hand carries a figure nobody bounded, such as a chance of 1000 "
                 + "meant to say \"always\".",
                   ErrorOrigin.External,
                   "A hundred is the most a percentage can say; anything above it means the same thing.")
              .WithExamples(() => OutsideTheRange(101));
    }

    #endregion

    #region Constructors & Destructor

    private ChanceError(ErrorCode                   code,
                         string                      diagnosticMessage,
                         string                      shortMessage,
                         string                      detailedMessage,
                         Action<ErrorContextBuilder> configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    #endregion

    /// <summary>Raised as this concept's own exception, so a caller can catch it by name.</summary>
    public override DiagnosableException ToException() {
        return new ChanceException(this);
    }

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="ChanceError.OutsideTheRange" />.</summary>
        public static readonly ErrorCode OutsideTheRange = ErrorCode.Create("CHANCE_OUTSIDE_RANGE");

        #endregion

    }

}
