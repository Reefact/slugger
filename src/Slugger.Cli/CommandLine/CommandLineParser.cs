using System.Globalization;
using FirstClassErrors;
using Slugger.Application.Options;
using Slugger.Domain;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// Turns the arguments into a <see cref="CommandLineRequest"/>, or into every reason they were
/// refused.
/// </summary>
/// <remarks>
/// It reads the whole line before refusing, the way a theme file is read to the end: three typos
/// in one command should be three complaints in one run, not three runs. Only a flag that eats
/// the token after it stops - there is nothing sensible to do with the rest of the line once the
/// reader has lost its place.
/// </remarks>
internal static class CommandLineParser
{
    /// <summary>Every option slugger accepts, listed in a refusal so the reader has somewhere to go.</summary>
    internal static IReadOnlyList<string> KnownFlags { get; } =
    [
        "--theme", "--theme-dir", "--sep", "--word-sep", "--fold-accents", "--ascii", "--casing",
        "--token-length", "--token-hex",
        "--token-chance", "--token-glued", "--segment", "--count", "--seed", "--list-themes",
        "--oneshot", "--clipboard", "--mimic-style", "--allow-small-theme", "--init",
        "--register", "--unregister",
    ];

    /// <param name="arguments">The command line as the runtime handed it over.</param>
    internal static Outcome<CommandLineRequest> Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        Reader reader = new(arguments);
        reader.ReadAll();

        return reader.Complaints.Count > 0
            ? Outcome<CommandLineRequest>.Failure(CliErrors.Rejected(reader.Complaints))
            : Outcome<CommandLineRequest>.Success(reader.ToRequest());
    }

    /// <summary>Walks the arguments once, gathering what it understood and what it did not.</summary>
    private sealed class Reader(IReadOnlyList<string> arguments)
    {
        private readonly List<string> _themes = [];
        private int _position;

        internal List<DomainError> Complaints { get; } = [];

        private CliCommand Command { get; set; } = CliCommand.Generate;

        private string? CommandFlag { get; set; }

        private string? Argument { get; set; }

        private SluggerOptions Options { get; set; } = SluggerOptions.Empty;

        internal void ReadAll()
        {
            while (_position < arguments.Count)
            {
                Read(arguments[_position++]);
            }
        }

        internal CommandLineRequest ToRequest() => new(
            Command,
            _themes.Count > 0 ? Options with { Themes = _themes } : Options,
            Argument);

        private void Read(string argument)
        {
            switch (argument)
            {
                case "--theme": AddThemes(); break;
                case "--theme-dir": Options = Options with { ThemeDirectory = Value(argument, "a path") }; break;
                case "--sep": ReadSeparator(); break;
                case "--word-sep": ReadWordSeparator(); break;
                case "--casing": Options = Options with { Casing = Choice<Casing>(argument) }; break;
                case "--segment": Options = Options with { SegmentMode = Choice<SegmentMode>(argument) }; break;
                case "--token-length": Options = Options with { TokenLength = Number(argument, 0, int.MaxValue) }; break;
                case "--token-chance": Options = Options with { TokenChance = Number(argument, 0, 100) }; break;
                case "--count": Options = Options with { Count = Number(argument, 1, int.MaxValue) }; break;
                case "--seed": Options = Options with { Seed = Number(argument, int.MinValue, int.MaxValue) }; break;
                case "--fold-accents": Options = Options with { FoldAccents = true }; break;
                case "--ascii": Options = Options with { Ascii = true }; break;
                case "--token-hex": Options = Options with { TokenHex = true }; break;
                case "--token-glued": Options = Options with { TokenGlued = true }; break;
                case "--oneshot": Options = Options with { Oneshot = true }; break;
                case "--clipboard": Options = Options with { Clipboard = true }; break;
                case "--allow-small-theme": Options = Options with { AllowSmallTheme = true }; break;
                case "--mimic-style": ReadMimicStyle(); break;
                case "--list-themes": Take(CliCommand.ListThemes, argument, needsArgument: false); break;
                case "--init": Take(CliCommand.SaveDefaults, argument, needsArgument: false); break;
                case "--register": Take(CliCommand.Register, argument, needsArgument: true); break;
                case "--unregister": Take(CliCommand.Unregister, argument, needsArgument: true); break;

                default:
                    Complaints.Add(argument.StartsWith("--", StringComparison.Ordinal)
                        ? CliErrors.UnknownFlag(argument, KnownFlags)
                        : CliErrors.UnexpectedArgument(argument));

                    break;
            }
        }

        /// <summary>Repeatable and comma-separated at once, both forms cumulative.</summary>
        private void AddThemes()
        {
            if (Value("--theme", "one or more theme names") is not { } value)
            {
                return;
            }

            _themes.AddRange(value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        private void ReadSeparator()
        {
            if (Value("--sep", "a single character") is not { } value)
            {
                return;
            }

            if (value.Length != 1)
            {
                Complaints.Add(CliErrors.NotASingleCharacter("--sep", "a single character", value));

                return;
            }

            Options = Options with { Separator = value[0] };
        }

        /// <summary>
        /// Nothing is a value here, and the useful one: --word-sep "" glues a compound value back
        /// together, which is what the slug looked like before the words were written apart.
        /// </summary>
        private void ReadWordSeparator()
        {
            if (Value("--word-sep", "a single character or nothing") is not { } value)
            {
                return;
            }

            if (value.Length > 1)
            {
                Complaints.Add(CliErrors.NotASingleCharacter("--word-sep", "a single character or nothing", value));

                return;
            }

            Options = Options with { WordSeparator = value };
        }

        /// <summary>
        /// Three states rather than a boolean: absent, on, and explicitly off. The value is
        /// optional, so the reader only eats the next token when it really is true or false.
        /// </summary>
        private void ReadMimicStyle()
        {
            if (Peek() is "true" or "false")
            {
                Options = Options with { MimicStyle = arguments[_position++] == "true" ? MimicStyle.Force : MimicStyle.Off };

                return;
            }

            Options = Options with { MimicStyle = MimicStyle.Force };
        }

        private void Take(CliCommand command, string flag, bool needsArgument)
        {
            if (CommandFlag is { } already)
            {
                Complaints.Add(CliErrors.OnlyOneCommand(already, flag));

                return;
            }

            CommandFlag = flag;
            Command = command;

            if (needsArgument)
            {
                Argument = Value(flag, command == CliCommand.Register ? "a path" : "a theme name");
            }
        }

        private int? Number(string flag, int minimum, int maximum)
        {
            if (Value(flag, "a whole number") is not { } value)
            {
                return null;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                Complaints.Add(CliErrors.NotAWholeNumber(flag, value));

                return null;
            }

            if (parsed < minimum || parsed > maximum)
            {
                Complaints.Add(CliErrors.OutOfRange(flag, parsed, minimum, maximum));

                return null;
            }

            return parsed;
        }

        private TChoice? Choice<TChoice>(string flag)
            where TChoice : struct, Enum
        {
            string[] choices = [.. Enum.GetNames<TChoice>().Select(name => name.ToLowerInvariant())];
            if (Value(flag, $"one of {string.Join(", ", choices)}") is not { } value)
            {
                return null;
            }

            if (Enum.TryParse(value, ignoreCase: true, out TChoice parsed) && Enum.IsDefined(parsed))
            {
                return parsed;
            }

            Complaints.Add(CliErrors.NotOneOf(flag, value, choices));

            return null;
        }

        /// <summary>The token after a flag, or a complaint when the flag was left hanging.</summary>
        private string? Value(string flag, string expected)
        {
            if (Peek() is not { } next || next.StartsWith("--", StringComparison.Ordinal))
            {
                Complaints.Add(CliErrors.MissingValue(flag, expected));

                return null;
            }

            _position++;

            return next;
        }

        private string? Peek() => _position < arguments.Count ? arguments[_position] : null;
    }
}
