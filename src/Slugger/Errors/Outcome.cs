#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace Slugger.Errors;

/// <summary>
///     What an operation that can be refused and gives nothing back returns: success, or the
///     <see cref="Error" /> that says why it was refused.
/// </summary>
internal sealed class Outcome {

    #region Static members

    /// <summary>The operation succeeded.</summary>
    internal static readonly Outcome Success = new(null);

    /// <summary>The operation was refused, for this reason.</summary>
    /// <param name="error">Why it was refused.</param>
    internal static Outcome Failure(Error error) {
        ArgumentNullException.ThrowIfNull(error);

        return new Outcome(error);
    }

    #endregion

    #region Constructors & Destructor

    private Outcome(Error? error) {
        Error = error;
    }

    #endregion

    /// <summary>Why the operation was refused, or <c>null</c> when it succeeded.</summary>
    internal Error? Error { get; }

    /// <summary>Whether the operation was refused, in which case <see cref="Error" /> says why.</summary>
    [MemberNotNullWhen(true, nameof(Error))]
    internal bool IsFailure => Error is not null;

    /// <summary>Whether the operation succeeded, in which case <see cref="Error" /> is <c>null</c>.</summary>
    [MemberNotNullWhen(false, nameof(Error))]
    internal bool IsSuccess => Error is null;

}
