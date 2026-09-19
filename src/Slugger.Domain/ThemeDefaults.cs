namespace Slugger.Domain;

/// <summary>
/// The <c>defaults</c> block of a theme file: the formatting preferences that belong to the
/// imitated style rather than to a session. Every member is optional, and <c>null</c> always
/// means "this theme says nothing about it" - never a value.
/// </summary>
public sealed record ThemeDefaults
{
    public static ThemeDefaults Empty { get; } = new();

    public char? Separator { get; init; }

    public Casing? Casing { get; init; }

    public int? TokenLength { get; init; }

    public bool? TokenHex { get; init; }

    /// <summary>Percentage, 0 to 100, that the token shows up at all once TokenLength is greater than zero.</summary>
    public int? TokenChance { get; init; }

    /// <summary>Glue the token to the previous segment with no separator, as Docker does.</summary>
    public bool? TokenGlued { get; init; }

    public SegmentMode? SegmentMode { get; init; }
}
