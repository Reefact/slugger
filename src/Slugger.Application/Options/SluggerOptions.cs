using Slugger.Domain;

namespace Slugger.Application.Options;

/// <summary>
/// One layer of the precedence chain, as requested rather than as resolved: the explicit
/// command line, or the config saved by <c>--init</c>. Null always means "this layer says
/// nothing about it", which is what lets a lower layer speak.
/// </summary>
public sealed record SluggerOptions
{
    public static SluggerOptions Empty { get; } = new();

    public IReadOnlyList<string>? Themes { get; init; }

    public string? ThemeDirectory { get; init; }

    public char? Separator { get; init; }

    public Casing? Casing { get; init; }

    public SegmentMode? SegmentMode { get; init; }

    public int? TokenLength { get; init; }

    public bool? TokenHex { get; init; }

    public int? TokenChance { get; init; }

    public bool? TokenGlued { get; init; }

    public int? Count { get; init; }

    public int? Seed { get; init; }

    public bool? Oneshot { get; init; }

    public bool? Clipboard { get; init; }

    public MimicStyle? MimicStyle { get; init; }

    public bool? AllowSmallTheme { get; init; }
}
