#region Usings declarations

using System.ComponentModel;

using Spectre.Console.Cli;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     Every option slugger accepts, declared once. This is the single source of the command line:
///     Spectre binds the arguments onto it and generates <c>--help</c> from the same attributes, so
///     the help cannot describe a flag that no longer exists or miss one that does (DEC0019).
/// </summary>
/// <remarks>
///     <para>
///         <b>Every value is a string.</b> Spectre would convert and validate each option as it binds
///         it, and stop at the first that failed; DEC0006 promises that one command line reports every
///         reason it was refused. So the binding is kept to "collect the text", and
///         <see cref="CommandLineReader" /> does the converting in one pass that accumulates.
///     </para>
///     <para>
///         The descriptions are what <c>--help</c> prints. They are the one place in the repository
///         where prose about a flag lives, which is what keeps it from drifting: there is nowhere else
///         for it to disagree with.
///     </para>
/// </remarks>
internal sealed class SluggerSettings : CommandSettings {

    /// <summary>Repeatable and comma-separated at once, both forms cumulative.</summary>
    [CommandOption("--theme <NAME>")]
    [Description("Theme to draw from (default: slugger). Repeatable or comma-separated; '*' for every theme available.")]
    public string[]? Themes { get; init; }

    [CommandOption("--theme-dir <PATH>")]
    [Description("Where registered themes live (default: ~/.slugger/themes).")]
    public string? ThemeDirectory { get; init; }

    [CommandOption("--sep <CHARACTER>")]
    [Description("What joins the terms. A single character (default: -).")]
    public string? Separator { get; init; }

    [CommandOption("--word-sep <CHARACTER>")]
    [Description("What joins the words of a multi-word term. A single character, or \"\" to glue them (default: the separator).")]
    public string? WordSeparator { get; init; }

    [CommandOption("--casing <CASING>")]
    [Description("kebab, snake or camel (default: kebab). kebab and snake differ only by --sep.")]
    public string? Casing { get; init; }

    [CommandOption("--segment <MODE>")]
    [Description("What precedes the noun: adjective, participle, either, both or threeOrTwo (default: both).")]
    public string? SegmentMode { get; init; }

    [CommandOption("--max-length <CHARACTERS>")]
    [Description("Longest slug allowed, in characters. Leaves out the words that would not fit; never truncates.")]
    public string? MaxLength { get; init; }

    [CommandOption("--max-segment-words <WORDS>")]
    [Description("Most words a term may hold, or 'none'. Leaves out longer terms; never splits one. 'none' also lifts a cap set by the theme's style.")]
    public string? MaxSegmentWords { get; init; }

    [CommandOption("--token-length <DIGITS>")]
    [Description("Length of the trailing token (default: 0, no token).")]
    public string? TokenLength { get; init; }

    [CommandOption("--token-chance <PERCENT>")]
    [Description("How many slugs out of 100 get a token (default: 100).")]
    public string? TokenChance { get; init; }

    [CommandOption("--count <N>")]
    [Description("How many slugs one round generates (default: 1).")]
    public string? Count { get; init; }

    [CommandOption("--seed <N>")]
    [Description("Seed for a reproducible run.")]
    public string? Seed { get; init; }

    [CommandOption("--fold-accents")]
    [Description("Strip the accents that can be stripped, keeping the base letter.")]
    public bool FoldAccents { get; init; }

    [CommandOption("--ascii")]
    [Description("Force an ASCII slug, even if it mangles the words.")]
    public bool Ascii { get; init; }

    [CommandOption("--token-hex")]
    [Description("Draw the token in hexadecimal rather than decimal.")]
    public bool TokenHex { get; init; }

    [CommandOption("--token-glued")]
    [Description("Glue the token to the previous segment, with no separator.")]
    public bool TokenGlued { get; init; }

    [CommandOption("--oneshot")]
    [Description("Generate once and quit. Without it, a terminal stays open: Enter draws again, Ctrl+D quits.")]
    public bool Oneshot { get; init; }

    [CommandOption("--clipboard")]
    [Description("Copy the last slug to the clipboard (needs xsel on Linux).")]
    public bool Clipboard { get; init; }

    [CommandOption("--allow-small-theme")]
    [Description("Waive the minimum size rules for this run.")]
    public bool AllowSmallTheme { get; init; }

    /// <summary>
    ///     Three states rather than two: absent, on, and explicitly off.
    /// </summary>
    /// <remarks>
    ///     A string rather than a bool, and for the same reason every other value here is one: a
    ///     <c>FlagValue&lt;bool&gt;</c> reads a bare <c>--mimic-style</c> as its default, which is
    ///     false - so "on" and "explicitly off" arrive identical (measured). A string keeps them
    ///     apart, the bare flag leaving the value null.
    /// </remarks>
    [CommandOption("--mimic-style [true|false]")]
    [Description("Whether the drawn theme's own style applies (default: yes for one theme, no for several). Without a value, means true.")]
    public FlagValue<string>? MimicStyle { get; init; }

    [CommandOption("--list-themes")]
    [Description("List the themes available and quit.")]
    public bool ListThemes { get; init; }

    [CommandOption("--init")]
    [Description("Save the other options of this command line as your defaults, and quit.")]
    public bool SaveDefaults { get; init; }

    [CommandOption("--register <PATH>")]
    [Description("Validate a theme file and copy it into the theme directory.")]
    public string? Register { get; init; }

    [CommandOption("--unregister <NAME>")]
    [Description("Remove a registered theme.")]
    public string? Unregister { get; init; }

    [CommandOption("--analyze <PATH>")]
    [Description("Measure a theme file and write the report next to it.")]
    public string? Analyze { get; init; }

    [CommandOption("--theme-info <NAME>")]
    [Description("Show a theme's own metadata - title, description, version, author, source - and quit.")]
    public string? ThemeInfo { get; init; }

}