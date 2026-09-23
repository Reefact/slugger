#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Generation;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What a theme actually draws, read back and checked against what the theme itself declares:
///     the adjective and the participle have to be words some category of that noun reaches, the
///     noun must not refuse either by name (DEC0011), and the pair must not be one the theme calls
///     impossible (DEC0017).
/// </summary>
/// <remarks>
///     <para>
///         The loading rules answer for the pools as a whole - every noun reaches enough words. They
///         say nothing about a finished slug, which is what a reader sees. This closes that gap from
///         the other end, on the text rather than on the file, and it is deliberately independent of
///         the engine: <see cref="SlugDecomposer" /> rebuilds the category algebra from the JSON
///         rather than asking the resolver, so a resolver that let a word through is caught here
///         instead of agreeing with itself.
///     </para>
///     <para>
///         A silent guard is worth nothing, so the faults are also injected on purpose below. Each
///         of the three the filter can report has a test that it is reported, and each of the two
///         ways the reading can lie to itself has a test that it does not.
///     </para>
/// </remarks>
public sealed class DrawnSlugTests {

    /// <summary>
    ///     Enough draws that a theme's rarer nouns come up, cheap enough to run on every build.
    ///     A run reads about ten thousand slugs across the repository's themes in under a second.
    /// </summary>
    private const int DrawsPerTheme = 800;

    /// <summary>Fixed, because a guard that fails on some builds and not others cannot be acted on.</summary>
    private const int Seed = 20_250_923;

    #region Static members

    /// <summary>The file name alone, so a failure names the theme rather than someone's disk.</summary>
    public static TheoryData<string> EveryRepositoryTheme() {
        TheoryData<string> themes = [];
        foreach (string path in RepositoryDirectory.ThemeFiles()) {
            themes.Add(Path.GetFileName(path));
        }

        return themes;
    }

    public static TheoryData<string> EveryEmbeddedTheme() {
        TheoryData<string> themes = [];
        foreach (string name in Themes.ListEmbedded()) {
            themes.Add(name);
        }

        return themes;
    }

    /// <summary>
    ///     Every slug this theme draws that cannot be read back into words it allows, or nothing at
    ///     all. Every reason rather than the first, so fixing a theme is one pass (DEC0006).
    /// </summary>
    /// <remarks>
    ///     The format is pinned rather than taken from the theme: a slug is checked for the words it
    ///     carries, and a theme that writes them in camel with an underscore carries the same ones.
    ///     The segment mode is not pinned, because that is what decides how many words are drawn.
    /// </remarks>
    private static List<string> CannotBeReadBack(Theme theme) {
        SlugDecomposer decomposer = new(theme, '-');
        GenerationOptions options = new() {
            Separator   = '-',
            Casing      = Casing.Kebab,
            SegmentMode = theme.Defaults.SegmentMode ?? SegmentMode.Both
        };

        List<string>        faults = [];
        DefaultRandomSource random = new(Seed);
        for (int draw = 0; draw < DrawsPerTheme; draw++) {
            string                     slug     = SlugGenerator.Generate(theme, options, random);
            IReadOnlyList<SlugReading> readings = decomposer.Readings(slug);
            if (readings.Count == 0) {
                faults.Add($"{slug} - holds no word this theme declares");

                continue;
            }

            // One valid reading is enough: a two-word adjective looks exactly like an adjective
            // followed by a participle, so only a slug no reading saves is a fault.
            string?[] reasons = [.. readings.Select(decomposer.Fault)];
            if (reasons.Any(reason => reason is null)) { continue; }

            faults.Add($"{slug} - {reasons[0]}");
        }

        return faults;
    }

    private static Theme Inline(string json) {
        Outcome<Theme> loaded = Themes.LoadFromJsonResult(json, "theme", true);

        return loaded.GetResultOrThrow();
    }

    #endregion

    [Theory]
    [MemberData(nameof(EveryRepositoryTheme))]
    public void A_repository_theme_draws_nothing_its_own_rules_refuse(string file) {
        // Setup
        Theme theme = Themes.LoadFromFile(Path.Combine(RepositoryDirectory.Themes, file));

        // Exercise
        List<string> faults = CannotBeReadBack(theme);

        // Verify
        Assert.Empty(faults);
    }

    [Theory]
    [MemberData(nameof(EveryEmbeddedTheme))]
    public void An_embedded_theme_draws_nothing_its_own_rules_refuse(string name) {
        // Setup
        Theme theme = Themes.LoadEmbedded(name);

        // Exercise
        List<string> faults = CannotBeReadBack(theme);

        // Verify
        Assert.Empty(faults);
    }

    /// <summary>
    ///     Literal on purpose: the point is a word the noun names, so an arbitrary one would say
    ///     nothing about the mechanism (DEC0011).
    /// </summary>
    [Fact]
    public void A_word_the_noun_refuses_by_name_is_reported() {
        // Setup
        Theme theme = Inline("""
                             {
                               "adjectives": { "common": ["boring", "keen"] },
                               "nouns": [{ "value": "wozniak", "except": ["boring"] }]
                             }
                             """);

        // Exercise
        SlugDecomposer decomposer = new(theme, '-');
        string?        fault      = decomposer.Fault(decomposer.Readings("boring-wozniak")[0]);

        // Verify
        Assert.Equal("\"boring\" is refused by \"wozniak\" itself", fault);
    }

    [Fact]
    public void A_word_no_category_of_the_noun_reaches_is_reported() {
        // Setup - "aquatic" is a real category of the file, and "moon" is not in it.
        Theme theme = Inline("""
                             {
                               "adjectives": { "aquatic": ["rippling"], "common": ["waning"] },
                               "nouns": [{ "value": "moon" }, { "value": "river", "categories": ["aquatic"] }]
                             }
                             """);

        // Exercise
        SlugDecomposer decomposer = new(theme, '-');
        string?        fault      = decomposer.Fault(decomposer.Readings("rippling-moon")[0]);

        // Verify
        Assert.Equal("\"rippling\" is in no category \"moon\" reaches", fault);
    }

    [Fact]
    public void A_participle_the_adjective_refuses_beside_it_is_reported() {
        // Setup
        Theme theme = Inline("""
                             {
                               "adjectives": { "common": ["frozen"] },
                               "participles": { "common": ["burning"] },
                               "incompatible": { "frozen": ["burning"] },
                               "nouns": [{ "value": "forge" }]
                             }
                             """);

        // Exercise
        SlugDecomposer decomposer = new(theme, '-');
        string?        fault      = decomposer.Fault(decomposer.Readings("frozen-burning-forge")[0]);

        // Verify
        Assert.Equal("\"frozen\" refuses \"burning\" beside it", fault);
    }

    /// <summary>
    ///     The reading's own trap, and the reason a fault is only a fault when no reading holds:
    ///     "silent running" is one adjective, and reading it as "silent" plus the participle
    ///     "running" would accuse a theme of something the reader did.
    /// </summary>
    [Fact]
    public void An_adjective_written_in_two_words_is_not_read_as_two() {
        // Setup
        Theme theme = Inline("""
                             {
                               "adjectives": { "common": ["silent running", "silent"] },
                               "participles": { "common": ["running"] },
                               "nouns": [{ "value": "whale", "except": ["silent"] }]
                             }
                             """);

        // Exercise
        SlugDecomposer             decomposer = new(theme, '-');
        IReadOnlyList<SlugReading> readings   = decomposer.Readings("silent-running-whale");

        // Verify - both readings exist, and the whole adjective is the one that holds.
        Assert.Equal(2, readings.Count);
        Assert.Contains(readings, reading => decomposer.Fault(reading) is null);
    }

    /// <summary>
    ///     The other trap: taking the longest noun and never reconsidering leaves the adjective
    ///     orphaned when a shorter noun is a suffix of a longer one.
    /// </summary>
    [Fact]
    public void The_longer_of_two_nouns_does_not_swallow_the_adjective() {
        // Setup - "soured head cheese" ends with "head cheese", and "rhubarb soured" is one word.
        Theme theme = Inline("""
                             {
                               "adjectives": { "common": ["rhubarb soured"] },
                               "nouns": [{ "value": "head cheese" }, { "value": "soured head cheese" }]
                             }
                             """);

        // Exercise
        SlugDecomposer             decomposer = new(theme, '-');
        IReadOnlyList<SlugReading> readings   = decomposer.Readings("rhubarb-soured-head-cheese");

        // Verify
        SlugReading read = Assert.Single(readings);
        Assert.Equal("rhubarb soured", read.Adjective);
        Assert.Equal("head cheese", read.Noun.Value);
    }

}