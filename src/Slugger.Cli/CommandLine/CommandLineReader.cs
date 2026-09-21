using System.Globalization;
using FirstClassErrors;
using Slugger.Application.Options;
using Slugger.Domain;
using Spectre.Console.Cli;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// Turns the options Spectre bound onto <see cref="SluggerSettings"/> into a
/// <see cref="CommandLineRequest"/>, or into every reason they were refused.
/// </summary>
/// <remarks>
/// <para>
/// It reads every option before refusing, the way a theme file is read to the end: three typos
/// in one command are three complaints in one run, not three runs (DEC0006). That is the reason
/// the settings bind to strings - Spectre converts as it binds and stops at the first failure,
/// where this converts afterwards and keeps going.
/// </para>
/// <para>
/// An option slugger does not have is read the same way, and that is why parsing is left lenient
/// rather than made strict. Strict parsing throws on the first token it cannot place, so
/// <c>--nope --casing SHOUT</c> would report the typo and never look at the casing; lenient
/// parsing binds the rest of the line and hands the leftovers over as the remaining arguments,
/// which arrive here as complaints like any other. Both are measured.
/// </para>
/// </remarks>
internal static class CommandLineReader
{
    /// <param name="settings">What Spectre bound from the command line.</param>
    /// <param name="remaining">What it could not place, which is where an unknown option lands.</param>
    internal static Outcome<CommandLineRequest> Read(SluggerSettings settings, IRemainingArguments remaining)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(remaining);

        Reading reading = new(settings, remaining);

        return reading.Complaints.Count > 0
            ? Outcome<CommandLineRequest>.Failure(CliErrors.Rejected(reading.Complaints))
            : Outcome<CommandLineRequest>.Success(reading.ToRequest());
    }

    /// <summary>One pass over the bound options, gathering what it understood and what it did not.</summary>
    private sealed class Reading
    {
        private readonly SluggerSettings _settings;

        internal Reading(SluggerSettings settings, IRemainingArguments remaining)
        {
            _settings = settings;

            ReadLeftovers(remaining);

            // Read before the options, because both feed one list of complaints and the caller
            // reads that list once: a command flag complaining later than it is looked at would
            // never be seen (measured - "--list-themes --init" ran the first and ignored both).
            (Command, Argument) = ReadCommand();
            Options = new SluggerOptions
            {
                Themes = Themes(settings),
                ThemeDirectory = settings.ThemeDirectory,
                Separator = SingleCharacter("--sep", settings.Separator),
                WordSeparator = AtMostOneCharacter("--word-sep", settings.WordSeparator),
                Casing = Choice<Casing>("--casing", settings.Casing),
                SegmentMode = Choice<SegmentMode>("--segment", settings.SegmentMode),
                MaxLength = Number("--max-length", settings.MaxLength, 1, int.MaxValue),
                MaxSegmentWords = Number("--max-segment-words", settings.MaxSegmentWords, 1, int.MaxValue),
                TokenLength = Number("--token-length", settings.TokenLength, 0, int.MaxValue),
                TokenChance = Number("--token-chance", settings.TokenChance, 0, 100),
                Count = Number("--count", settings.Count, 1, int.MaxValue),
                Seed = Number("--seed", settings.Seed, int.MinValue, int.MaxValue),
                FoldAccents = True(settings.FoldAccents),
                Ascii = True(settings.Ascii),
                TokenHex = True(settings.TokenHex),
                TokenGlued = True(settings.TokenGlued),
                Oneshot = True(settings.Oneshot),
                Clipboard = True(settings.Clipboard),
                AllowSmallTheme = True(settings.AllowSmallTheme),
                MimicStyle = Mimic(),
            };
        }

        internal List<DomainError> Complaints { get; } = [];

        private SluggerOptions Options { get; }

        private CliCommand Command { get; }

        private string? Argument { get; }

        internal CommandLineRequest ToRequest() => new(Command, Options, Argument);

        /// <summary>
        /// Everything Spectre read and could not attach to anything: an option slugger does not
        /// have, and whatever was written after the <c>--</c> that ends the options.
        /// </summary>
        /// <remarks>
        /// A token after the <c>--</c> that looks like an option is in both collections, so the
        /// raw ones are named first and the parsed ones only where they are not already named -
        /// without which "slugger -- --nope" complains about it twice (measured). The raw form
        /// carries its value where the parsed one does not, so the two are matched on the name
        /// alone: "-- --nope=x" is one complaint, not two.
        /// </remarks>
        /// <param name="remaining">What the parser had left over.</param>
        private void ReadLeftovers(IRemainingArguments remaining)
        {
            HashSet<string> literal = [.. remaining.Raw.Select(OptionName)];
            foreach (string word in remaining.Raw)
            {
                Complaints.Add(CliErrors.NotUnderstood($"\"{word}\" is not attached to any option"));
            }

            foreach (string flag in remaining.Parsed.Select(option => option.Key).Where(flag => !literal.Contains(flag)))
            {
                Complaints.Add(CliErrors.UnknownOption(flag));
            }
        }

        /// <summary>The name part of a token, without the value Spectre would have split off.</summary>
        /// <param name="token">A token exactly as it was typed.</param>
        private static string OptionName(string token) => token.Split(['=', ':'], 2)[0];

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
                .. Asked("--theme-info", CliCommand.ThemeInfo, _settings.ThemeInfo is not null, _settings.ThemeInfo),
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
        /// <remarks>
        /// Asking for nothing is refused rather than ignored: <c>slugger --theme "$THEME"</c> with
        /// the variable unset would otherwise draw from the default theme and say nothing, which
        /// is the failure a script never notices.
        /// </remarks>
        private string[]? Themes(SluggerSettings settings)
        {
            if (settings.Themes is not { Length: > 0 })
            {
                return null;
            }

            string[] names = [.. settings.Themes
                .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))];

            if (names.Length > 0)
            {
                return names;
            }

            Complaints.Add(CliErrors.EmptyValue("--theme", "one or more theme names"));

            return null;
        }

        /// <summary>
        /// A flag that was not passed says nothing, where one that was says true. Null rather
        /// than false is what lets the saved config speak for an option this line left alone.
        /// </summary>
        private static bool? True(bool passed) => passed ? true : null;

        /// <summary>The bare flag means on; only the word "false" turns the style off.</summary>
        private MimicStyle? Mimic()
        {
            if (_settings.MimicStyle is not { IsSet: true } flag)
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

        /// <summary>
        /// One of the words the option offers, and nothing else.
        /// </summary>
        /// <remarks>
        /// Matched against the declared names rather than parsed, because Enum.TryParse also
        /// reads a number and a comma-separated list: "--casing 1" and "--casing Kebab,Snake"
        /// both quietly meant snake, where --help offers three words and no arithmetic
        /// (measured).
        /// </remarks>
        /// <typeparam name="TChoice">The set of words.</typeparam>
        /// <param name="flag">The flag it was given to.</param>
        /// <param name="value">What was typed, or null where the flag was not passed.</param>
        private TChoice? Choice<TChoice>(string flag, string? value)
            where TChoice : struct, Enum
        {
            if (value is null)
            {
                return null;
            }

            string[] names = Enum.GetNames<TChoice>();
            if (Array.Find(names, name => name.Equals(value, StringComparison.OrdinalIgnoreCase)) is { } named)
            {
                return Enum.Parse<TChoice>(named);
            }

            Complaints.Add(CliErrors.NotOneOf(flag, value, [.. Spelling.All<TChoice>()]));

            return null;
        }
    }
}
