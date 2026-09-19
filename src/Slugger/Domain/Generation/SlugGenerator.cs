using Slugger.Domain.Resolution;

namespace Slugger.Domain.Generation;

/// <summary>
/// Pick a theme, draw a noun, resolve its pool, draw the segment word or words, format.
/// The entry point a library consumer calls directly, without ever touching the CLI.
/// </summary>
public static class SlugGenerator
{
    /// <summary>Generates one slug, seeding the random source from <see cref="GenerationOptions.Seed"/>.</summary>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to format.</param>
    public static string Generate(Theme theme, GenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Generate(theme, options, new DefaultRandomSource(options.Seed));
    }

    /// <summary>Generates one slug from an explicit random source.</summary>
    /// <param name="theme">The theme to draw from.</param>
    /// <param name="options">How to draw and how to format.</param>
    /// <param name="random">Where every draw comes from.</param>
    public static string Generate(Theme theme, GenerationOptions options, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return Generate(new ThemeResolver(theme), options, random);
    }

    /// <summary>
    /// Generates from several themes at once, drawing the theme with
    /// <see cref="WeightedThemePicker"/> and the noun inside the theme it picked.
    /// </summary>
    /// <param name="themes">The themes in scope, weighted by size.</param>
    /// <param name="options">How to draw and how to format.</param>
    /// <param name="random">Where every draw comes from.</param>
    public static string Generate(WeightedThemePicker themes, GenerationOptions options, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(themes);

        return Generate(new ThemeResolver(themes.Pick(random)), options, random);
    }

    private static string Generate(ThemeResolver resolver, GenerationOptions options, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(random);

        IReadOnlyList<Noun> nouns = resolver.Theme.Nouns;
        if (nouns.Count == 0)
        {
            throw new ArgumentException($"Theme \"{resolver.Theme.Name}\" holds no noun to draw.", nameof(resolver));
        }

        Noun noun = nouns[random.Next(nouns.Count)];
        List<string> segments = [.. DrawPrefix(resolver, noun, options.SegmentMode, random), noun.Value];

        return SlugFormatter.Format(segments, SlugFormatter.DrawToken(options, random), options);
    }

    /// <summary>
    /// The words that sit before the noun. Degrades silently rather than failing when the noun
    /// reaches no participle: participle and either fall back to an adjective, both drops to
    /// the adjective alone. A noun that reaches no adjective either - only possible in a theme
    /// loaded under allowSmall - yields the noun on its own.
    /// </summary>
    private static IEnumerable<string> DrawPrefix(
        ThemeResolver resolver,
        Noun noun,
        SegmentMode mode,
        IRandomSource random)
    {
        IReadOnlyList<string> adjectives = resolver.Pool(noun);
        IReadOnlyList<string> participles = resolver.ParticiplePool(noun);

        switch (mode)
        {
            case SegmentMode.Participle when participles.Count > 0:
                yield return Draw(participles, random);

                break;

            case SegmentMode.Either when participles.Count > 0 && (adjectives.Count == 0 || random.Next(2) == 0):
                yield return Draw(participles, random);

                break;

            case SegmentMode.Both when participles.Count > 0 && adjectives.Count > 0:
                yield return Draw(adjectives, random);
                yield return Draw(participles, random);

                break;

            default:
                if (adjectives.Count > 0)
                {
                    yield return Draw(adjectives, random);
                }

                break;
        }
    }

    private static string Draw(IReadOnlyList<string> words, IRandomSource random) => words[random.Next(words.Count)];
}
