#region Usings declarations

using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Domain.Generation;

/// <summary>
///     Generates slugs: draws a noun from a theme, the epithet that precedes it and the token that
///     follows it, then writes them out as <see cref="GenerationOptions" /> says.
/// </summary>
/// <remarks>
///     <para>
///         Every overload can be called from several threads at once with the same
///         <see cref="ThemeDocument" /> and the same options. The random source is the part to watch: a
///         <see cref="DefaultRandomSource" /> built with a seed is not thread-safe, so give each thread its
///         own.
///     </para>
///     <para>
///         A call takes microseconds and allocates a few kilobytes. Setting
///         <see cref="GenerationOptions.MaxLength" /> or <see cref="GenerationOptions.MaxSegmentWords" />
///         makes it milliseconds, as those properties explain.
///     </para>
/// </remarks>
public static class SlugGenerator {

    #region Static members

    /// <summary>
    ///     Generates one slug, drawing from a new source seeded with <see cref="GenerationOptions.Seed" />,
    ///     or from a shared one when there is no seed.
    /// </summary>
    /// <remarks>
    ///     <b>With a seed, every call returns the same slug</b>, because this overload builds a new random
    ///     source from <see cref="GenerationOptions.Seed" /> on each call. For a reproducible sequence,
    ///     create one <see cref="DefaultRandomSource" /> and pass it to every call of
    ///     <see cref="Generate(ThemeDocument, GenerationOptions, IRandomSource)" />. Without a seed, the
    ///     draws come from <see cref="Random.Shared" />, which is safe to use from several threads.
    /// </remarks>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to write the slug out.</param>
    /// <returns>The slug, written out.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="theme" /> or <paramref name="options" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <see cref="GenerationOptions.MaxLength" /> or <see cref="GenerationOptions.MaxSegmentWords" /> is below one.
    /// </exception>
    /// <exception cref="FirstClassErrors.DomainException">
    ///     No noun is left to draw: the theme holds none (<see cref="ThemeErrors.Codes.NoNounToDrawFrom" />),
    ///     or a limit in the options leaves none (<see cref="ThemeErrors.Codes.NothingFitsTheLimit" /> for
    ///     <see cref="GenerationOptions.MaxLength" />, <see cref="ThemeErrors.Codes.NoValueIsShortEnough" /> for
    ///     <see cref="GenerationOptions.MaxSegmentWords" />).
    /// </exception>
    public static string Generate(ThemeDocument theme, GenerationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        return Generate(theme, options, new DefaultRandomSource(options.Seed));
    }

    /// <summary>
    ///     Generates one slug, taking every draw from the source you pass. <see cref="GenerationOptions.Seed" />
    ///     is ignored.
    /// </summary>
    /// <remarks>
    ///     Pass the same source to every call of a run: one <c>new DefaultRandomSource(42)</c> shared by
    ///     the calls gives the same sequence of slugs each time the program runs.
    /// </remarks>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to write the slug out.</param>
    /// <param name="random">
    ///     Where every draw comes from. A <see cref="DefaultRandomSource" /> built with a seed must not be
    ///     shared between threads.
    /// </param>
    /// <returns>The slug, written out.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <see cref="GenerationOptions.MaxLength" /> or <see cref="GenerationOptions.MaxSegmentWords" /> is below one.
    /// </exception>
    /// <exception cref="FirstClassErrors.DomainException">
    ///     No noun is left to draw: the theme holds none (<see cref="ThemeErrors.Codes.NoNounToDrawFrom" />),
    ///     or a limit in the options leaves none (<see cref="ThemeErrors.Codes.NothingFitsTheLimit" /> for
    ///     <see cref="GenerationOptions.MaxLength" />, <see cref="ThemeErrors.Codes.NoValueIsShortEnough" /> for
    ///     <see cref="GenerationOptions.MaxSegmentWords" />).
    /// </exception>
    public static string Generate(ThemeDocument theme, GenerationOptions options, IRandomSource random) {
        ArgumentNullException.ThrowIfNull(theme);

        return Generate(ResolverFor(theme, options), options, random);
    }

    /// <summary>
    ///     Generates one slug from several themes: the picker draws a theme, weighted by how many nouns it
    ///     holds, and the slug is drawn inside that theme.
    /// </summary>
    /// <remarks>
    ///     The same options apply whichever theme is drawn, so no theme's own style is applied. To keep
    ///     each theme's style, call <see cref="WeightedThemePicker.Pick" /> yourself and pass the options
    ///     for that theme to <see cref="Generate(ThemeDocument, GenerationOptions, IRandomSource)" />: the
    ///     draws are the same.
    /// </remarks>
    /// <param name="themes">The themes to draw from.</param>
    /// <param name="options">How to draw and how to write the slug out.</param>
    /// <param name="random">
    ///     Where every draw comes from, the choice of theme included. A <see cref="DefaultRandomSource" />
    ///     built with a seed must not be shared between threads.
    /// </param>
    /// <returns>The slug, written out.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <see cref="GenerationOptions.MaxLength" /> or <see cref="GenerationOptions.MaxSegmentWords" /> is below one.
    /// </exception>
    /// <exception cref="FirstClassErrors.DomainException">
    ///     No noun is left to draw in the theme drawn: it holds none, or a limit in the options leaves none -
    ///     with the same codes as <see cref="Generate(ThemeDocument, GenerationOptions, IRandomSource)" />.
    /// </exception>
    public static string Generate(WeightedThemePicker themes, GenerationOptions options, IRandomSource random) {
        ArgumentNullException.ThrowIfNull(themes);

        ThemeDocument drawn = themes.Pick(random);

        return Generate(ResolverFor(drawn, options), options, random);
    }

    /// <summary>
    ///     The surface a run draws from, narrowed to what its budget leaves room for (DEC0018), or
    ///     the whole theme when it declares none.
    /// </summary>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to format, which is what decides the budget.</param>
    internal static ThemeResolver ResolverFor(ThemeDocument theme, GenerationOptions options) {
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
            // The same situation the validator reports, named by the same method, travelling as an
            // exception because this overload promises a string. A loaded theme always holds a noun,
            // but a length limit or a word cap in the options can leave it none, and generation does
            // not validate the surface they leave - so the refusal names the limit, not the theme.
            throw ThemeValidator.NothingToDraw(resolver).ToException();
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