namespace Slugger.Domain;

/// <summary>
/// The effective generation levers, once the precedence chain has already been collapsed
/// (explicit argument, theme defaults, saved config, program default - see
/// Slugger.Application.Options.OptionResolver). The domain receives the outcome of that
/// chain and never resolves it itself.
/// </summary>
public sealed record GenerationOptions
{
    /// <summary>What slugger does when nothing else says otherwise.</summary>
    public static GenerationOptions Default { get; } = new();

    /// <summary>Joins the segments, and replaces the internal spaces of a compound value.</summary>
    public char Separator { get; init; } = '-';

    /// <summary>Shape of the assembled slug.</summary>
    public Casing Casing { get; init; } = Casing.Kebab;

    /// <summary>Which words sit before the noun.</summary>
    public SegmentMode SegmentMode { get; init; } = SegmentMode.Both;

    /// <summary>Length of the trailing token. Zero means no token at all.</summary>
    public int TokenLength { get; init; }

    /// <summary>Draw the token in hexadecimal rather than decimal digits.</summary>
    public bool TokenHex { get; init; }

    /// <summary>Append the token with no separator before it.</summary>
    public bool TokenGlued { get; init; }

    /// <summary>
    /// Percentage, 0 to 100, that a token is drawn at all. A value in between simulates a rare
    /// suffix - Docker's collision digit - without implementing collision detection.
    /// </summary>
    public int TokenChance { get; init; } = 100;

    /// <summary>Seed for a reproducible run (<c>--seed</c>). Null draws from a time based source.</summary>
    public int? Seed { get; init; }
}
