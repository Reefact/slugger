using FirstClassErrors;
using Slugger.Application.Options;
using Slugger.Cli.CommandLine;
using Slugger.Domain;

using Spectre.Console.Cli;

namespace Slugger.Cli.UnitTests;

public sealed class CommandLineReaderTests
{
    [Fact]
    public void An_empty_line_generates_with_no_opinion_about_anything()
    {
        // Exercise
        CommandLineRequest request = Parse();

        // Verify
        Assert.Equal(CliCommand.Generate, request.Command);
        Assert.Equal(SluggerOptions.Empty, request.Options);
    }

    [Fact]
    public void Reads_the_options_that_carry_a_value()
    {
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
    public void Reads_the_options_that_are_only_present_or_absent()
    {
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
    public void Gathers_themes_from_repeats_and_from_comma_lists_alike()
    {
        // Exercise
        CommandLineRequest request = Parse("--theme", "porno,animaux", "--theme", "docker");

        // Verify
        Assert.Equal(["porno", "animaux", "docker"], request.Options.Themes);
    }

    [Fact]
    public void Mimic_style_on_its_own_forces_the_themes_style()
    {
        // Exercise
        CommandLineRequest request = Parse("--mimic-style");

        // Verify
        Assert.Equal(MimicStyle.Force, request.Options.MimicStyle);
    }

    /// <summary>
    /// Three states, not a boolean: the flag is the only one whose value is optional, so the
    /// reader must only eat the token after it when it really is true or false.
    /// </summary>
    [Fact]
    public void Mimic_style_false_refuses_the_themes_style()
    {
        // Exercise
        CommandLineRequest request = Parse("--mimic-style", "false", "--count", "2");

        // Verify - and "--count" was not swallowed as its value.
        Assert.Equal(MimicStyle.Off, request.Options.MimicStyle);
        Assert.Equal(2, request.Options.Count);
    }

    [Theory]
    [InlineData("--list-themes")]
    [InlineData("--init")]
    public void Recognises_the_commands_that_need_no_argument(string flag)
    {
        // Exercise
        CommandLineRequest request = Parse(flag);

        // Verify
        Assert.NotEqual(CliCommand.Generate, request.Command);
        Assert.Null(request.Argument);
    }

    [Fact]
    public void Recognises_register_and_keeps_its_path()
    {
        // Exercise
        CommandLineRequest request = Parse("--register", "/tmp/porno.json");

        // Verify
        Assert.Equal(CliCommand.Register, request.Command);
        Assert.Equal("/tmp/porno.json", request.Argument);
    }

    [Fact]
    public void Recognises_unregister_and_keeps_its_name()
    {
        // Exercise
        CommandLineRequest request = Parse("--unregister", "porno");

        // Verify
        Assert.Equal(CliCommand.Unregister, request.Command);
        Assert.Equal("porno", request.Argument);
    }

    /// <summary>
    /// The same principle the theme loader follows: read the whole thing, then refuse with
    /// everything wrong with it. Three typos in one command are three complaints in one run.
    /// </summary>
    /// <remarks>
    /// The unknown option is in there on purpose, and it is why parsing is left lenient: strict
    /// parsing throws on it and the three values after it are never looked at, where lenient
    /// hands it over as a remaining argument and the line is read to the end (measured).
    /// </remarks>
    [Fact]
    public void Reports_every_complaint_rather_than_the_first()
    {
        // Exercise
        Outcome<CommandLineRequest> outcome = Read(
            ["--nope", "--casing", "SHOUT", "--count", "abc", "--token-chance", "500"]);

        // Verify
        Assert.Equal(4, outcome.Error!.InnerErrors.Count);
    }

    /// <summary>
    /// What is left of DEC0019's concession, pinned so that it stays that narrow: a bare word is
    /// read as the name of a command, and no command by that name ends the line there.
    /// </summary>
    [Fact]
    public void Reports_a_bare_word_alone_even_among_other_mistakes()
    {
        // Exercise
        Outcome<CommandLineRequest> outcome = Read(["docker", "--casing", "SHOUT", "--count", "abc"]);

        // Verify
        Assert.Equal(CliErrorCodes.NotUnderstood, Assert.Single(outcome.Error!.InnerErrors).Code);
    }

    [Fact]
    public void Refuses_a_value_that_is_not_one_of_the_choices()
    {
        // Verify
        Assert.Equal(CliErrorCodes.NotOneOf, OnlyComplaintOf("--casing", "SHOUT").Code);
    }

    [Fact]
    public void Refuses_a_value_that_is_not_a_number()
    {
        // Verify
        Assert.Equal(CliErrorCodes.NotAWholeNumber, OnlyComplaintOf("--count", "abc").Code);
    }

    [Fact]
    public void Refuses_a_number_outside_what_the_option_accepts()
    {
        // Verify - token chance is a percentage, so 0 to 100 and nothing else.
        Assert.Equal(CliErrorCodes.OutOfRange, OnlyComplaintOf("--token-chance", "500").Code);
    }

    [Fact]
    public void Refuses_a_separator_of_more_than_one_character()
    {
        // Verify
        Assert.Equal(CliErrorCodes.NotASingleCharacter, OnlyComplaintOf("--sep", "::").Code);
    }

    [Fact]
    public void Reads_a_word_separator_of_its_own()
    {
        // Verify
        Assert.Equal("_", Parse("--word-sep", "_").Options.WordSeparator);
    }

    /// <summary>
    /// Nothing is the value that matters here - it is how a compound value's words are glued -
    /// so an empty argument has to survive the reader rather than read as a missing value.
    /// </summary>
    [Fact]
    public void Reads_nothing_as_a_word_separator_rather_than_as_a_missing_value()
    {
        // Verify
        Assert.Equal("", Parse("--word-sep", "").Options.WordSeparator);
    }

    [Fact]
    public void Refuses_a_word_separator_of_more_than_one_character()
    {
        // Verify
        Assert.Equal(CliErrorCodes.NotASingleCharacter, OnlyComplaintOf("--word-sep", "::").Code);
    }

    /// <summary>
    /// The two separators share one complaint, so it has to name which of them was refused -
    /// "the separator must be a single character" sends the reader to the wrong flag half the time.
    /// </summary>
    [Fact]
    public void Names_which_of_the_two_separators_it_refused()
    {
        // Verify
        Assert.Contains("--sep", OnlyComplaintOf("--sep", "::").DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("--word-sep", OnlyComplaintOf("--word-sep", "::").DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// A flag is never a value: --word-sep swallowing the next option would leave that option
    /// silently unapplied, which is worse than refusing the line.
    /// </summary>
    [Fact]
    public void Refuses_a_word_separator_left_without_its_value()
    {
        // Exercise
        Error complaint = OnlyComplaintOf("--word-sep", "--count", "3");

        // Verify - the parser's own refusal, and it names the option it was about.
        Assert.Equal(CliErrorCodes.NotUnderstood, complaint.Code);
        Assert.Contains("word-sep", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// Nothing at all after an option that needs something. Spectre stops there, which is the
    /// one place a complaint still arrives on its own, so what matters is that it names the
    /// option rather than leaving the reader to guess which of twenty-four it was.
    /// </summary>
    [Fact]
    public void Refuses_a_flag_left_without_its_value()
    {
        // Exercise
        Error complaint = OnlyComplaintOf("--theme");

        // Verify
        Assert.Equal(CliErrorCodes.NotUnderstood, complaint.Code);
        Assert.Contains("theme", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuses_a_bare_word_attached_to_nothing()
    {
        // Verify - refused by the parser rather than by the reader since DEC0019, so the code is
        // the one that wraps what the parser said.
        Assert.Equal(CliErrorCodes.NotUnderstood, OnlyComplaintOf("docker").Code);
    }

    /// <summary>
    /// The one thing DEC0019 cost: the refusal names the option it did not know, and no longer
    /// guesses which one was meant - that guess was the hand-written parser's, and the answer to
    /// a typo is <c>--help</c> instead. What must not be lost is the refusal itself, and Spectre
    /// ignores an unknown option unless someone looks at what it could not place.
    /// </summary>
    [Fact]
    public void Refuses_an_option_it_does_not_know_and_names_it()
    {
        // Exercise
        Error complaint = OnlyComplaintOf("--thme");

        // Verify
        Assert.Equal(CliErrorCodes.UnknownOption, complaint.Code);
        Assert.Contains("thme", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>Each of them runs and exits, so two on one line cannot both be honoured.</summary>
    [Fact]
    public void Refuses_two_commands_on_the_same_line()
    {
        // Verify
        Assert.Equal(CliErrorCodes.OnlyOneCommand, OnlyComplaintOf("--list-themes", "--init").Code);
    }

    /// <summary>
    /// Refusing a value without naming the ones that would have worked leaves the reader to
    /// guess, and the whole point of the complaint is that they stop guessing.
    /// </summary>
    [Fact]
    public void Lists_the_values_an_option_accepts_when_it_refuses_one()
    {
        // Exercise
        Error complaint = OnlyComplaintOf("--casing", "SHOUT");

        // Verify
        Assert.Contains("kebab, snake, camel", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Names_the_separator_option_when_what_was_given_is_not_one_character()
    {
        // Exercise
        Error complaint = OnlyComplaintOf("--sep", "ab");

        // Verify - the flag, because a line may carry several and only one of them is at fault.
        Assert.Contains("--sep", complaint.DiagnosticMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// The CLI prints diagnostic messages, so nothing here shows a public one - but a consumer
    /// driving the parser from code shows exactly that. Compared against the library's sentinel
    /// rather than against emptiness: FirstClassErrors substitutes it for a missing short
    /// message, so a complaint that forgot one does not read as blank, it reads as
    /// <see cref="Error.MissingShortMessage"/> in someone else's user interface.
    /// </summary>
    [Fact]
    public void Every_complaint_carries_a_message_a_consumer_could_show()
    {
        // Setup - one of every complaint the parser can raise.
        DomainError[] complaints =
        [
            CliErrors.NotUnderstood("\"docker\" is not attached to any option"),
            CliErrors.UnknownOption("--thme"),
            CliErrors.NotAWholeNumber("--count", "many"),
            CliErrors.OutOfRange("--count", 0, 1, int.MaxValue),
            CliErrors.NotOneOf("--casing", "SHOUT", ["kebab", "snake", "camel"]),
            CliErrors.NotASingleCharacter("--sep", "a single character", "ab"),
            CliErrors.OnlyOneCommand("--init", "--list-themes"),
        ];

        // Verify
        Assert.All(complaints, complaint => Assert.NotEqual(Error.MissingShortMessage, complaint.ShortMessage));

        PrimaryPortError rejected = CliErrors.Rejected(complaints);
        Assert.NotEqual(Error.MissingShortMessage, rejected.ShortMessage);
        Assert.False(string.IsNullOrWhiteSpace(rejected.DetailedMessage));
    }

    /// <summary>
    /// Through the real application, so what is under test is the command line as a user types
    /// it - Spectre's binding included, and under the same strict parsing (DEC0019).
    /// </summary>
    private static CommandLineRequest Parse(params string[] arguments)
    {
        Outcome<CommandLineRequest> outcome = Read(arguments);
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);

        return outcome.GetResultOrThrow();
    }

    private static Error OnlyComplaintOf(params string[] arguments) =>
        Assert.Single(Read(arguments).Error!.InnerErrors);

    private static Outcome<CommandLineRequest> Read(string[] arguments)
    {
        Capture capture = new();
        try
        {
            SluggerApp.Build<Capture>(new PortRegistrar().With(capture)).Run(arguments);
        }
        catch (CommandAppException refused)
        {
            return Outcome<CommandLineRequest>.Failure(
                CliErrors.Rejected([CliErrors.NotUnderstood(refused.Message)]));
        }

        return capture.Result!;
    }

    /// <summary>Runs nothing and keeps what the line was read as.</summary>
    private sealed class Capture : Command<SluggerSettings>
    {
        internal Outcome<CommandLineRequest>? Result { get; private set; }

        protected override int Execute(CommandContext context, SluggerSettings settings, CancellationToken cancellationToken)
        {
            Result = CommandLineReader.Read(settings, context.Remaining);

            return 0;
        }
    }
}
