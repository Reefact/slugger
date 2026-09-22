#region Usings declarations

using System.Globalization;
using System.Text;

using Slugger.Domain.Normalization;

#endregion

namespace Slugger.Domain.Generation;

/// <summary>
///     Assembles the drawn segments into the final slug:
///     <code>
/// &lt;adjective&gt;[sep](&lt;participle&gt;[sep])?&lt;noun&gt;([sep]&lt;token&gt;)?
/// </code>
///     This is also where step 4 of normalization happens - the internal spaces of a compound
///     value become the separator - for the reason spelled out on
///     <see cref="Normalization.WordNormalizer" />.
/// </summary>
public static class SlugFormatter {

    private const string DecimalDigits     = "0123456789";
    private const string HexadecimalDigits = "0123456789abcdef";

    #region Static members

    /// <summary>Joins the segments, applies the casing, and appends the token when one was drawn.</summary>
    /// <param name="segments">The drawn words, in order, already canonicalised to lowercase.</param>
    /// <param name="token">The trailing token, or null when none was drawn.</param>
    /// <param name="options">The separator, casing and gluing to apply.</param>
    public static string Format(IReadOnlyList<string> segments, string? token, GenerationOptions options) {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(options);

        // Folded here rather than at load time, and for the same reason step 4 is: it depends on
        // an option, and the theme is written once while the option varies from run to run.
        // Ascii implies the fold, so the stronger one is tested first and they never stack.
        IReadOnlyList<string> words = options switch {
            { Ascii      : true } => [.. segments.Select(ToAscii).Where(segment => segment.Length > 0)],
            { FoldAccents: true } => [.. segments.Select(Fold)],
            _                     => segments
        };

        return options.Casing == Casing.Camel
            ? FormatCamel(words, token)
            : FormatSeparated(words, token, options);
    }

    /// <summary>
    ///     Draws the optional trailing token. Returns null when TokenLength is zero, or when the
    ///     TokenChance roll came up short - a rare token simulates a collision suffix without
    ///     implementing real collision detection.
    /// </summary>
    /// <param name="options">The token's length, alphabet and likelihood.</param>
    /// <param name="random">Where the draw comes from.</param>
    public static string? DrawToken(GenerationOptions options, IRandomSource random) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(random);

        if (options.TokenLength <= 0 || options.TokenChance <= 0) {
            return null;
        }

        // Next(100) lands in 0..99, so a chance of 100 always draws and one of 1 draws a
        // hundredth of the time - which is how docker's collision digit stays rare.
        if (options.TokenChance < 100 && random.Next(100) >= options.TokenChance) {
            return null;
        }

        string        alphabet = options.TokenHex ? HexadecimalDigits : DecimalDigits;
        StringBuilder token    = new(options.TokenLength);
        for (int position = 0; position < options.TokenLength; position++) {
            token.Append(alphabet[random.Next(alphabet.Length)]);
        }

        return token.ToString();
    }

    /// <summary>
    ///     Drops the diacritic a letter carries by decomposing it and keeping everything that is not
    ///     a combining mark: "françois" becomes "francois", "risqué" becomes "risque".
    /// </summary>
    /// <remarks>
    ///     Only what decomposes folds. "ß", "ø" and "œ" have no decomposition, and no non-Latin
    ///     script does either, so they come through as written - deliberately, and the reason the
    ///     option is named for what it does rather than for an ASCII result it cannot promise.
    /// </remarks>
    private static string Fold(string value) {
        StringBuilder folded = new(value.Length);
        foreach (char character in value.Normalize(NormalizationForm.FormD)) {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) {
                folded.Append(character);
            }
        }

        // Back to composed form: a letter that lost no mark must come out exactly as it went in.
        return folded.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    ///     Folds, then drops every character that is still outside ASCII: "straße" becomes "strae",
    ///     "москва" becomes nothing at all.
    /// </summary>
    /// <remarks>
    ///     Dropped rather than turned into a boundary, because by the time this runs every character
    ///     left is a letter, a digit or a single space - normalization reduced everything else at
    ///     load (DEC0008). Turning a letter into a boundary would not spoil the spelling, it would
    ///     split a word that was never split, and a slug's segments carry meaning. "søren straße"
    ///     gives "sren strae", two words still, where a boundary would have given four.
    ///     A value that comes back empty is dropped from the slug by the caller rather than joined
    ///     as a hole, which is the whole difference between disfiguring a slug and breaking it.
    /// </remarks>
    private static string ToAscii(string value) {
        string ascii = new([.. Fold(value).Where(char.IsAscii)]);

        return WordNormalizer.Canonicalize(ascii);
    }

    private static string FormatSeparated(IReadOnlyList<string> segments, string? token, GenerationOptions options) {
        string separator   = options.Separator.ToString();
        // Step 4 of normalization: a compound value's internal spaces are closed up, so "john doe"
        // joins the rest of the slug as one token rather than opening a hole in it. With nothing
        // said, the separator does it; a word separator of its own is what keeps the segment
        // boundary legible - "gorgeous-john_doe" says where the noun starts.
        string insideAWord = options.WordSeparator ?? separator;
        string slug        = string.Join(separator, segments.Select(segment => segment.Replace(" ", insideAWord, StringComparison.Ordinal)));

        if (token is null) {
            return slug;
        }

        return options.TokenGlued ? slug + token : slug + separator + token;
    }

    /// <summary>
    ///     Camel has no separator to speak of, so the token is appended directly whether or not
    ///     TokenGlued is set: there is nothing to glue it with.
    /// </summary>
    private static string FormatCamel(IReadOnlyList<string> segments, string? token) {
        StringBuilder slug = new();
        foreach (string word in segments.SelectMany(segment => segment.Split(' ', StringSplitOptions.RemoveEmptyEntries))) {
            slug.Append(slug.Length == 0 ? word : Capitalise(word));
        }

        return token is null ? slug.ToString() : slug.Append(token).ToString();
    }

    private static string Capitalise(string word) {
        return word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..];
    }

    #endregion

}