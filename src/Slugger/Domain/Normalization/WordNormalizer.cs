#region Usings declarations

using System.Text;

#endregion

namespace Slugger.Domain.Normalization;

/// <summary>
///     Steps 1 to 3 of value normalization, applied once when a value is read from the JSON:
///     trim, reduce everything that is neither a letter nor a digit to a single word boundary,
///     lowercase. Accents are preserved as written - what the theme file says is what the author
///     meant - because an accented letter is a letter.
/// </summary>
/// <remarks>
///     Step 4 - turning the remaining spaces into the separator - deliberately does
///     not happen here. The separator is only known at generation time, and in multi-theme
///     --mimic-style it varies from one draw to the next because each drawn theme applies its
///     own. Baking it in at load time would pick one separator for the whole run. It belongs to
///     <see cref="Generation.SlugFormatter" /> instead, which yields the same result for a single
///     theme and the correct one for several.
/// </remarks>
public static class WordNormalizer {

    #region Static members

    /// <summary>
    ///     Applies steps 1 to 3. A compound value keeps its internal single spaces, and everything
    ///     that separated its words - a space, an apostrophe, an ampersand, a hyphen - has become
    ///     one of them. The result holds letters, digits and single spaces, and nothing else.
    /// </summary>
    /// <remarks>
    ///     A boundary is anything that is neither a letter nor a digit, which is what keeps "rené"
    ///     intact while "jack o'neil" loses its apostrophe: an accented letter is a letter. Reducing
    ///     them here rather than at format time is deliberate - it needs no separator to be known,
    ///     so it belongs with the other load-time steps (DEC0005), and a run of them collapses the
    ///     same way runs of whitespace always have: "smith &amp; wesson" has one boundary, not three.
    /// </remarks>
    public static string Canonicalize(string value) {
        ArgumentNullException.ThrowIfNull(value);

        StringBuilder builder             = new(value.Length);
        bool          previousWasBoundary = false;

        foreach (char character in value) {
            bool isBoundary = !char.IsLetterOrDigit(character);
            if (isBoundary && previousWasBoundary) {
                continue;
            }

            builder.Append(isBoundary ? ' ' : char.ToLowerInvariant(character));
            previousWasBoundary = isBoundary;
        }

        // A leading or trailing boundary is now a space whatever it was written as, so trimming
        // once at the end does the work of step 1 for "!yahoo!" as well as for "  yahoo  ".
        return builder.ToString().Trim();
    }

    #endregion

}