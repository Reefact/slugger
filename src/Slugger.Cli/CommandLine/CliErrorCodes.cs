#region Usings declarations

using FirstClassErrors;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     The identifier of every way a command line can be refused, declared once so that a caller
///     branching on a situation - or a test asserting one - references a constant the compiler
///     resolves rather than a string nothing validates.
/// </summary>
internal static class CliErrorCodes {

    #region Static members

    /// <summary>See <see cref="CliErrors.Rejected" />.</summary>
    internal static readonly ErrorCode Rejected = ErrorCode.Create("CLI_REJECTED");

    /// <summary>See <see cref="CliErrors.UnknownOption" />.</summary>
    internal static readonly ErrorCode UnknownOption = ErrorCode.Create("CLI_UNKNOWN_OPTION");

    /// <summary>See <see cref="CliErrors.NotUnderstood" />.</summary>
    internal static readonly ErrorCode NotUnderstood = ErrorCode.Create("CLI_NOT_UNDERSTOOD");

    /// <summary>See <see cref="CliErrors.EmptyValue" />.</summary>
    internal static readonly ErrorCode EmptyValue = ErrorCode.Create("CLI_EMPTY_VALUE");

    /// <summary>See <see cref="CliErrors.NotAWholeNumber" />.</summary>
    internal static readonly ErrorCode NotAWholeNumber = ErrorCode.Create("CLI_NOT_A_WHOLE_NUMBER");

    /// <summary>See <see cref="CliErrors.OutOfRange" />.</summary>
    internal static readonly ErrorCode OutOfRange = ErrorCode.Create("CLI_OUT_OF_RANGE");

    /// <summary>See <see cref="CliErrors.NotOneOf" />.</summary>
    internal static readonly ErrorCode NotOneOf = ErrorCode.Create("CLI_NOT_ONE_OF");

    /// <summary>See <see cref="CliErrors.NotASingleCharacter" />.</summary>
    internal static readonly ErrorCode NotASingleCharacter = ErrorCode.Create("CLI_NOT_A_SINGLE_CHARACTER");

    /// <summary>See <see cref="CliErrors.OnlyOneCommand" />.</summary>
    internal static readonly ErrorCode OnlyOneCommand = ErrorCode.Create("CLI_ONLY_ONE_COMMAND");

    #endregion

}