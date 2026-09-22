#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Options;
using Slugger.Cli.CommandLine;
using Slugger.Domain;

using Spectre.Console.Cli;

#endregion

namespace Slugger.Cli.UnitTests;

public sealed class CommandLineReaderTests {

    #region Static members

    /// <summary>
    ///     Every option that can be given a value it does not accept. A line may carry several and
    ///     only one of them is at fault, so a refusal that does not say which leaves the reader to
    ///     find it.
    /// </summary>
    public static TheoryData<string, string> OptionsAndAValueTheyRefuse => new() {
        { "--theme", "" },
        { "--sep", "ab" },
        { "--word-sep", "abc" },
        { "--casing", "SHOUT" },
        { "--segment", "sideways" },
        { "--max-length", "none" },
        { "--max-segment-words", "banana" },
        { "--token-length", "none" },
        { "--token-chance", "500" },
        { "--count", "none" },
        { "--seed", "none" },
        { "--mimic-style", "maybe" }
    };

    /// <summary>
    ///     Through the real application, so what is under test is the command line as a user types
    ///     it - Spectre's binding included, and under the configuration the real one runs by, down
    ///     to how it tokenizes and what it leaves over (DEC0019).
    /// </summary>
    private static CommandLineRequest Parse(params string[] arguments) {
        Outcome<CommandLineRequest> outcome = Read(arguments);
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);

        return outcome.GetResultOrThrow();
    }

    private static Error OnlyComplaintOf(params string[] arguments) {
        return Assert.Single(Read(arguments).Error!.InnerErrors);
    }

    private static Outcome<CommandLineRequest> Read(string[] arguments) {
        Capture capture = new();
        try {
            SluggerApp.Build<Capture>(new PortRegistrar().With(capture)).Run(arguments);
        } catch (CommandAppException refused) {
            return Outcome<CommandLineRequest>.Failure(
                CliErrors.Rejected([CliErrors.NotUnderstood(refused.Message)]));
        }

        return capture.Result!;
    }

    #endregion

    [Fact]
    public void An_empty_line_generates_with_no_opinion_about_anything() {
        // Exercise
        CommandLineRequest request = Parse();

        // Verify
        Assert.Equal(CliCommand.Generate, request.Command);
        Assert.Equal(SluggerOptions.Empty, request.Options);
    }

    [Fact]
    public void Reads_the_options_that_carry_a_value() {
        // Exercise
        CommandLineRequest request = Parse(
            "--sep", "_", "--casing", "snake", "--segment", "both",
            "--token-length", "4", "--token-chance", "50", "--count", "3", "--seed", "42",
            "--max-length", "63",
            "--theme-dir", "/elsewhere");

        // Verify
        SluggerOptions options = request.Options;
        Assert.Equal('_', options.Separator);
        Assert.Equal(Casing.Snake, options.Casing);
        Assert.Equal(SegmentMode.Both, options.SegmentMode);
        Assert.Equal(4, options.TokenLength);
        Assert.Equal(63, options.MaxLength);
        Assert.Equal(50, options.TokenChance);
        Assert.Equal(3, options.Count);
        Assert.Equal(42, options.Seed);
        Assert.Equal("/elsewhere", options.ThemeDirectory);
    }

    [Fact]
    public void Reads_the_options_that_are_only_present_or_absent() {
        // Exercise
        CommandLineRequest request = Parse(
            "--token-hex", "--token-glued", "--oneshot", "--clipboard", "--allow-small-theme",
            "--fold-accents", "--ascii");

        // Verify
        SluggerOptions options = request.Options;
        Assert.True(options.FoldAccents);
        Assert.True(options.Ascii);
        Assert.True(options.TokenHex);
        Assert.True(options.TokenGlued);
        Assert.True(options.Oneshot);
        Assert.True(options.Clipboard);
        Assert.True(options.AllowSmallTheme);
    }

    /// <summary>Both forms, and cumulative.</summary>
    [Fact]
    public void Gathers_themes_from_repeats_and_from_comma_lists_alike() {
        // Exercise
        CommandLineRequest request = Parse("--theme", "porno,animaux", "--theme", "docker");

        // Verify
        Assert.Equal(["porno", "animaux", "docker"], request.Options.Themes);
    }

    [Fact]
    public void Mimic_style_on_its_own_forces_the_themes_style() {
        // Exercise
        CommandLineRequest request = Parse("--mimic-style");

        // Verify
        Assert.Equal(MimicStyle.Force, request.Options.MimicStyle);
    }

    /// <summary>
    ///     Three states, not a boolean: the flag is the only one whose value is optional, so the
    ///     reader must only eat the token after it when it really is true or false.
    /// </summary>
    [Fact]
    public void Mimic_style_false_refuses_the_themes_style() {
        // Exercise
        CommandLineRequest request = Parse("--mimic-style", "false", "--count", "2");

        // Verify - and "--count" was not swallowed as its value.
        Assert.Equal(MimicStyle.Off, request.Options.MimicStyle);
        Assert.Equal(2, request.Options.Count);
    }

    [Fact]
    public void Max_segment_words_reads_a_number_as_a_cap() {
        // Exercise
        CommandLineRequest request = Parse("--max-segment-words", "2");

        // Verify
        Assert.Equal(SegmentWordsCap.Of(2), request.Options.MaxSegmentWords);
    }

    /// <summary>
    ///     "none" is the one word this option answers besides a number - what lets an explicit
    ///     argument override a cap the drawn theme's own defaults would otherwise apply, which no
    ///     other option on the chain can say (DEC0024).
    /// </summary>
    [Fact]
    public void Max_segment_words_none_asks_for_no_cap_at_all() {
        // Exercise
        CommandLineRequest request = Parse("--max-segment-words", "none");

        // Verify
        Assert.Equal(SegmentWordsCap.None, request.Options.MaxSegmentWords);
    }

    [Theory]
    [InlineData("--list-themes")]
    [InlineData("--init")]
    public void Recognises_the_commands_that_need_no_argument(string flag) {
        // Exercise
        CommandLineRequest request = Parse(flag);

        // Verify
        Assert.NotEqual(CliCommand.Generate, request.Command);
        Assert.Null(request.Argument);
    }

    [Fact]
    public void Recognises_register_and_keeps_its_path() {
        // Exercise
        CommandLineRequest request = Parse("--register", "/tmp/porno.json");

        // Verify
        Assert.Equal(CliCommand.Register, request.Command);
        Assert.Equal("/tmp/porno.json", request.Argument);
    }

    [Fact]
    public void Recognises_unregister_and_keeps_its_name() {
        // Exercise
        CommandLineRequest request = Parse("--unregister", "porno");

        // Verify
        Assert.Equal(CliCommand.Unregister, request.Command);
        Assert.Equal("porno", request.Argument);
    }

    /// <summary>
    ///     The same principle the theme loader follows: read the whole thing, then refuse with
    ///     everything wrong with it. Three typos in one command are three complaints in one run.
    /// </summary>
    /// <remarks>
    ///     The unknown option is in there on purpose, and it is why parsing is left lenient: strict
    ///     parsing throws on it and the three values after it are never looked at, where lenient
    ///     hands it over as a remaining argument and the line is read to the end (measured).
    /// </remarks>
    [Fact]
    public void Reports_every_complaint_rather_than_the_first() {
        // Exercise
        Outcome<CommandLineRequest> outcome = Read(
            ["--nope", "--casing", "SHOUT", "--count", "abc", "--token-chance", "500"]);

        // Verify
        Assert.Equal(4, outcome.Error!.InnerErrors.Count);
    }

    /// <summary>
    ///     What is left of DEC0019's concession, pinned so that it stays that narrow: a bare word is
    ///     read as the name of a command, and no command by that name ends the line there.
    /// </summary>
    [Fact]
    public void Reports_a_bare_word_alone_even_among_other_mistakes() {
        // Exercise
        Outcome<CommandLineRequest> outcome = Read(["docker", "--casing", "SHOUT", "--count", "abc"]);

        // Verify
        Assert.Equal(CliErrorCodes.NotUnderstood, Assert.Single(outcome.Error!.InnerErrors).Code);
    }

    [Fact]
    public void Refuses_a_value_that_is_not_one_of_the_choices() {
        // Verify
        Assert.Equal(CliErrorCodes.NotOneOf, OnlyComplaintOf("--casing", "SHOUT").Code);
    }

    [Fact]
    public void Refuses_a_value_that_is_not_a_number() {
        // Verify
        Assert.Equal(CliErrorCodes.NotAWholeNumber, OnlyComplaintOf("--count", "abc").Code);
    }

    [Fact]
    public void Refuses_a_number_outside_what_the_option_accepts() {
        // Verify - token chance is a percentage, so 0 to 100 and nothing else.
        Assert.Equal(CliErrorCodes.OutOfRange, OnlyComplaintOf("--token-chance", "500").Code);
    }

    /// <summary>
    ///     A word, never a number. Enum.TryParse reads "1" as the value 1, so this used to accept
    ///     "--casing 1" and quietly mean snake, where --help offers three words and no arithmetic
    ///     (measured).
    /// </summary>
    [Fact]
    public void Refuses_the_number_behind_a_choice_rather_than_reading_it() {
        // Verify
        Assert.Equal(CliErrorCodes.NotOneOf, OnlyComplaintOf("--casing", "1").Code);
    }

    /// <summary>
    ///     The same reading, and the same silence: Enum.TryParse combines a comma-separated list
    ///     into one value, so "kebab,snake" used to mean snake.
    /// </summary>
    [Fact]
    public void Refuses_two_choices_given_at_once() {
        // Verify
        Assert.Equal(CliErrorCodes.NotOneOf, OnlyComplaintOf("--casing", "kebab,snake").Code);
    }

    /// <summary>
    ///     Asking for nothing is not asking for the default. "slugger --theme $THEME" with the
    ///     variable unset would otherwise draw from whatever was configured and say nothing, which
    ///     is the failure a script never notices.
    /// </summary>
    [Fact]
    public void Refuses_a_theme_named_by_an_empty_value() {
        // Verify
        Assert.Equal(CliErrorCodes.EmptyValue, OnlyComplaintOf("--theme", string.Empty).Code);
    }

    [Fact]
    public void Refuses_a_separator_of_more_than_one_character() {
        // Verify
        Assert.Equal(CliErrorCodes.NotASingleCharacter, OnlyComplaintOf("--sep", "::").Code);
    }

    [Fact]
    public void Reads_a_word_separator_of_its_own() {
        // Verify
        Assert.Equal("_", Parse("--word-sep", "_").Options.WordSeparator);
    }

    /// <summary>
    ///     Nothing is the value that matters here - it is how a compound value's words are glued -
    ///     so an empty argument has to survive the reader rather than read as a missing value.
    /// </summary>
    [Fact]
    public void Reads_nothing_as_a_word_separator_rather_than_as_a_missing_value() {
        // Verify
        Assert.Equal("", Parse("--word-sep", "").Options.WordSeparator);
    }

    [Fact]
    public void Refuses_a_word_separator_of_more_than_one_character() {
        // Verify
        Assert.Equal(CliErrorCodes.NotASingleCharacter, OnlyComplaintOf("--word-sep", "::").Code);
    }

    /// <summary>
    ///     The two separators share one complaint, so it has to name which of them was refused -
    ///     "the separator must be a single character" sends the reader to the wrong flag half the time.
    /// </summary>
    [Fact]
    public void Names_which_of_the_two_separators_it_refused() {
        // Verify
        Assert.Contains("--sep", OnlyComplaintOf("--sep", "::").DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("--word-sep", OnlyComplaintOf("--word-sep", "::").DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     A flag is never a value: --word-sep swallowing the next option would leave that option
    ///     silently unapplied, which is worse than refusing the line.
    /// </summary>
    [Fact]
    public void Refuses_a_word_separator_left_without_its_value() {
        // Exercise
        Error complaint = OnlyComplaintOf("--word-sep", "--count", "3");

        // Verify - the parser's own refusal, and it names the option it was about.
        Assert.Equal(CliErrorCodes.NotUnderstood, complaint.Code);
        Assert.Contains("word-sep", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Nothing at all after an option that needs something. Spectre stops there, which is the
    ///     one place a complaint still arrives on its own, so what matters is that it names the
    ///     option rather than leaving the reader to guess which of twenty-four it was.
    /// </summary>
    [Fact]
    public void Refuses_a_flag_left_without_its_value() {
        // Exercise
        Error complaint = OnlyComplaintOf("--theme");

        // Verify
        Assert.Equal(CliErrorCodes.NotUnderstood, complaint.Code);
        Assert.Contains("theme", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The "--" that ends the options. slugger takes no positional argument, so whatever follows
    ///     it is attached to nothing - and saying so is what keeps it from being read as nothing.
    /// </summary>
    [Fact]
    public void Refuses_a_word_written_after_the_end_of_the_options() {
        // Exercise
        Error complaint = OnlyComplaintOf("--", "foo");

        // Verify
        Assert.Equal(CliErrorCodes.NotUnderstood, complaint.Code);
        Assert.Contains("foo", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     One token, one complaint. A token after the "--" that looks like an option comes back
    ///     from the parser twice, once whole and once as a name without its value, and the two are
    ///     matched on the name alone - without which this says the same thing twice (measured).
    /// </summary>
    [Fact]
    public void Refuses_an_option_written_after_the_end_of_the_options_once() {
        // Verify
        Assert.Equal(CliErrorCodes.NotUnderstood, OnlyComplaintOf("--", "--nope=x").Code);
    }

    [Fact]
    public void Refuses_a_bare_word_attached_to_nothing() {
        // Verify - refused by the parser rather than by the reader since DEC0019, so the code is
        // the one that wraps what the parser said.
        Assert.Equal(CliErrorCodes.NotUnderstood, OnlyComplaintOf("docker").Code);
    }

    /// <summary>
    ///     The one thing DEC0019 cost: the refusal names the option it did not know, and no longer
    ///     guesses which one was meant - that guess was the hand-written parser's, and the answer to
    ///     a typo is <c>--help</c> instead. What must not be lost is the refusal itself, and Spectre
    ///     ignores an unknown option unless someone looks at what it could not place.
    /// </summary>
    [Fact]
    public void Refuses_an_option_it_does_not_know_and_names_it() {
        // Exercise
        Error complaint = OnlyComplaintOf("--thme");

        // Verify
        Assert.Equal(CliErrorCodes.UnknownOption, complaint.Code);
        Assert.Contains("thme", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>Each of them runs and exits, so two on one line cannot both be honoured.</summary>
    /// <remarks>
    ///     Both flags are named, for the same reason the other complaints name theirs: which two
    ///     clashed is the whole content of this refusal, and nothing asserted it until KillMutants
    ///     pointed out that all five command literals could be blanked unnoticed.
    /// </remarks>
    [Fact]
    public void Refuses_two_commands_on_the_same_line_and_names_both() {
        // Exercise
        Error complaint = OnlyComplaintOf("--list-themes", "--init");

        // Verify
        Assert.Equal(CliErrorCodes.OnlyOneCommand, complaint.Code);
        Assert.Contains("--list-themes", complaint.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("--init", complaint.DiagnosticMessage, StringComparison.Ordinal);
        Assert.True(complaint.Context.TryGet(CliErrors.Flag, out string? refused), "no flag on the complaint");
        Assert.Equal("--init", refused);
    }

    /// <summary>
    ///     The three that carry an argument, which the one above cannot reach: a command flag is
    ///     only ever compared against the first one asked for, so each needs a line of its own.
    /// </summary>
    /// <param name="second">The command that cannot join the first.</param>
    [Theory]
    [InlineData("--register")]
    [InlineData("--unregister")]
    [InlineData("--analyze")]
    public void Names_the_second_command_whatever_it_was(string second) {
        // Exercise
        Error complaint = OnlyComplaintOf("--list-themes", second, "something");

        // Verify
        Assert.Contains(second, complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Refusing a value without naming the ones that would have worked leaves the reader to
    ///     guess, and the whole point of the complaint is that they stop guessing.
    /// </summary>
    [Fact]
    public void Lists_the_values_an_option_accepts_when_it_refuses_one() {
        // Exercise
        Error complaint = OnlyComplaintOf("--casing", "SHOUT");

        // Verify
        Assert.Contains("kebab, snake, camel", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The same list, for the option whose values are no longer all one word: a mode read back
    ///     as "threeortwo" names nothing, and both parsers read case-insensitively so spelling it
    ///     properly changes what is shown and never what is accepted (DEC0020).
    /// </summary>
    [Fact]
    public void Spells_a_segment_mode_of_several_words_as_a_theme_file_writes_it() {
        // Exercise
        Error complaint = OnlyComplaintOf("--segment", "sideways");

        // Verify
        Assert.Contains(
            "adjective, participle, either, both, threeOrTwo",
            complaint.DiagnosticMessage,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Reads_the_segment_mode_that_may_leave_its_participle_out() {
        // Exercise
        CommandLineRequest request = Parse("--segment", "threeOrTwo");

        // Verify
        Assert.Equal(SegmentMode.ThreeOrTwo, request.Options.SegmentMode);
    }

    [Fact]
    public void Names_the_separator_option_when_what_was_given_is_not_one_character() {
        // Exercise
        Error complaint = OnlyComplaintOf("--sep", "ab");

        // Verify - the flag, because a line may carry several and only one of them is at fault.
        Assert.Contains("--sep", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     In the sentence and as a fact, because the two serve different readers: the sentence is
    ///     what the terminal prints, the context is what a consumer driving the parser branches on.
    /// </summary>
    /// <remarks>
    ///     Written after KillMutants found that blanking any of these flag literals killed no test:
    ///     the cases asserted which kind of complaint was raised and almost never which option it
    ///     was about, so a refusal naming the wrong flag - or none - would have passed.
    /// </remarks>
    /// <param name="flag">The option.</param>
    /// <param name="given">Something it does not accept.</param>
    [Theory]
    [MemberData(nameof(OptionsAndAValueTheyRefuse))]
    public void Names_the_option_a_refusal_is_about(string flag, string given) {
        // Exercise
        Error complaint = OnlyComplaintOf(flag, given);

        // Verify
        Assert.Contains(flag, complaint.DiagnosticMessage, StringComparison.Ordinal);
        Assert.True(complaint.Context.TryGet(CliErrors.Flag, out string? named), "no flag on the complaint");
        Assert.Equal(flag, named);
    }

    /// <summary>
    ///     The same values as the sentence lists, where something other than a terminal can read
    ///     them - a consumer showing its own message needs the choices, not the prose around them.
    /// </summary>
    [Fact]
    public void Carries_the_values_an_option_accepts_as_a_fact_of_its_own() {
        // Exercise
        Error complaint = OnlyComplaintOf("--casing", "SHOUT");

        // Verify
        Assert.True(complaint.Context.TryGet(CliErrors.Expected, out string? accepted), "no expectation on the complaint");
        Assert.Equal("kebab, snake, camel", accepted);
    }

    /// <summary>
    ///     What the parser hands over already ends in a full stop and what the reader writes does
    ///     not, so one is added where it is missing and nowhere else. Inverting that test produced
    ///     "Unknown command 'docker'.." and no case noticed (measured, KillMutants).
    /// </summary>
    [Fact]
    public void Ends_a_refusal_with_one_full_stop_whether_or_not_it_came_with_one() {
        // Verify
        Assert.Equal(
            "Unknown command 'docker'.",
            CliErrors.NotUnderstood("Unknown command 'docker'.").DiagnosticMessage);
        Assert.Equal(
            "\"foo\" is not attached to any option.",
            CliErrors.NotUnderstood("\"foo\" is not attached to any option").DiagnosticMessage);
    }

    /// <summary>
    ///     The CLI prints diagnostic messages, so nothing here shows a public one - but a consumer
    ///     driving the parser from code shows exactly that. Compared against the library's sentinel
    ///     rather than against emptiness: FirstClassErrors substitutes it for a missing short
    ///     message, so a complaint that forgot one does not read as blank, it reads as
    ///     <see cref="Error.MissingShortMessage" /> in someone else's user interface.
    /// </summary>
    [Fact]
    public void Every_complaint_carries_a_message_a_consumer_could_show() {
        // Setup - one of every complaint the parser can raise.
        DomainError[] complaints = [
            CliErrors.NotUnderstood("\"docker\" is not attached to any option"),
            CliErrors.UnknownOption("--thme"),
            CliErrors.NotAWholeNumber("--count", "many"),
            CliErrors.OutOfRange("--count", 0, 1, int.MaxValue),
            CliErrors.NotOneOf("--casing", "SHOUT", ["kebab", "snake", "camel"]),
            CliErrors.NotASingleCharacter("--sep", "a single character", "ab"),
            CliErrors.OnlyOneCommand("--init", "--list-themes"),
            CliErrors.EmptyValue("--theme", "one or more theme names")
        ];

        // Verify - blank as well as missing: the library substitutes its sentinel for a message
        // that was never given, and says nothing about one given as an empty string.
        Assert.All(complaints, complaint => Assert.NotEqual(Error.MissingShortMessage, complaint.ShortMessage));
        Assert.All(complaints, complaint => Assert.False(string.IsNullOrWhiteSpace(complaint.ShortMessage)));
        Assert.All(complaints, complaint => Assert.False(string.IsNullOrWhiteSpace(complaint.DiagnosticMessage)));

        PrimaryPortError rejected = CliErrors.Rejected(complaints);
        Assert.NotEqual(Error.MissingShortMessage, rejected.ShortMessage);
        Assert.False(string.IsNullOrWhiteSpace(rejected.DetailedMessage));
    }

    #region Nested types

    /// <summary>Runs nothing and keeps what the line was read as.</summary>
    private sealed class Capture : Command<SluggerSettings> {

        internal Outcome<CommandLineRequest>? Result { get; private set; }

        protected override int Execute(CommandContext context, SluggerSettings settings, CancellationToken cancellationToken) {
            Result = CommandLineReader.Read(settings, context.Remaining);

            return 0;
        }

    }

    #endregion

}