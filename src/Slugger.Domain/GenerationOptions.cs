namespace Slugger.Domain;

/// <summary>
/// The effective generation levers, once the precedence chain has already been collapsed
/// (explicit argument, theme defaults, saved config, program default - see
/// Slugger.Application.Options.OptionResolver). The domain receives the outcome of that
/// chain and never resolves it itself.
/// </summary>
public sealed record GenerationOptions
{
    public static GenerationOptions Default { get; } = new();

    public char Separator { get; init; } = '-';

    public Casing Casing { get; init; } = Casing.Kebab;

    public SegmentMode SegmentMode { get; init; } = SegmentMode.Both;

    /// <summary>Length of the trailing token. Zero means no token at all.</summary>
    public int TokenLength { get; init; }

    public bool TokenHex { get; init; }

    public bool TokenGlued { get; init; }

    public int TokenChance { get; init; } = 100;

    /// <summary>Seed for a reproducible run (<c>--seed</c>). Null draws from a time based source.</summary>
    public int? Seed { get; init; }
}
