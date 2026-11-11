#region Usings declarations

using Slugger.Domain.Analysis;

#endregion

namespace Slugger;

/// <summary>
///     What a theme measures, and nothing written about it: the numbers a report is built from. Every
///     one is a fact about the theme, not a judgement - a wide spread of exposure may be exactly what its
///     author wanted.
/// </summary>
/// <remarks>
///     The command's <c>--analyze</c> report is written from these same numbers, so a page that renders
///     them shows what the command shows (DEC0014, DEC0038).
/// </remarks>
public sealed class ThemeMeasurements {

    #region Static members

    /// <summary>The rows of the margins table, in the order the command's report lists them.</summary>
    private static IEnumerable<ThemeMargin> MarginsOf(ThemeMeasurementFacts facts) {
        yield return new ThemeMargin(ThemeRule.DistinctNouns, facts.DistinctNouns, ThemeFloors.MinimumNouns);

        if (facts.WordsBeforeTheNoun is { Floor: { } wordsFloor } words) { yield return new ThemeMargin(ThemeRule.WordsBeforeTheNoun, words.Smallest, wordsFloor, words.Noun); }
        if (facts.Adjectives is { Floor: { } adjectivesFloor } adjectives) { yield return new ThemeMargin(ThemeRule.AdjectivesPerNoun, adjectives.Smallest, adjectivesFloor, adjectives.Noun); }
        if (facts.Participles is { Floor: { } participlesFloor } participles) { yield return new ThemeMargin(ThemeRule.ParticiplesPerNoun, participles.Smallest, participlesFloor, participles.Noun); }
        if (facts.ParticiplesBesideAnAdjective is { } couple) { yield return new ThemeMargin(ThemeRule.ParticiplesBesideAnAdjective, couple.Smallest, couple.Floor, couple.Noun, couple.Adjective); }
        if (facts.CharacterCeiling is { } ceiling) { yield return new ThemeMargin(ThemeRule.Characters, facts.LongestSlug.Length, ceiling, facts.LongestSlug); }
        if (facts.Combinations is { } category) { yield return new ThemeMargin(ThemeRule.CombinationsPerCategory, category.Smallest, category.Floor, category.Category); }
    }

    #endregion

    #region Constructors & Destructor

    internal ThemeMeasurements(ThemeMeasurementFacts facts) {
        ArgumentNullException.ThrowIfNull(facts);

        Margins               = [.. MarginsOf(facts)];
        Combinations          = facts.CombinationsDrawn;
        DuplicatedNouns       = facts.DuplicatedNouns;
        UnreachableCategories = facts.UnreachableCategories;
        LeastExposed          = new WordExposure(facts.LeastExposed.Word, facts.LeastExposed.Nouns);
        MostExposed           = new WordExposure(facts.MostExposed.Word, facts.MostExposed.Nouns);
        LongestSlug           = facts.LongestSlug;
    }

    #endregion

    /// <summary>
    ///     Every rule the theme is held to, with its worst case, its limit and the margin between them -
    ///     only the rules that apply to it: a rule its segment mode does not hold it to, or a length it
    ///     does not promise, has no row.
    /// </summary>
    public IReadOnlyList<ThemeMargin> Margins { get; }

    /// <summary>
    ///     How many distinct slugs the theme draws in its own style, token aside - counted as a
    ///     <see cref="SlugGenerator" /> counts them.
    /// </summary>
    public long Combinations { get; }

    /// <summary>Nouns declared more than once, which the draw favours accordingly.</summary>
    public IReadOnlyList<string> DuplicatedNouns { get; }

    /// <summary>Declared categories no noun carries, whose words are never drawn.</summary>
    public IReadOnlyList<string> UnreachableCategories { get; }

    /// <summary>The adjective the fewest nouns can reach.</summary>
    public WordExposure LeastExposed { get; }

    /// <summary>The adjective the most nouns can reach.</summary>
    public WordExposure MostExposed { get; }

    /// <summary>The longest slug the theme can produce in its own style, written as it would be drawn.</summary>
    public string LongestSlug { get; }

}
