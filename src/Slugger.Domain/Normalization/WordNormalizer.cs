using System.Text;

namespace Slugger.Domain.Normalization;

/// <summary>
/// Steps 1 to 3 of value normalization, applied once when a value is read from the JSON:
/// trim, collapse runs of whitespace into a single space, lowercase. Accents and special
/// characters are preserved as written - what the theme file says is what the author meant.
/// </summary>
/// <remarks>
/// Step 4 of the spec - turning the remaining spaces into the separator - deliberately does
/// not happen here. The separator is only known at generation time, and in multi-theme
/// --mimic-style it varies from one draw to the next because each drawn theme applies its
/// own. Baking it in at load time would pick one separator for the whole run. It belongs to
/// <see cref="Generation.SlugFormatter"/> instead, which yields the same result for a single
/// theme and the correct one for several.
/// </remarks>
public static class WordNormalizer
{
    /// <summary>Applies steps 1 to 3. A compound value keeps its internal single spaces.</summary>
    public static string Canonicalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.AsSpan().Trim();
        var builder = new StringBuilder(trimmed.Length);
        var previousWasSpace = false;

        foreach (var character in trimmed)
        {
            var isSpace = char.IsWhiteSpace(character);
            if (isSpace && previousWasSpace)
            {
                continue;
            }

            builder.Append(isSpace ? ' ' : char.ToLowerInvariant(character));
            previousWasSpace = isSpace;
        }

        return builder.ToString();
    }
}
