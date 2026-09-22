#region Usings declarations

using System.Globalization;

using FirstClassErrors;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     Every way a command line can be refused, declared once.
/// </summary>
/// <remarks>
///     The command line is what FirstClassErrors calls a primary port - an incoming request - so a
///     refusal is a <see cref="PrimaryPortError" /> carrying each complaint as an inner error. Every
///     option is read before any is refused, exactly as a theme file is read to the end, so one run
///     tells the caller everything wrong with what they typed.
/// </remarks>
internal static class CliErrors {

    #region Static members

    /// <summary>The flag a refusal is about.</summary>
    internal static readonly ErrorContextKey<string> Flag = ErrorContextKey.Create<string>("Flag", "The flag the complaint is about.");

    /// <summary>What was given where a valid value was expected.</summary>
    internal static readonly ErrorContextKey<string> Given = ErrorContextKey.Create<string>("Given", "The value that was given.");

    /// <summary>What would have been accepted.</summary>
    internal static readonly ErrorContextKey<string> Expected = ErrorContextKey.Create<string>("Expected", "What would have been accepted.");

    /// <summary>A refusal that is about the line rather than about one option's value.</summary>
    internal static readonly ErrorContextKey<string> Reason = ErrorContextKey.Create<string>("Reason", "Why the line could not be read.");

    /// <summary>The whole report: one error carrying every complaint about the command line.</summary>
    /// <param name="complaints">Every complaint, not just the first.</param>
    internal static PrimaryPortError Rejected(IEnumerable<DomainError> complaints) {
        PrimaryPortInnerErrors inner = new();
        foreach (DomainError complaint in complaints) {
            inner.Add(complaint);
        }

        return PrimaryPortError.Create(CliErrorCodes.Rejected, "The command line was refused.", inner)
                               .WithPublicMessage("That command line cannot be run.", "See the reasons it carries.");
    }

    /// <summary>An option that is not one of the ones declared.</summary>
    /// <param name="flag">What was typed, dashes included.</param>
    internal static DomainError UnknownOption(string flag) {
        return DomainError.Create(
                               CliErrorCodes.UnknownOption,
                               $"\"{flag}\" is not an option slugger has. \"--help\" lists the ones it does.",
                               context => context.Add(Flag, flag))
                          .WithPublicMessage("That option does not exist.");
    }

    /// <summary>
    ///     A refusal about the line itself rather than about one option's value: a word attached to
    ///     nothing, and whatever Spectre stopped on before any option was read - a bare word it takes
    ///     for a command name, or an option left without its value (DEC0019). Wrapped here so a
    ///     refusal reads the same whichever of them raised it.
    /// </summary>
    /// <param name="reason">The whole sentence, from Spectre or from the reader.</param>
    internal static DomainError NotUnderstood(string reason) {
        return DomainError.Create(
                               CliErrorCodes.NotUnderstood,
                               reason.EndsWith('.') ? reason : reason + ".",
                               context => context.Add(Reason, reason))
                          .WithPublicMessage("The command line could not be read.");
    }

    /// <summary>An option given a value that holds nothing usable.</summary>
    /// <param name="flag">The flag it was given to.</param>
    /// <param name="expected">What it wanted, in the words the refusal will use.</param>
    internal static DomainError EmptyValue(string flag, string expected) {
        return DomainError.Create(
                               CliErrorCodes.EmptyValue,
                               $"\"{flag}\" needs {expected}, and what followed it is empty.",
                               context => context.Add(Flag, flag).Add(Expected, expected))
                          .WithPublicMessage("An option was given an empty value.");
    }

    /// <summary>A value that should have been a number.</summary>
    /// <param name="flag">The flag it was given to.</param>
    /// <param name="given">What was typed instead.</param>
    internal static DomainError NotAWholeNumber(string flag, string given) {
        return DomainError.Create(
                               CliErrorCodes.NotAWholeNumber,
                               $"\"{flag}\" needs a whole number, and \"{given}\" is not one.",
                               context => context.Add(Flag, flag).Add(Given, given))
                          .WithPublicMessage("An option was given something that is not a number.");
    }

    /// <summary>A number outside what the option accepts.</summary>
    /// <param name="flag">The flag it was given to.</param>
    /// <param name="given">The number that was typed.</param>
    /// <param name="minimum">The lowest it may be.</param>
    /// <param name="maximum">The highest it may be.</param>
    internal static DomainError OutOfRange(string flag, int given, int minimum, int maximum) {
        return DomainError.Create(
                               CliErrorCodes.OutOfRange,
                               $"\"{flag}\" accepts {minimum} to {maximum}, and {given} is outside that.",
                               context => context
                                         .Add(Flag, flag)
                                         .Add(Given, given.ToString(CultureInfo.InvariantCulture))
                                         .Add(Expected, $"{minimum} to {maximum}"))
                          .WithPublicMessage("An option was given a number outside its range.");
    }

    /// <summary>A value that is not one of the choices an option offers.</summary>
    /// <param name="flag">The flag it was given to.</param>
    /// <param name="given">What was typed.</param>
    /// <param name="choices">What it accepts.</param>
    internal static DomainError NotOneOf(string flag, string given, IReadOnlyList<string> choices) {
        return DomainError.Create(
                               CliErrorCodes.NotOneOf,
                               $"\"{flag}\" accepts {string.Join(", ", choices)}, and \"{given}\" is none of them.",
                               context => context.Add(Flag, flag).Add(Given, given).Add(Expected, string.Join(", ", choices)))
                          .WithPublicMessage("An option was given a value it does not accept.");
    }

    /// <summary>A separator longer than the flag accepts.</summary>
    /// <param name="flag">The separator flag the complaint is about.</param>
    /// <param name="expected">What that flag accepts, in the words the refusal will use.</param>
    /// <param name="given">What was typed.</param>
    internal static DomainError NotASingleCharacter(string flag, string expected, string given) {
        return DomainError.Create(
                               CliErrorCodes.NotASingleCharacter,
                               $"\"{flag}\" needs {expected}, and \"{given}\" is {given.Length}.",
                               context => context.Add(Flag, flag).Add(Given, given).Add(Expected, expected))
                          .WithPublicMessage($"The separator must be {expected}.");
    }

    /// <summary>Two options that each run and exit, on the same line.</summary>
    /// <param name="first">The command already asked for.</param>
    /// <param name="second">The one that cannot join it.</param>
    internal static DomainError OnlyOneCommand(string first, string second) {
        return DomainError.Create(
                               CliErrorCodes.OnlyOneCommand,
                               $"\"{first}\" and \"{second}\" each run and exit, so only one of them can be asked for at a time.",
                               context => context.Add(Flag, second).Add(Expected, first))
                          .WithPublicMessage("Only one command can run at a time.");
    }

    #endregion

}