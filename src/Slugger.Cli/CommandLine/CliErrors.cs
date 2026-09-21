using FirstClassErrors;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// Every way a command line can be refused, declared once.
/// </summary>
/// <remarks>
/// The command line is what FirstClassErrors calls a primary port - an incoming request - so a
/// refusal is a <see cref="PrimaryPortError"/> carrying each complaint as an inner error. Every
/// option is read before any is refused, exactly as a theme file is read to the end, so one run
/// tells the caller everything wrong with what they typed.
/// </remarks>
internal static class CliErrors
{
    /// <summary>The flag a refusal is about.</summary>
    internal static readonly ErrorContextKey<string> Flag = ErrorContextKey.Create<string>("Flag", "The flag the complaint is about.");

    /// <summary>What was given where a valid value was expected.</summary>
    internal static readonly ErrorContextKey<string> Given = ErrorContextKey.Create<string>("Given", "The value that was given.");

    /// <summary>What would have been accepted.</summary>
    internal static readonly ErrorContextKey<string> Expected = ErrorContextKey.Create<string>("Expected", "What would have been accepted.");

    /// <summary>The whole report: one error carrying every complaint about the command line.</summary>
    /// <param name="complaints">Every complaint, not just the first.</param>
    internal static PrimaryPortError Rejected(IEnumerable<DomainError> complaints)
    {
        PrimaryPortInnerErrors inner = new();
        foreach (DomainError complaint in complaints)
        {
            inner.Add(complaint);
        }

        return PrimaryPortError.Create(CliErrorCodes.Rejected, "The command line was refused.", inner)
            .WithPublicMessage("That command line cannot be run.", "See the reasons it carries.");
    }

    /// <summary>
    /// What the command line parser itself refused, before any option was read. Spectre stops on
    /// the first token it cannot place - an unknown option, a bare word - where the options it
    /// did bind are converted together afterwards (DEC0019). Wrapped here so a refusal reads the
    /// same whichever of the two stopped it.
    /// </summary>
    /// <param name="reason">What the parser said.</param>
    internal static DomainError NotUnderstood(string reason) =>
        DomainError.Create(
                CliErrorCodes.NotUnderstood,
                reason.EndsWith('.') ? reason : reason + ".",
                context => context.Add(Given, reason))
            .WithPublicMessage("The command line could not be read.");

    /// <summary>A flag that takes a value, with nothing after it.</summary>
    /// <param name="flag">The flag left hanging.</param>
    /// <param name="expected">What it wanted.</param>
    internal static DomainError MissingValue(string flag, string expected) =>
        DomainError.Create(
                CliErrorCodes.MissingValue,
                $"\"{flag}\" needs {expected}, and nothing followed it.",
                context => context.Add(Flag, flag).Add(Expected, expected))
            .WithPublicMessage("An option is missing its value.");

    /// <summary>A value that should have been a number.</summary>
    /// <param name="flag">The flag it was given to.</param>
    /// <param name="given">What was typed instead.</param>
    internal static DomainError NotAWholeNumber(string flag, string given) =>
        DomainError.Create(
                CliErrorCodes.NotAWholeNumber,
                $"\"{flag}\" needs a whole number, and \"{given}\" is not one.",
                context => context.Add(Flag, flag).Add(Given, given))
            .WithPublicMessage("An option was given something that is not a number.");

    /// <summary>A number outside what the option accepts.</summary>
    /// <param name="flag">The flag it was given to.</param>
    /// <param name="given">The number that was typed.</param>
    /// <param name="minimum">The lowest it may be.</param>
    /// <param name="maximum">The highest it may be.</param>
    internal static DomainError OutOfRange(string flag, int given, int minimum, int maximum) =>
        DomainError.Create(
                CliErrorCodes.OutOfRange,
                $"\"{flag}\" accepts {minimum} to {maximum}, and {given} is outside that.",
                context => context
                    .Add(Flag, flag)
                    .Add(Given, given.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Add(Expected, $"{minimum} to {maximum}"))
            .WithPublicMessage("An option was given a number outside its range.");

    /// <summary>A value that is not one of the choices an option offers.</summary>
    /// <param name="flag">The flag it was given to.</param>
    /// <param name="given">What was typed.</param>
    /// <param name="choices">What it accepts.</param>
    internal static DomainError NotOneOf(string flag, string given, IReadOnlyList<string> choices) =>
        DomainError.Create(
                CliErrorCodes.NotOneOf,
                $"\"{flag}\" accepts {string.Join(", ", choices)}, and \"{given}\" is none of them.",
                context => context.Add(Flag, flag).Add(Given, given).Add(Expected, string.Join(", ", choices)))
            .WithPublicMessage("An option was given a value it does not accept.");

    /// <summary>A separator longer than the flag accepts.</summary>
    /// <param name="flag">The separator flag the complaint is about.</param>
    /// <param name="expected">What that flag accepts, in the words the refusal will use.</param>
    /// <param name="given">What was typed.</param>
    internal static DomainError NotASingleCharacter(string flag, string expected, string given) =>
        DomainError.Create(
                CliErrorCodes.NotASingleCharacter,
                $"\"{flag}\" needs {expected}, and \"{given}\" is {given.Length}.",
                context => context.Add(Flag, flag).Add(Given, given).Add(Expected, expected))
            .WithPublicMessage($"The separator must be {expected}.");

    /// <summary>Two options that each run and exit, on the same line.</summary>
    /// <param name="first">The command already asked for.</param>
    /// <param name="second">The one that cannot join it.</param>
    internal static DomainError OnlyOneCommand(string first, string second) =>
        DomainError.Create(
                CliErrorCodes.OnlyOneCommand,
                $"\"{first}\" and \"{second}\" each run and exit, so only one of them can be asked for at a time.",
                context => context.Add(Flag, second).Add(Expected, first))
            .WithPublicMessage("Only one command can run at a time.");
}
