namespace Slugger.Domain.Generation;

/// <summary>
/// How long a slug comes out, asked one candidate at a time, so that a pool can be reduced to
/// what fits before anything is drawn (DEC0018).
/// </summary>
/// <remarks>
/// <para>
/// It <b>formats</b> rather than counts. The separator, the word separator inside a compound
/// value, camel casing which has no separator at all, the fold and the ASCII pass which can make
/// a word shorter, and the token: each of them changes the answer, and arithmetic reproducing
/// all six beside <see cref="SlugFormatter"/> is arithmetic that drifts from it. Formatting the
/// candidate costs a string per word per noun, once, and cannot be wrong.
/// </para>
/// <para>
/// The token is counted as drawn even when <see cref="GenerationOptions.TokenChance"/> makes it
/// rare: a budget is about the worst case, and a slug that only fits when the token does not
/// show up does not fit.
/// </para>
/// </remarks>
public sealed class SlugBudget
{
    private readonly GenerationOptions _options;
    private readonly string? _token;

    /// <param name="maxLength">The most characters the finished slug may carry.</param>
    /// <param name="options">How the slug will be formatted, which is what decides its length.</param>
    public SlugBudget(int maxLength, GenerationOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);
        ArgumentNullException.ThrowIfNull(options);

        MaxLength = maxLength;
        _options = options;
        _token = Placeholder(options);
    }

    /// <summary>The most characters the finished slug may carry.</summary>
    public int MaxLength { get; }

    /// <summary>What the run puts in front of the noun, which the theme's own defaults no longer decide.</summary>
    public SegmentMode SegmentMode => _options.SegmentMode;

    /// <summary>Whether that mode draws two words rather than one, <b>always</b>.</summary>
    /// <remarks>
    /// "threeOrTwo" is deliberately not one of them, and the omission is the point rather than an
    /// oversight (DEC0020). It may draw one word, so no room is reserved in front of the noun: a
    /// long adjective stays in the pool, and when nothing fits behind it the narrowed participle
    /// pool comes back empty and the absence is all that is left to draw. The ceiling holds
    /// either way, and the theme keeps adjectives that "both" has to throw away.
    /// </remarks>
    public bool DrawsTwoWords => SegmentMode == SegmentMode.Both;

    /// <summary>How long these segments come out once formatted, token included.</summary>
    /// <param name="segments">The drawn words, in order, the noun last.</param>
    /// <param name="options">How the slug will be formatted.</param>
    public static int LengthOf(IReadOnlyList<string> segments, GenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return SlugFormatter.Format(segments, Placeholder(options), options).Length;
    }

    /// <summary>Whether these segments, in this order, fit once formatted.</summary>
    /// <param name="segments">The drawn words, in order, the noun last.</param>
    public bool Fits(params string[] segments) =>
        SlugFormatter.Format(segments, _token, _options).Length <= MaxLength;

    /// <summary>
    /// A token of the right length and of no particular value: its digits change what the slug
    /// says, never how long it is.
    /// </summary>
    private static string? Placeholder(GenerationOptions options) =>
        options.TokenLength > 0 ? new string('0', options.TokenLength) : null;
}
