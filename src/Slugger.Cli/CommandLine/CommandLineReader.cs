using System.Globalization;
using FirstClassErrors;
using Slugger.Application.Options;
using Slugger.Domain;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// Turns the options Spectre bound onto <see cref="SluggerSettings"/> into a
/// <see cref="CommandLineRequest"/>, or into every reason they were refused.
/// </summary>
/// <remarks>
/// It reads every option before refusing, the way a theme file is read to the end: three typos
/// in one command are three complaints in one run, not three runs (DEC0006). That is the reason
/// the settings bind to strings - Spectre converts as it binds and stops at the first failure,
/// where this converts afterwards and keeps going.
/// </remarks>
internal static class CommandLineReader
{
    /// <param name="settings">What Spectre bound from the command line.</param>
    internal static Outcome<CommandLineRequest> Read(SluggerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Reading reading = new(settings);

        return reading.Complaints.Count > 0
            ? Outcome<CommandLineRequest>.Failure(CliErrors.Rejected(reading.Complaints))
            : Outcome<CommandLineRequest>.Success(reading.ToRequest());
    }

    /// <summary>One pass over the bound options, gathering what it understood and what it did not.</summary>
    private sealed class Reading
    {
        private readonly SluggerSettings _settings;

        internal Reading(SluggerSettings settings)
        {
            _settings = settings;

            // Read before the options, because both feed one list of complaints and the caller
            // reads that list once: a command flag complaining later than it is looked at would
            // never be seen (measured - "--list-themes --init" ran the first and ignored both).
            (Command, Argument) = ReadCommand();
            Options = new SluggerOptions
            {
                Themes = Themes(settings),
                ThemeDirectory = Given("--theme-dir", settings.ThemeDirectory),
                Separator = SingleCharacter("--sep", Given("--sep", settings.Separator)),
                WordSeparator = AtMostOneCharacter("--word-sep", Given("--word-sep", settings.WordSeparator)),
                Casing = Choice<Casing>("--casing", Given("--casing", settings.Casing)),
                SegmentMode = Choice<SegmentMode>("--segment", Given("--segment", settings.SegmentMode)),
                MaxLength = Number("--max-length", Given("--max-length", settings.MaxLength), 1, int.MaxValue),
                TokenLength = Number("--token-length", Given("--token-length", settings.TokenLength), 0, int.MaxValue),
                TokenChance = Number("--token-chance", Given("--token-chance", settings.TokenChance), 0, 100),
                Count = Number("--count", Given("--count", settings.Count), 1, int.MaxValue),
                Seed = Number("--seed", Given("--seed", settings.Seed), int.MinValue, int.MaxValue),
                FoldAccents = True(settings.FoldAccents),
                Ascii = True(settings.Ascii),
                TokenHex = True(settings.TokenHex),
                TokenGlued = True(settings.TokenGlued),
                Oneshot = True(settings.Oneshot),
                Clipboard = True(settings.Clipboard),
                AllowSmallTheme = True(settings.AllowSmallTheme),
                MimicStyle = Mimic(settings),
            };
        }

        internal List<DomainError> Complaints { get; } = [];

        private SluggerOptions Options { get; }

        private CliCommand Command { get; }

        private string? Argument { get; }

        internal CommandLineRequest ToRequest() => new(Command, Options, Argument);

        /// <summary>
        /// A flag naming a command replaces generating, and two of them together is a refusal
        /// rather than a silent winner - the same reading the hand-written parser had.
        /// </summary>
        private (CliCommand Command, string? Argument) ReadCommand()
        {
            (string Flag, CliCommand Command, string? Argument)[] asked =
            [
                .. Asked("--list-themes", CliCommand.ListThemes, _settings.ListThemes, null),
                .. Asked("--init", CliCommand.SaveDefaults, _settings.SaveDefaults, null),
                .. Asked("--register", CliCommand.Register, _settings.Register is not null, _settings.Register),
                .. Asked("--unregister", CliCommand.Unregister, _settings.Unregister is not null, _settings.Unregister),
                .. Asked("--analyze", CliCommand.Analyze, _settings.Analyze is not null, _settings.Analyze),
            ];

            foreach ((string flag, _, _) in asked.Skip(1))
            {
                Complaints.Add(CliErrors.OnlyOneCommand(asked[0].Flag, flag));
            }

            return asked.Length == 0
                ? (CliCommand.Generate, null)
                : (asked[0].Command, asked[0].Argument);
        }

        private static IEnumerable<(string Flag, CliCommand Command, string? Argument)> Asked(
            string flag,
            CliCommand command,
            bool present,
            string? argument)
        {
            if (present)
            {
                yield return (flag, command, argument);
            }
        }

        /// <summary>Repeatable and comma-separated at once, both forms cumulative.</summary>
        private IReadOnlyList<string>? Themes(SluggerSettings settings) => settings.Themes is not { Length: > 0 }
            ? null
            : [.. settings.Themes
                .Select(value => Given("--theme", value))
                .OfType<string>()
                .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))];

        /// <summary>
        /// The value an option was actually given, or a complaint when it was left hanging.
        /// </summary>
        /// <remarks>
        /// An application whose only command is the default one carries a synthetic name for it,
        /// and an option left without a value swallows that name as its value: <c>slugger
        /// --theme</c> goes looking for a theme called <c>__default_command</c> (measured). The
        /// marker is read back here so the refusal says what actually went wrong. It is an
        /// internal of Spectre's, so the test that pins the message is what would catch it
        /// changing.
        /// </remarks>
        private string? Given(string flag, string? value)
        {
            const string DefaultCommandMarker = "__default_command";

            if (value != DefaultCommandMarker)
            {
                return value;
            }

            Complaints.Add(CliErrors.MissingValue(flag, "a value"));

            return null;
        }

        /// <summary>
        /// A flag that was not passed says nothing, where one that was says true. Null rather
        /// than false is what lets the saved config speak for an option this line left alone.
        /// </summary>
        private static bool? True(bool passed) => passed ? true : null;

        /// <summary>The bare flag means on; only the word "false" turns the style off.</summary>
        private MimicStyle? Mimic(SluggerSettings settings)
        {
            if (settings.MimicStyle is not { IsSet: true } flag)
            {
                return null;
            }

            if (flag.Value is null or "true")
            {
                return MimicStyle.Force;
            }

            if (flag.Value == "false")
            {
                return MimicStyle.Off;
            }

            Complaints.Add(CliErrors.NotOneOf("--mimic-style", flag.Value, ["true", "false"]));

            return null;
        }

        private char? SingleCharacter(string flag, string? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value.Length == 1)
            {
                return value[0];
            }

            Complaints.Add(CliErrors.NotASingleCharacter(flag, "a single character", value));

            return null;
        }

        /// <summary>
        /// Nothing is a value here, and the useful one: --word-sep "" glues a compound value back
        /// together, which is what the slug looked like before the words were written apart.
        /// </summary>
        private string? AtMostOneCharacter(string flag, string? value)
        {
            if (value is null || value.Length <= 1)
            {
                return value;
            }

            Complaints.Add(CliErrors.NotASingleCharacter(flag, "a single character or nothing", value));

            return null;
        }

        private int? Number(string flag, string? value, int minimum, int maximum)
        {
            if (value is null)
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

        private TChoice? Choice<TChoice>(string flag, string? value)
            where TChoice : struct, Enum
        {
            if (value is null)
            {
                return null;
            }

            if (Enum.TryParse(value, ignoreCase: true, out TChoice parsed) && Enum.IsDefined(parsed))
            {
                return parsed;
            }

            Complaints.Add(CliErrors.NotOneOf(
                flag, value, [.. Enum.GetNames<TChoice>().Select(name => name.ToLowerInvariant())]));

            return null;
        }
    }
}
