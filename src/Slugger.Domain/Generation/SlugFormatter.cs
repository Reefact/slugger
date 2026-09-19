namespace Slugger.Domain.Generation;

/// <summary>
/// Assembles the drawn segments into the final slug:
/// <code>
/// &lt;adjective&gt;[sep](&lt;participle&gt;[sep])?&lt;noun&gt;([sep]&lt;token&gt;)?
/// </code>
/// This is also where step 4 of normalization happens - the internal spaces of a compound
/// value become the separator - for the reason spelled out on
/// <see cref="Normalization.WordNormalizer"/>.
/// </summary>
public static class SlugFormatter
{
    /// <summary>Joins the segments, applies the casing, and appends the token when one was drawn.</summary>
    public static string Format(IReadOnlyList<string> segments, string? token, GenerationOptions options) => throw new NotImplementedException();

    /// <summary>
    /// Draws the optional trailing token. Returns null when TokenLength is zero, or when the
    /// TokenChance roll came up short - a rare token simulates a collision suffix without
    /// implementing real collision detection.
    /// </summary>
    public static string? DrawToken(GenerationOptions options, IRandomSource random) => throw new NotImplementedException();
}
