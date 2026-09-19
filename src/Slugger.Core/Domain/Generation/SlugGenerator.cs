using Slugger.Domain.Resolution;

namespace Slugger.Domain.Generation;

/// <summary>
/// Pick a theme, draw a noun, resolve its pool, draw the segment word or words, format.
/// The entry point a library consumer calls directly, without ever touching the CLI.
/// </summary>
public static class SlugGenerator
{
    /// <summary>Generates one slug, seeding the random source from <see cref="GenerationOptions.Seed"/>.</summary>
    public static string Generate(Theme theme, GenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Generate(theme, options, new DefaultRandomSource(options.Seed));
    }

    /// <summary>Generates one slug from an explicit random source.</summary>
    public static string Generate(Theme theme, GenerationOptions options, IRandomSource random) => throw new NotImplementedException();

    /// <summary>
    /// Generates from several themes at once, drawing the theme with
    /// <see cref="WeightedThemePicker"/> and the noun inside the theme it picked.
    /// </summary>
    public static string Generate(WeightedThemePicker themes, GenerationOptions options, IRandomSource random) => throw new NotImplementedException();
}
