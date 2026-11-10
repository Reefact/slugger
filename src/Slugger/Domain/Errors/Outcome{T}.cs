#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace Slugger.Domain.Errors;

/// <summary>
///     What an operation that can be refused gives back: its result, or the <see cref="Error" />
///     that says why there is none.
/// </summary>
/// <typeparam name="T">What the operation gives when it succeeds.</typeparam>
public sealed class Outcome<T> where T : notnull {

    #region Static members

    /// <summary>The operation succeeded and gave this.</summary>
    /// <param name="result">What it gave.</param>
    internal static Outcome<T> Success(T result) {
        ArgumentNullException.ThrowIfNull(result);

        return new Outcome<T>(result, null);
    }

    /// <summary>The operation was refused, for this reason.</summary>
    /// <param name="error">Why it was refused.</param>
    internal static Outcome<T> Failure(Error error) {
        ArgumentNullException.ThrowIfNull(error);

        return new Outcome<T>(default, error);
    }

    #endregion

    #region Fields

    private readonly T? _result;

    #endregion

    #region Constructors & Destructor

    private Outcome(T? result, Error? error) {
        _result = result;
        Error   = error;
    }

    #endregion

    /// <summary>Why the operation was refused, or <c>null</c> when it succeeded.</summary>
    public Error? Error { get; }

    /// <summary>Whether the operation was refused, in which case <see cref="Error" /> says why.</summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    /// <summary>Whether the operation succeeded, in which case <see cref="Error" /> is <c>null</c>.</summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>The result, or the error raised as an exception when there is none.</summary>
    /// <exception cref="DiagnosableException">The operation was refused; the exception holds the error.</exception>
    public T GetResultOrThrow() {
        if (Error is not null) { throw Error.ToException(); }

        return _result!;
    }

}
