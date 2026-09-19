using System.Text;

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
    private const string DecimalDigits = "0123456789";
    private const string HexadecimalDigits = "0123456789abcdef";

    /// <summary>Joins the segments, applies the casing, and appends the token when one was drawn.</summary>
    /// <param name="segments">The drawn words, in order, already canonicalised to lowercase.</param>
    /// <param name="token">The trailing token, or null when none was drawn.</param>
    /// <param name="options">The separator, casing and gluing to apply.</param>
    public static string Format(IReadOnlyList<string> segments, string? token, GenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(options);

        return options.Casing == Casing.Camel
            ? FormatCamel(segments, token)
            : FormatSeparated(segments, token, options);
    }

    /// <summary>
    /// Draws the optional trailing token. Returns null when TokenLength is zero, or when the
    /// TokenChance roll came up short - a rare token simulates a collision suffix without
    /// implementing real collision detection.
    /// </summary>
    /// <param name="options">The token's length, alphabet and likelihood.</param>
    /// <param name="random">Where the draw comes from.</param>
    public static string? DrawToken(GenerationOptions options, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(random);

        if (options.TokenLength <= 0 || options.TokenChance <= 0)
        {
            return null;
        }

        // Next(100) lands in 0..99, so a chance of 100 always draws and one of 1 draws a
        // hundredth of the time - which is how docker's collision digit stays rare.
        if (options.TokenChance < 100 && random.Next(100) >= options.TokenChance)
        {
            return null;
        }

        string alphabet = options.TokenHex ? HexadecimalDigits : DecimalDigits;
        StringBuilder token = new(options.TokenLength);
        for (int position = 0; position < options.TokenLength; position++)
        {
            token.Append(alphabet[random.Next(alphabet.Length)]);
        }

        return token.ToString();
    }

    private static string FormatSeparated(IReadOnlyList<string> segments, string? token, GenerationOptions options)
    {
        string separator = options.Separator.ToString();
        // Step 4 of normalization: a compound value's internal spaces become the separator, so
        // "john doe" joins the rest of the slug as one token rather than opening a hole in it.
        string slug = string.Join(separator, segments.Select(segment => segment.Replace(" ", separator, StringComparison.Ordinal)));

        if (token is null)
        {
            return slug;
        }

        return options.TokenGlued ? slug + token : slug + separator + token;
    }

    /// <summary>
    /// Camel has no separator to speak of, so the token is appended directly whether or not
    /// TokenGlued is set: there is nothing to glue it with.
    /// </summary>
    private static string FormatCamel(IReadOnlyList<string> segments, string? token)
    {
        StringBuilder slug = new();
        foreach (string word in segments.SelectMany(segment => segment.Split(' ', StringSplitOptions.RemoveEmptyEntries)))
        {
            slug.Append(slug.Length == 0 ? word : Capitalise(word));
        }

        return token is null ? slug.ToString() : slug.Append(token).ToString();
    }

    private static string Capitalise(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..];
}
