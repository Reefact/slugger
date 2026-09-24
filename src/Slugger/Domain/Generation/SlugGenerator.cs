#region Usings declarations

using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Domain.Generation;

/// <summary>
///     Pick a theme, draw a noun, resolve its pool, draw the segment word or words, format.
///     The entry point a library consumer calls directly, without ever touching the CLI.
/// </summary>
public static class SlugGenerator {

    #region Static members

    /// <summary>Generates one slug, seeding the random source from <see cref="GenerationOptions.Seed" />.</summary>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to format.</param>
    public static string Generate(Theme theme, GenerationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        return Generate(theme, options, new DefaultRandomSource(options.Seed));
    }

    /// <summary>Generates one slug from an explicit random source.</summary>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to format.</param>
    /// <param name="random">Where every draw comes from.</param>
    public static string Generate(Theme theme, GenerationOptions options, IRandomSource random) {
        ArgumentNullException.ThrowIfNull(theme);

        return Generate(ResolverFor(theme, options), options, random);
    }

    /// <summary>
    ///     Generates from several themes at once, drawing the theme with
    ///     <see cref="WeightedThemePicker" /> and the noun inside the theme it picked.
    /// </summary>
    /// <param name="themes">The themes in scope, weighted by size.</param>
    /// <param name="options">How to draw and how to format.</param>
    /// <param name="random">Where every draw comes from.</param>
    public static string Generate(WeightedThemePicker themes, GenerationOptions options, IRandomSource random) {
        ArgumentNullException.ThrowIfNull(themes);

        Theme drawn = themes.Pick(random);

        return Generate(ResolverFor(drawn, options), options, random);
    }

    /// <summary>
    ///     The surface a run draws from, narrowed to what its budget leaves room for (DEC0018), or
    ///     the whole theme when it declares none.
    /// </summary>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to format, which is what decides the budget.</param>
    internal static ThemeResolver ResolverFor(Theme theme, GenerationOptions options) {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(options);

        return new ThemeResolver(
            theme,
            options.SegmentMode,
            options.MaxLength is { } ceiling ? new SlugBudget(ceiling, options) : null,
            options.MaxSegmentWords);
    }

    /// <summary>
    ///     Generates from a resolver already built, which is what keeps a batch from reducing the
    ///     same surface once per slug.
    /// </summary>
    /// <param name="resolver">The surface to draw from.</param>
    /// <param name="options">How to draw and how to format.</param>
    /// <param name="random">Where every draw comes from.</param>
    internal static string Generate(ThemeResolver resolver, GenerationOptions options, IRandomSource random) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(random);

        IReadOnlyList<NounEntry> nouns = resolver.Nouns;
        if (nouns.Count == 0) {
            // The same situation the validator reports, named by the same factory, travelling as
            // an exception because this overload promises a string. A theme loaded through any
            // catalog cannot reach here - only one built in memory by a caller can.
            throw ThemeErrors.NoNounToDrawFrom(resolver.Theme.Name).ToException();
        }

        NounEntry         noun     = nouns[random.Next(nouns.Count)];
        List<string> terms = [.. DrawPrefix(resolver, noun, options.SegmentMode, random), noun.Value];

        return SlugFormatter.Format(terms, SlugFormatter.DrawToken(options, random), options);
    }

    /// <summary>
    ///     The words that sit before the noun. Degrades silently rather than failing when the noun
    ///     reaches no participle: participle and either fall back to an adjective, both and
    ///     threeOrTwo drop to the adjective alone. A noun that reaches no adjective either - only
    ///     possible in a theme loaded under allowSmall - yields the noun on its own.
    /// </summary>
    /// <remarks>
    ///     A word may legitimately sit in both sections - "charming" and "boring" are adjectives and
    ///     present participles alike - so "both" can draw the same word twice. It is emitted once,
    ///     which is the same degradation as a noun that reaches no participle at all. Both draws are
    ///     still made, so the random source is consumed identically whether or not they collide:
    ///     re-drawing until they differ would have been unbounded on a pool of one, and would have
    ///     made a scripted draw unpredictable.
    /// </remarks>
    private static IEnumerable<string> DrawPrefix(ThemeResolver resolver,
                                                  NounEntry          noun,
                                                  SegmentMode   mode,
                                                  IRandomSource random) {
        IReadOnlyList<string> adjectives  = resolver.Pool(noun);
        IReadOnlyList<string> participles = resolver.ParticiplePool(noun);

        switch (mode) {
            case SegmentMode.Participle when participles.Count > 0:
                yield return Draw(participles, random);

                break;

            // Weighted by what the noun actually reaches, not a coin flip: "either" then means
            // drawing from the two pools as one, so every word before the noun has the same
            // chance whichever section declared it. A 50/50 split gave a pool of 20 participles
            // the same weight as 178 adjectives, which is not what the word says.
            case SegmentMode.Either when participles.Count                                 > 0
                                      && random.Next(adjectives.Count + participles.Count) < participles.Count:
                yield return Draw(participles, random);

                break;

            case SegmentMode.Both when participles.Count       > 0 && adjectives.Count > 0:
            case SegmentMode.ThreeOrTwo when participles.Count > 0 && adjectives.Count > 0:
                foreach (string word in DrawAPair(resolver, noun, adjectives, mode, random)) {
                    yield return word;
                }

                break;

            default:
                if (adjectives.Count > 0) {
                    yield return Draw(adjectives, random);
                }

                break;
        }
    }

    /// <summary>
    ///     An adjective, then the participle that may follow it. The adjective is drawn first and
    ///     the participle from what it leaves (DEC0017), so a refused pair never has to be undone.
    /// </summary>
    /// <remarks>
    ///     Under "threeOrTwo" the second draw runs over one candidate more than the pool holds, and
    ///     that extra one is the absence of a participle (DEC0020): a noun reaching 25 of them draws
    ///     over 26, so every participle and the absence are equally likely and the slug comes out
    ///     with two segments instead of three. Under "both" the bound is the pool itself, which is
    ///     what keeps a draw scripted against it reading as it did.
    /// </remarks>
    private static IEnumerable<string> DrawAPair(ThemeResolver         resolver,
                                                 NounEntry                  noun,
                                                 IReadOnlyList<string> adjectives,
                                                 SegmentMode           mode,
                                                 IRandomSource         random) {
        string adjective = Draw(adjectives, random);

        // Empty under allowSmall, where a theme was accepted without the floor that rules it
        // out, and under a ceiling that leaves no room behind this adjective (DEC0018). The noun
        // then keeps its adjective alone, exactly as a noun reaching no participle does - and
        // the draw is not made at all, so the absence never costs a value a script has to carry.
        IReadOnlyList<string> allowed = resolver.ParticiplePool(noun, adjective);

        yield return adjective;

        if (allowed.Count == 0) {
            yield break;
        }

        int candidates = mode == SegmentMode.ThreeOrTwo ? allowed.Count + 1 : allowed.Count;
        int drawn      = random.Next(candidates);
        if (drawn == allowed.Count) {
            yield break;
        }

        string participle = allowed[drawn];
        if (!string.Equals(participle, adjective, StringComparison.Ordinal)) {
            yield return participle;
        }
    }

    private static string Draw(IReadOnlyList<string> words, IRandomSource random) {
        return words[random.Next(words.Count)];
    }

    #endregion

}