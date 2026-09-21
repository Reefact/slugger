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
/// <para>
/// It answers how long, never what is drawn. <c>ThemeResolver.AskedMode</c> holds the run's
/// segment mode and <c>SegmentModes</c> the questions asked of it, because a run declares a mode
/// whether or not it declares a ceiling - answering that from here made the mode reachable only
/// through a budget, and <c>--segment</c> silently missed the floors without <c>--max-length</c>.
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
