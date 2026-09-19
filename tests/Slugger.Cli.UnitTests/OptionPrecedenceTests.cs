using System.Globalization;
using Slugger.Application.Abstractions;
using Slugger.Application.UseCases;
using Slugger.Infrastructure.Configuration;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger.Cli.UnitTests;

/// <summary>
/// The chain the spec states, read where a user meets it - on the terminal - rather than on the
/// resolver: an explicit argument beats what <c>--init</c> saved, which beats the drawn theme's
/// own defaults, which beat the program's default (spec, "Persistance de configuration" and
/// "Style hérité").
/// </summary>
/// <remarks>
/// Two themes of the test's own making rather than the shipped ones: the point here is the
/// chain, not docker's vocabulary, and a made-up theme can spell out which layer spoke. Their
/// words carry their family - <c>quux…</c> adjective, <c>blip…</c> participle, <c>zog…</c> noun -
/// so a slug names what it is made of.
/// </remarks>
public sealed class OptionPrecedenceTests : IDisposable
{
    /// <summary>Declares no defaults, so only --init and the command line have an opinion.</summary>
    private const string Plain = "atelier";

    /// <summary>Declares a style of its own: underscore, one word before the noun, no token.</summary>
    private const string Styled = "maison";

    private const string Adjective = "quux[a-z]{2}";
    private const string Participle = "blip[a-z]{2}";
    private const string Noun = "zog[a-z]{2}";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"slugger-precedence-{Guid.NewGuid():N}");
    private readonly FakeClipboard _clipboard = new();

    public OptionPrecedenceTests()
    {
        Directory.CreateDirectory(Themes);
        File.WriteAllText(Path.Combine(Themes, $"{Plain}.json"), Theme(defaults: null));
        File.WriteAllText(
            Path.Combine(Themes, $"{Styled}.json"),
            Theme("""{ "sep": "_", "segmentMode": "adjective", "tokenLength": 0 }"""));
    }

    private string Themes => Path.Combine(_directory, "themes");

    public void Dispose()
    {
        if (Directory.Exists(_directory)) { Directory.Delete(_directory, recursive: true); }
    }

    /// <summary>
    /// The promise of --init in one line: it persists every other option on the command line,
    /// and a later run that asks for nothing is shaped by all of them at once.
    /// </summary>
    [Fact]
    public void A_run_that_asks_for_nothing_is_shaped_by_everything_init_saved()
    {
        // Setup
        Save("--sep", "=", "--segment", "both", "--token-length", "3", "--token-hex", "--token-glued");

        // Exercise
        List<string> slugs = Generate("--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}={Participle}={Noun}[0-9a-f]{{3}}$", Assert.Single(slugs));
    }

    [Fact]
    public void An_option_on_the_command_line_overrules_the_same_option_saved()
    {
        // Setup
        Save("--sep", "=", "--segment", "both", "--token-length", "3");

        // Exercise
        List<string> slugs = Generate(
            "--sep", "#", "--segment", "adjective", "--token-length", "1", "--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}#{Noun}#[0-9]$", Assert.Single(slugs));
    }

    /// <summary>
    /// The other half of the same promise: overruling one option must not quietly drop the
    /// others, which is what makes --init worth having at all.
    /// </summary>
    [Fact]
    public void An_option_the_command_line_leaves_out_still_comes_from_what_was_saved()
    {
        // Setup - the saved line decides the token entirely; the command line only moves the separator.
        Save("--sep", "=", "--segment", "adjective", "--token-length", "2", "--token-hex", "--token-glued");

        // Exercise
        List<string> slugs = Generate("--sep", "@", "--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}@{Noun}[0-9a-f]{{2}}$", Assert.Single(slugs));
    }

    [Fact]
    public void A_saved_camel_casing_drops_the_separator_a_later_run_never_mentions()
    {
        // Setup
        Save("--casing", "camel", "--token-length", "2");

        // Exercise
        List<string> slugs = Generate("--theme", Plain, "--theme-dir", Themes);

        // Verify - camel has nowhere to put a separator, so the words carry the joins themselves.
        Assert.Matches($"^{Adjective}Blip[a-z]{{2}}Zog[a-z]{{2}}[0-9]{{2}}$", Assert.Single(slugs));
    }

    [Fact]
    public void A_casing_on_the_command_line_overrules_the_saved_one()
    {
        // Setup
        Save("--casing", "camel");

        // Exercise
        List<string> slugs = Generate("--casing", "kebab", "--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}-{Participle}-{Noun}$", Assert.Single(slugs));
    }

    [Fact]
    public void A_saved_count_decides_how_many_slugs_a_round_prints()
    {
        // Setup - the number itself carries nothing, only that the round obeys it.
        int count = Any.Int32().Between(2, 6).Generate();
        Save("--count", count.ToString(CultureInfo.InvariantCulture));

        // Exercise
        List<string> slugs = Generate("--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Equal(count, slugs.Count);
    }

    [Fact]
    public void A_count_on_the_command_line_overrules_the_saved_one()
    {
        // Setup
        Save("--count", "5");

        // Exercise
        List<string> slugs = Generate("--count", "2", "--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Equal(2, slugs.Count);
    }

    /// <summary>
    /// A saved chance of zero has to silence a saved length, or the two options cannot be
    /// configured independently - which the spec's "no option gets special treatment" requires.
    /// </summary>
    [Fact]
    public void A_saved_token_chance_of_zero_leaves_the_slug_without_a_token()
    {
        // Setup
        Save("--token-length", "3", "--token-chance", "0");

        // Exercise
        List<string> slugs = Generate("--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}-{Participle}-{Noun}$", Assert.Single(slugs));
    }

    [Fact]
    public void A_token_chance_on_the_command_line_brings_the_token_back()
    {
        // Setup
        Save("--token-length", "3", "--token-chance", "0");

        // Exercise
        List<string> slugs = Generate("--token-chance", "100", "--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}-{Participle}-{Noun}-[0-9]{{3}}$", Assert.Single(slugs));
    }

    [Fact]
    public void A_saved_seed_makes_every_later_run_draw_the_same_slugs()
    {
        // Setup
        Save("--seed", Any.Int32().Between(1, 100_000).Generate().ToString(CultureInfo.InvariantCulture), "--count", "3");

        // Exercise
        List<string> first = Generate("--theme", Plain, "--theme-dir", Themes);
        List<string> again = Generate("--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Equal(first, again);
    }

    [Fact]
    public void A_seed_on_the_command_line_draws_something_else_than_the_saved_one()
    {
        // Setup
        Save("--seed", "1", "--count", "3");

        // Exercise
        List<string> saved = Generate("--theme", Plain, "--theme-dir", Themes);
        List<string> asked = Generate("--seed", "2", "--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.NotEqual(saved, asked);
    }

    [Fact]
    public void A_saved_theme_directory_is_where_a_later_run_looks_for_a_theme()
    {
        // Setup - the command line names the theme but never says where it lives.
        Save("--theme-dir", Themes);

        // Exercise
        List<string> slugs = Generate("--theme", Plain);

        // Verify
        Assert.Matches($"^{Adjective}-{Participle}-{Noun}$", Assert.Single(slugs));
    }

    [Fact]
    public void A_saved_oneshot_keeps_a_later_run_from_waiting_for_another_round()
    {
        // Setup - input is waiting, and the saved --oneshot is what has to refuse it.
        Save("--oneshot");
        FakeConsole console = new("", "");

        // Exercise
        Run(console, "--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Single(console.Output);
    }

    [Fact]
    public void A_saved_clipboard_copies_what_a_later_run_printed()
    {
        // Setup
        Save("--clipboard");

        // Exercise
        List<string> slugs = Generate("--theme", Plain, "--theme-dir", Themes);

        // Verify
        Assert.Equal(Assert.Single(slugs), _clipboard.LastCopied);
    }

    /// <summary>
    /// The size rules are an arbitration, not a law - and the arbitration is one --init can save
    /// like any other, which is what "no option gets special treatment" means for a refusal.
    /// </summary>
    [Fact]
    public void A_saved_allow_small_theme_lets_a_later_run_load_a_theme_too_small_to_pass()
    {
        // Setup
        File.WriteAllText(
            Path.Combine(Themes, "poche.json"),
            """{ "adjectives": { "common": ["quuxaa", "quuxab"] }, "nouns": [{ "value": "zogaa" }] }""");
        FakeConsole refused = new() { IsInputRedirected = true };
        Assert.Equal(SluggerRunner.Refused, Run(refused, "--theme", "poche", "--theme-dir", Themes));

        Save("--allow-small-theme");

        // Exercise
        List<string> slugs = Generate("--theme", "poche", "--theme-dir", Themes);

        // Verify
        Assert.Matches("^quux[a-z]{2}-zogaa$", Assert.Single(slugs));
    }

    /// <summary>
    /// A preference stated once with --init outranks a theme's own taste, or --init would have
    /// no effect on formatting at all: every theme shipped here declares five format levers or
    /// more, and a single theme is in scope in every ordinary run.
    /// </summary>
    [Fact]
    public void What_init_saved_beats_the_drawn_themes_own_style()
    {
        // Setup - the saved line asks for '=' and three segments; the theme asks for '_' and two.
        Save("--sep", "=", "--segment", "both");

        // Exercise
        List<string> slugs = Generate("--theme", Styled, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}={Participle}={Noun}$", Assert.Single(slugs));
    }

    /// <summary>
    /// The other side of it, and what keeps a theme worth having: --theme maison still styles
    /// everything the config never mentions.
    /// </summary>
    [Fact]
    public void The_drawn_themes_own_style_fills_in_what_nobody_else_stated()
    {
        // Setup - the config speaks about the token alone, the theme about the separator and the segments.
        Save("--token-length", "2");

        // Exercise
        List<string> slugs = Generate("--theme", Styled, "--theme-dir", Themes);

        // Verify
        // The token joins on the theme's separator, not the program's: the theme decided that too.
        Assert.Matches($"^{Adjective}_{Noun}_[0-9]{{2}}$", Assert.Single(slugs));
    }

    [Fact]
    public void An_explicit_argument_beats_the_drawn_themes_own_style()
    {
        // Exercise
        List<string> slugs = Generate(
            "--sep", "=", "--segment", "both", "--theme", Styled, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}={Participle}={Noun}$", Assert.Single(slugs));
    }

    /// <summary>
    /// --mimic-style false takes the theme out of the chain, and what it leaves behind is the
    /// saved config - not the program's default, which is the whole difference between the two.
    /// </summary>
    [Fact]
    public void A_saved_mimic_style_of_false_hands_the_slug_back_to_the_saved_config()
    {
        // Setup
        Save("--mimic-style", "false", "--sep", "=");

        // Exercise
        List<string> slugs = Generate("--theme", Styled, "--theme-dir", Themes);

        // Verify
        Assert.Matches($"^{Adjective}={Participle}={Noun}$", Assert.Single(slugs));
    }

    private void Save(params string[] arguments) => Run(new FakeConsole(), ["--init", .. arguments]);

    private List<string> Generate(params string[] arguments)
    {
        FakeConsole console = new() { IsInputRedirected = true };

        // A refusal here is a broken fixture rather than the behaviour under test, and an empty
        // output would otherwise fail further down with nothing to explain it.
        Assert.Equal(0, Run(console, arguments));

        return console.Output;
    }

    private int Run(FakeConsole console, params string[] arguments)
    {
        IConfigStore config = new XdgConfigStore(Path.Combine(_directory, "config.json"));
        IThemeDirectory directories = new ThemeDirectory();

        SluggerRunner runner = new(
            console,
            config,
            new GenerateSlugsUseCase(directories, config, _clipboard),
            new ListThemesUseCase(directories, config),
            new RegisterThemeUseCase(directories, config),
            new UnregisterThemeUseCase(directories, config),
            new SaveDefaultsUseCase(config));

        return runner.Run(arguments);
    }

    /// <summary>
    /// A theme large enough to clear the size rules - 120 of each, every noun reaching all 120
    /// adjectives through "common" - whose words say which family they belong to.
    /// </summary>
    /// <param name="defaults">The theme's own defaults block, or null for a theme with no opinion.</param>
    private static string Theme(string? defaults)
    {
        string defaultsEntry = defaults is null ? string.Empty : $""" "defaults": {defaults},""";

        return $$"""
            {{{defaultsEntry}}
              "adjectives": { "common": [{{Words("quux")}}] },
              "participles": { "common": [{{Words("blip")}}] },
              "nouns": [{{string.Join(", ", Enumerable.Range(0, 120).Select(index => $$"""{ "value": "zog{{Suffix(index)}}" }"""))}}]
            }
            """;
    }

    private static string Words(string family) =>
        string.Join(", ", Enumerable.Range(0, 120).Select(index => $"\"{family}{Suffix(index)}\""));

    /// <summary>Letters rather than digits, so a glued token is never mistaken for part of a word.</summary>
    private static string Suffix(int index) => $"{(char)('a' + (index / 26))}{(char)('a' + (index % 26))}";
}
