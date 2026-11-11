#region Usings declarations

using System.Globalization;

using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Errors;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     Turns the options <see cref="CommandLineParser" /> bound onto <see cref="SluggerSettings" /> into a
///     <see cref="CommandLineRequest" />, or into every reason they were refused.
/// </summary>
/// <remarks>
///     <para>
///         It reads every option before refusing, the way a theme file is read to the end: three typos
///         in one command are three complaints in one run, not three runs (DEC0006). That is the reason
///         the settings bind to strings: the parser only places text, and this converts it afterwards
///         and keeps going.
///     </para>
///     <para>
///         What the parser could not place is read the same way: an option slugger does not have, a
///         word attached to none, an option missing its value. They arrive here as leftovers, so
///         <c>--nope --casing SHOUT</c> reports the typo and the casing in one run (DEC0032).
///     </para>
/// </remarks>
internal static class CommandLineReader {

    #region Static members

    /// <param name="settings">What the parser bound from the command line.</param>
    /// <param name="remaining">What it could not place.</param>
    internal static Outcome<CommandLineRequest> Read(SluggerSettings settings, Leftovers remaining) {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(remaining);

        Reading reading = new(settings, remaining);

        return reading.Complaints.Count > 0
            ? Outcome<CommandLineRequest>.Failure(CliErrors.Rejected(reading.Complaints))
            : Outcome<CommandLineRequest>.Success(reading.ToRequest());
    }

    #endregion

    #region Nested types

    /// <summary>One pass over the bound options, gathering what it understood and what it did not.</summary>
    private sealed class Reading {

        #region Static members

        private static IEnumerable<(string Flag, CliCommand Command, string? Argument)> Asked(string     flag,
                                                                                              CliCommand command,
                                                                                              bool       present,
                                                                                              string?    argument) {
            if (present) {
                yield return (flag, command, argument);
            }
        }

        #endregion

        #region Fields

        private readonly SluggerSettings _settings;

        #endregion

        #region Constructors & Destructor

        internal Reading(SluggerSettings settings, Leftovers remaining) {
            _settings = settings;

            ReadLeftovers(remaining);

            // Read before the options, because both feed one list of complaints and the caller
            // reads that list once: a command flag complaining later than it is looked at would
            // never be seen (measured - "--list-themes --init" ran the first and ignored both).
            (Command, Argument) = ReadCommand();
            Options = new SluggerOptions {
                Themes          = Themes(settings),
                ThemeDirectory  = settings.ThemeDirectory,
                Separator       = AtMostOneCharacter("--sep", settings.Separator),
                WordSeparator   = AtMostOneCharacter("--word-sep", settings.WordSeparator),
                Casing          = Choice<Casing>("--casing", settings.Casing),
                SegmentMode     = Choice<SegmentMode>("--segment", settings.SegmentMode),
                MaxLength       = Number("--max-length", settings.MaxLength, 1, int.MaxValue),
                MaxSegmentWords = MaxSegmentWordsCap(settings.MaxSegmentWords),
                TokenLength     = Number("--token-length", settings.TokenLength, 0, int.MaxValue),
                TokenChance     = Number("--token-chance", settings.TokenChance, 0, 100),
                Count           = Number("--count", settings.Count, 1, int.MaxValue),
                Seed            = Number("--seed", settings.Seed, int.MinValue, int.MaxValue),
                FoldAccents     = Switch("--fold-accents", settings.FoldAccents),
                Ascii           = Switch("--ascii", settings.Ascii),
                TokenHex        = Switch("--token-hex", settings.TokenHex),
                TokenGlued      = Switch("--token-glued", settings.TokenGlued),
                Oneshot         = Switch("--oneshot", settings.Oneshot),
                Clipboard       = Switch("--clipboard", settings.Clipboard),
                AllowSmallTheme = Switch("--allow-small-theme", settings.AllowSmallTheme),
                MimicStyle      = Mimic()
            };
        }

        #endregion

        internal List<DomainError> Complaints { get; } = [];

        private SluggerOptions Options { get; }

        private CliCommand Command { get; }

        private string? Argument { get; }

        internal CommandLineRequest ToRequest() {
            return new CommandLineRequest(Command, Options, Argument);
        }

        /// <summary>
        ///     Everything the parser read and could not place, each a complaint of its own: a word
        ///     attached to no option - a bare one, or one written after the <c>--</c> that ends the
        ///     options -, an option slugger does not have, and an option it could not read.
        /// </summary>
        /// <param name="remaining">What the parser had left over.</param>
        private void ReadLeftovers(Leftovers remaining) {
            foreach (string word in remaining.Unattached) {
                Complaints.Add(CliErrors.NotUnderstood($"\"{word}\" is not attached to any option"));
            }

            foreach (string flag in remaining.UnknownOptions) {
                Complaints.Add(CliErrors.UnknownOption(flag));
            }

            foreach (string reason in remaining.Unreadable) {
                Complaints.Add(CliErrors.NotUnderstood(reason));
            }
        }

        /// <summary>
        ///     A flag naming a command replaces generating, and two of them together is a refusal
        ///     rather than a silent winner - the same reading the hand-written parser had.
        /// </summary>
        private (CliCommand Command, string? Argument) ReadCommand() {
            (string Flag, CliCommand Command, string? Argument)[] asked = [
                .. Asked("--list-themes", CliCommand.ListThemes, _settings.ListThemes, null),
                .. Asked("--init", CliCommand.SaveDefaults, _settings.SaveDefaults, null),
                .. Asked("--register", CliCommand.Register, _settings.Register is not null, _settings.Register),
                .. Asked("--unregister", CliCommand.Unregister, _settings.Unregister is not null, _settings.Unregister),
                .. Asked("--analyze", CliCommand.Analyze, _settings.Analyze is not null, _settings.Analyze),
                .. Asked("--theme-info", CliCommand.ThemeInfo, _settings.ThemeInfo is not null, _settings.ThemeInfo)
            ];

            foreach ((string flag, _, _) in asked.Skip(1)) {
                Complaints.Add(CliErrors.OnlyOneCommand(asked[0].Flag, flag));
            }

            return asked.Length == 0
                ? (CliCommand.Generate, null)
                : (asked[0].Command, asked[0].Argument);
        }

        /// <summary>Repeatable and comma-separated at once, both forms cumulative.</summary>
        /// <remarks>
        ///     Asking for nothing is refused rather than ignored: <c>slugger --theme "$THEME"</c> with
        ///     the variable unset would otherwise draw from the default theme and say nothing, which
        ///     is the failure a script never notices.
        /// </remarks>
        private string[]? Themes(SluggerSettings settings) {
            if (settings.Themes is not { Length: > 0 }) { return null; }

            string[] names = [
                .. settings.Themes
                           .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            ];

            if (names.Length > 0) { return names; }

            Complaints.Add(CliErrors.EmptyValue("--theme", "one or more theme names"));

            return null;
        }

        /// <summary>Read like any other switch: on forces the theme's style, off refuses it.</summary>
        private MimicStyle? Mimic() {
            bool? applies = Switch("--mimic-style", _settings.MimicStyle);
            if (applies is null) { return null; }

            return applies.Value ? MimicStyle.Force : MimicStyle.Off;
        }

        /// <summary>
        ///     A switch that was not passed says nothing; on its own or followed by "true" it says
        ///     true, and followed by "false" it says false. Null rather than false for the first is
        ///     what lets the saved config speak for an option this line left alone, and false for the
        ///     last is what lets this line turn off what the saved config turned on.
        /// </summary>
        /// <param name="flag">The flag, for a complaint about what followed it.</param>
        /// <param name="value">What the parser bound, null where the flag was not passed.</param>
        private bool? Switch(string flag, FlagValue<string>? value) {
            if (value is not { IsSet: true } passed) { return null; }
            if (passed.Value is null or "true") { return true; }
            if (passed.Value == "false") { return false; }

            Complaints.Add(CliErrors.NotOneOf(flag, passed.Value, ["true", "false"]));

            return null;
        }

        /// <summary>
        ///     Nothing is a value here, and a useful one: --word-sep "" glues a compound value back
        ///     together, and --sep "" writes the terms with nothing between them (DEC0030).
        /// </summary>
        private string? AtMostOneCharacter(string flag, string? value) {
            if (value is null || value.Length <= 1) { return value; }

            Complaints.Add(CliErrors.NotASingleCharacter(flag, "a single character or nothing", value));

            return null;
        }

        private int? Number(string flag, string? value, int minimum, int maximum) {
            if (value is null) { return null; }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) {
                Complaints.Add(CliErrors.NotAWholeNumber(flag, value));

                return null;
            }

            if (parsed < minimum || parsed > maximum) {
                Complaints.Add(CliErrors.OutOfRange(flag, parsed, minimum, maximum));

                return null;
            }

            return parsed;
        }

        /// <summary>
        ///     <c>--max-segment-words</c> reads like <see cref="Number" /> for a number, and takes
        ///     "none" besides - the one word that turns the flag itself into an answer rather than
        ///     its absence, which is what lets it override a cap the drawn theme's own
        ///     <c>defaults</c> would otherwise apply (DEC0024).
        /// </summary>
        /// <param name="value">What was typed, or null where the flag was not passed.</param>
        private SegmentWordsCap? MaxSegmentWordsCap(string? value) {
            if (value is null) { return null; }
            if (value == "none") { return SegmentWordsCap.None; }

            int? words = Number("--max-segment-words", value, 1, int.MaxValue);

            return words is { } n ? SegmentWordsCap.Of(n) : null;
        }

        /// <summary>
        ///     One of the words the option offers, and nothing else.
        /// </summary>
        /// <remarks>
        ///     Matched against the declared names rather than parsed, because Enum.TryParse also
        ///     reads a number and a comma-separated list: "--casing 1" and "--casing Kebab,Snake"
        ///     both quietly meant snake, where --help offers three words and no arithmetic
        ///     (measured).
        /// </remarks>
        /// <typeparam name="TChoice">The set of words.</typeparam>
        /// <param name="flag">The flag it was given to.</param>
        /// <param name="value">What was typed, or null where the flag was not passed.</param>
        private TChoice? Choice<TChoice>(string flag, string? value)
            where TChoice : struct, Enum {
            if (value is null) { return null; }

            string[] names = Enum.GetNames<TChoice>();
            if (Array.Find(names, name => name.Equals(value, StringComparison.OrdinalIgnoreCase)) is { } named) { return Enum.Parse<TChoice>(named); }

            Complaints.Add(CliErrors.NotOneOf(flag, value, [.. Spelling.All<TChoice>()]));

            return null;
        }

    }

    #endregion

}