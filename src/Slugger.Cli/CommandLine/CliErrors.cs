using FirstClassErrors;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// Every way a command line can be refused, declared once.
/// </summary>
/// <remarks>
/// The command line is what FirstClassErrors calls a primary port - an incoming request - so a
/// refusal is a <see cref="PrimaryPortError"/> carrying each complaint as an inner error. The
/// parser walks the whole line before refusing, exactly as a theme file is read to the end, so
/// one run tells the caller everything wrong with what they typed.
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

    /// <summary>A flag nobody knows.</summary>
    /// <param name="flag">What was typed.</param>
    /// <param name="known">Every flag slugger accepts, to look for a near miss among.</param>
    internal static DomainError UnknownFlag(string flag, IReadOnlyList<string> known)
    {
        // A near miss is named; a wild guess is not. Listing all nineteen options on every
        // complaint buries the other complaints, which is the opposite of reporting them together.
        string? suggestion = Nearest(flag, known);

        return DomainError.Create(
                CliErrorCodes.UnknownFlag,
                suggestion is null
                    ? $"Unknown option \"{flag}\"."
                    : $"Unknown option \"{flag}\". Did you mean \"{suggestion}\"?",
                context => context.Add(Flag, flag))
            .WithPublicMessage("That option does not exist.");
    }

    /// <summary>The closest known flag, when one is close enough to be worth naming.</summary>
    private static string? Nearest(string flag, IReadOnlyList<string> known)
    {
        const int TooFar = 3;

        string? nearest = null;
        int best = TooFar;
        foreach (string candidate in known)
        {
            int distance = Distance(flag, candidate);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    /// <summary>Levenshtein distance, on two strings short enough that the simple form is the right one.</summary>
    private static int Distance(string left, string right)
    {
        int[] previous = [.. Enumerable.Range(0, right.Length + 1)];
        int[] current = new int[right.Length + 1];

        for (int i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= right.Length; j++)
            {
                int substitution = previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

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

    /// <summary>A separator of more than one character.</summary>
    /// <param name="given">What was typed.</param>
    internal static DomainError NotASingleCharacter(string given) =>
        DomainError.Create(
                CliErrorCodes.NotASingleCharacter,
                $"\"--sep\" needs a single character, and \"{given}\" is {given.Length}.",
                context => context.Add(Flag, "--sep").Add(Given, given))
            .WithPublicMessage("The separator must be a single character.");

    /// <summary>Two options that each run and exit, on the same line.</summary>
    /// <param name="first">The command already asked for.</param>
    /// <param name="second">The one that cannot join it.</param>
    internal static DomainError OnlyOneCommand(string first, string second) =>
        DomainError.Create(
                CliErrorCodes.OnlyOneCommand,
                $"\"{first}\" and \"{second}\" each run and exit, so only one of them can be asked for at a time.",
                context => context.Add(Flag, second).Add(Expected, first))
            .WithPublicMessage("Only one command can run at a time.");

    /// <summary>A bare word where no option was expecting one.</summary>
    /// <param name="given">What was typed.</param>
    internal static DomainError UnexpectedArgument(string given) =>
        DomainError.Create(
                CliErrorCodes.UnexpectedArgument,
                $"\"{given}\" is not attached to any option. Did you mean \"--theme {given}\"?",
                context => context.Add(Given, given))
            .WithPublicMessage("An argument belongs to no option.");
}
