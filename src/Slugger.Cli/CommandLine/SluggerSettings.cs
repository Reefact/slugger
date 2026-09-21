using System.ComponentModel;
using Spectre.Console.Cli;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// Every option slugger accepts, declared once. This is the single source of the command line:
/// Spectre binds the arguments onto it and generates <c>--help</c> from the same attributes, so
/// the help cannot describe a flag that no longer exists or miss one that does (DEC0019).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every value is a string.</b> Spectre would convert and validate each option as it binds
/// it, and stop at the first that failed; DEC0006 promises that one command line reports every
/// reason it was refused. So the binding is kept to "collect the text", and
/// <see cref="CommandLineReader"/> does the converting in one pass that accumulates.
/// </para>
/// <para>
/// The descriptions are what <c>--help</c> prints. They are the one place in the repository
/// where prose about a flag lives, which is what keeps it from drifting: there is nowhere else
/// for it to disagree with.
/// </para>
/// </remarks>
internal sealed class SluggerSettings : CommandSettings
{
    /// <summary>Repeatable and comma-separated at once, both forms cumulative.</summary>
    [CommandOption("--theme <NAME>")]
    [Description("Theme to draw from. Repeatable, and accepts a comma-separated list.")]
    public string[]? Themes { get; init; }

    [CommandOption("--theme-dir <PATH>")]
    [Description("Where registered themes live, instead of the default directory.")]
    public string? ThemeDirectory { get; init; }

    [CommandOption("--sep <CHARACTER>")]
    [Description("What joins the slug's segments. A single character.")]
    public string? Separator { get; init; }

    [CommandOption("--word-sep <CHARACTER>")]
    [Description("What joins the words of a compound value. A single character, or nothing to glue them.")]
    public string? WordSeparator { get; init; }

    [CommandOption("--casing <CASING>")]
    [Description("kebab, snake or camel.")]
    public string? Casing { get; init; }

    [CommandOption("--segment <MODE>")]
    [Description("What sits in front of the noun: adjective, participle, either, both or threeOrTwo.")]
    public string? SegmentMode { get; init; }

    [CommandOption("--max-length <CHARACTERS>")]
    [Description("The most characters a slug may carry. Narrows what the run draws from; never truncates.")]
    public string? MaxLength { get; init; }

    [CommandOption("--token-length <DIGITS>")]
    [Description("Length of the trailing token. 0 for none.")]
    public string? TokenLength { get; init; }

    [CommandOption("--token-chance <PERCENT>")]
    [Description("How often the token appears, 0 to 100.")]
    public string? TokenChance { get; init; }

    [CommandOption("--count <N>")]
    [Description("How many slugs one round generates.")]
    public string? Count { get; init; }

    [CommandOption("--seed <N>")]
    [Description("Seed for a reproducible run.")]
    public string? Seed { get; init; }

    [CommandOption("--fold-accents")]
    [Description("Drop the diacritic from a letter that carries one.")]
    public bool FoldAccents { get; init; }

    [CommandOption("--ascii")]
    [Description("Force an ASCII slug, whatever it costs the words.")]
    public bool Ascii { get; init; }

    [CommandOption("--token-hex")]
    [Description("Draw the token in hexadecimal rather than decimal.")]
    public bool TokenHex { get; init; }

    [CommandOption("--token-glued")]
    [Description("Glue the token to the previous segment, with no separator.")]
    public bool TokenGlued { get; init; }

    [CommandOption("--oneshot")]
    [Description("Generate once and quit, instead of staying in the REPL.")]
    public bool Oneshot { get; init; }

    [CommandOption("--clipboard")]
    [Description("Copy each generated slug to the clipboard.")]
    public bool Clipboard { get; init; }

    [CommandOption("--allow-small-theme")]
    [Description("Waive the minimum size rules for this run.")]
    public bool AllowSmallTheme { get; init; }

    /// <summary>
    /// Three states rather than two: absent, on, and explicitly off.
    /// </summary>
    /// <remarks>
    /// A string rather than a bool, and for the same reason every other value here is one: a
    /// <c>FlagValue&lt;bool&gt;</c> reads a bare <c>--mimic-style</c> as its default, which is
    /// false - so "on" and "explicitly off" arrive identical (measured). A string keeps them
    /// apart, the bare flag leaving the value null.
    /// </remarks>
    [CommandOption("--mimic-style [true|false]")]
    [Description("Whether the drawn theme's own defaults apply. On its own, means true.")]
    public FlagValue<string>? MimicStyle { get; init; }

    [CommandOption("--list-themes")]
    [Description("List the themes in scope and quit.")]
    public bool ListThemes { get; init; }

    [CommandOption("--init")]
    [Description("Save the options of this command line as the defaults, and quit.")]
    public bool SaveDefaults { get; init; }

    [CommandOption("--register <PATH>")]
    [Description("Validate a theme file and copy it into the theme directory.")]
    public string? Register { get; init; }

    [CommandOption("--unregister <NAME>")]
    [Description("Remove a registered theme.")]
    public string? Unregister { get; init; }

    [CommandOption("--analyze <PATH>")]
    [Description("Measure a theme file and write the report beside it.")]
    public string? Analyze { get; init; }
}
