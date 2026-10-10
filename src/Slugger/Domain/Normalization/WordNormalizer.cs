#region Usings declarations

using System.Text;

#endregion

namespace Slugger.Domain.Normalization;

/// <summary>
///     How a theme's words are cleaned up when the theme is read: trimmed, every run of characters that
///     are neither letters nor digits reduced to a single space, and lowercased. Accents are kept as
///     written - what the theme file says is what its author meant, and an accented letter is a letter.
/// </summary>
/// <remarks>
///     <para>
///         Turning the remaining spaces into the separator does not happen here. The separator belongs
///         to the options a slug is written with, which can differ from one draw to the next while the
///         theme is read once, so <see cref="Generation.SlugFormatter" /> does it when the slug is
///         written.
///     </para>
///     <para>
///         See decision record DEC0005 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0005-format-resolu-au-tirage.md
///     </para>
/// </remarks>
public static class WordNormalizer {

    #region Static members

    /// <summary>
    ///     Cleans up one value as a theme is read. A compound value keeps single spaces between its
    ///     words, and everything that separated them - a space, an apostrophe, an ampersand, a hyphen -
    ///     has become one. The result holds lowercase letters, digits and single spaces, and nothing else.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A boundary is anything that is neither a letter nor a digit, which is what keeps "rené"
    ///         intact while "jack o'neil" loses its apostrophe. A run of boundaries collapses into one:
    ///         "smith &amp; wesson" has one boundary, not three.
    ///     </para>
    ///     <para>
    ///         See decision record DEC0008 (in French):
    ///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0008-reduction-des-caracteres-non-alphanumeriques.md
    ///     </para>
    /// </remarks>
    /// <param name="value">The value as a theme file writes it.</param>
    /// <returns>The value cleaned up, possibly empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is null.</exception>
    public static string Canonicalize(string value) {
        ArgumentNullException.ThrowIfNull(value);

        StringBuilder builder             = new(value.Length);
        bool          previousWasBoundary = false;

        foreach (char character in value) {
            bool isBoundary = !char.IsLetterOrDigit(character);
            if (isBoundary && previousWasBoundary) { continue; }

            builder.Append(isBoundary ? ' ' : char.ToLowerInvariant(character));
            previousWasBoundary = isBoundary;
        }

        // A leading or trailing boundary is now a space whatever it was written as, so trimming
        // once at the end does the work of step 1 for "!yahoo!" as well as for "  yahoo  ".
        return builder.ToString().Trim();
    }

    #endregion

}