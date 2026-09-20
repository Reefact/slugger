namespace Slugger.Domain;

/// <summary>
/// The <c>defaults</c> block of a theme file: the formatting preferences that belong to the
/// imitated style rather than to a session. Every member is optional, and <c>null</c> always
/// means "this theme says nothing about it" - never a value.
/// </summary>
/// <remarks>
/// Only what makes up a style's visual identity belongs here. A session preference -
/// <c>--count</c>, <c>--seed</c>, <c>--theme</c>, <c>--oneshot</c>, <c>--clipboard</c> - has
/// nothing to do with the theme and is deliberately absent.
/// </remarks>
public sealed record ThemeDefaults
{
    /// <summary>A theme that states no preference at all.</summary>
    public static ThemeDefaults Empty { get; } = new();

    /// <summary>Docker writes <c>_</c>, Heroku and slugger write <c>-</c>.</summary>
    public char? Separator { get; init; }

    /// <summary>
    /// What joins the words of a compound value, when the style wants something other than
    /// <see cref="Separator"/>. An empty string glues them.
    /// </summary>
    public string? WordSeparator { get; init; }

    /// <summary>Kept consistent with the style's historical separator.</summary>
    public Casing? Casing { get; init; }

    /// <summary>Length of the trailing token: Heroku ends on four digits, Docker on one.</summary>
    public int? TokenLength { get; init; }

    /// <summary>Whether that token is hexadecimal rather than decimal.</summary>
    public bool? TokenHex { get; init; }

    /// <summary>Percentage, 0 to 100, that the token shows up at all once TokenLength is greater than zero.</summary>
    public int? TokenChance { get; init; }

    /// <summary>Glue the token to the previous segment with no separator, as Docker does.</summary>
    public bool? TokenGlued { get; init; }

    /// <summary>How many words sit before the noun, and whether they are adjectives or participles.</summary>
    public SegmentMode? SegmentMode { get; init; }
}
