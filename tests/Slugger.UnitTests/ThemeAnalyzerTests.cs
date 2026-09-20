using Slugger.Domain;
using Slugger.Domain.Analysis;

namespace Slugger.UnitTests;

/// <summary>
/// The analysis answers the question the validator cannot: not whether a floor is cleared, but
/// by how much, and what in the file never draws.
/// </summary>
public sealed class ThemeAnalyzerTests
{
    /// <summary>
    /// The one that justifies the whole report. A theme two words above a floor reads as fine
    /// and breaks on the next edit - and nothing said so before this.
    /// </summary>
    [Fact]
    public void Names_the_poorest_noun_and_how_far_it_is_from_the_floor()
    {
        // Setup - "moon" reaches common alone, "river" reaches one category more.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"], ["water"] = ["rushing"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", []), new Noun("river", ["water"])]);

        // Exercise
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(theme);

        // Verify
        Assert.Equal("moon", analysis.Measurements!.Adjectives.Noun);
        Assert.Equal(1, analysis.Measurements.Adjectives.Smallest);
    }

    /// <summary>
    /// The draw indexes the list while the size rule counts distinct values, so a noun written
    /// twice is drawn twice as often and the file looks right. Nothing else reports it.
    /// </summary>
    [Fact]
    public void Reports_a_noun_declared_more_than_once()
    {
        // Setup
        Theme theme = ThemeWith([new Noun("moon", []), new Noun("moon", []), new Noun("river", [])]);

        // Exercise
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(theme);

        // Verify
        Assert.Equal(["moon"], analysis.Measurements!.DuplicatedNouns);
        Assert.Equal(3, analysis.Measurements.Nouns);
        Assert.Equal(2, analysis.Measurements.DistinctNouns);
    }

    /// <summary>
    /// The mirror of the rule that refuses a noun naming a category the theme does not declare.
    /// Nothing looks the other way, so a category nobody carries is silently never drawn from.
    /// </summary>
    [Fact]
    public void Reports_a_category_no_noun_carries()
    {
        // Setup - "aside" is declared and nothing reaches it.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"], ["aside"] = ["hidden"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", [])]);

        // Exercise
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(theme);

        // Verify - "common" is never unreachable, every noun carries it.
        Assert.Equal(["aside"], analysis.Measurements!.UnreachableCategories);
    }

    [Fact]
    public void Measures_how_many_nouns_can_reach_the_rarest_and_the_commonest_adjective()
    {
        // Setup
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"], ["water"] = ["rushing"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            [new Noun("moon", []), new Noun("river", ["water"])]);

        // Exercise
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(theme);

        // Verify
        Assert.Equal(new Exposure("rushing", 1), analysis.Measurements!.LeastExposed);
        Assert.Equal(new Exposure("keen", 2), analysis.Measurements.MostExposed);
    }

    /// <summary>
    /// A theme declaring no participle is ordinary, not deficient: the floor does not apply to
    /// it, so the report has no worst case to name rather than a bad one.
    /// </summary>
    [Fact]
    public void Leaves_the_participle_floor_out_for_a_theme_that_declares_none()
    {
        // Exercise
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith([new Noun("moon", [])]));

        // Verify
        Assert.Null(analysis.Measurements!.Participles);
    }

    /// <summary>
    /// Both a value and a word may be compound, so the two multiply. An upper bound rather than
    /// a draw - these three need not meet - but a slug can never exceed it.
    /// </summary>
    [Fact]
    public void Counts_the_segments_the_longest_possible_slug_would_carry()
    {
        // Setup - two words each, so three segments become six.
        Theme theme = new(
            Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["well preserved"] },
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["catching light"] },
            [new Noun("common opal", [])]);

        // Exercise
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(theme);

        // Verify
        Assert.Equal(6, analysis.Measurements!.LongestSlugSegments);
        Assert.Equal(1, analysis.Measurements.TwoWordAdjectives);
        Assert.Equal(1, analysis.Measurements.TwoWordNouns);
    }

    /// <summary>
    /// The analysis composes the validator rather than repeating it, so a refused theme is
    /// measured and refused at once - which is the only moment the numbers are wanted.
    /// </summary>
    [Fact]
    public void Measures_a_theme_that_would_be_refused_and_carries_its_refusals()
    {
        // Setup - one noun and one adjective, far under every floor.
        ThemeAnalysis analysis = ThemeAnalyzer.Analyze(ThemeWith([new Noun("moon", [])]));

        // Verify
        Assert.NotEmpty(analysis.Refusals);
        Assert.NotNull(analysis.Measurements);
        Assert.Equal(1, analysis.Measurements.Adjectives.Smallest);
    }

    private static Theme ThemeWith(IReadOnlyList<Noun> nouns) =>
        new(Dummies.AnyThemeNameOtherThanTheBuiltInOnes(),
            new Dictionary<string, IReadOnlyList<string>> { ["common"] = ["keen"] },
            new Dictionary<string, IReadOnlyList<string>>(),
            nouns);
}
