namespace Slugger.Domain.Generation;

/// <summary>
///     A length limit for slugs written with given options: it tells whether a set of terms fits once
///     written out, token included. Pass one to <see cref="Resolution.ThemeResolver" /> to leave out of a
///     theme the words that do not fit, before anything is drawn.
/// </summary>
/// <remarks>
///     <para>
///         It <b>writes the slug out</b> rather than counting. The separator, the separator inside a
///         compound term, camel case which has no separator at all, accent folding and ASCII which can
///         make a word shorter, and the token: each of them changes the answer, and arithmetic
///         reproducing all of them beside <see cref="SlugFormatter" /> would drift from it.
///     </para>
///     <para>
///         The token is counted at its full length even when <see cref="GenerationOptions.TokenChance" />
///         makes it rare: a limit is about the worst case, and a slug that only fits when the token does
///         not show up does not fit.
///     </para>
///     <para>
///         It answers how long a slug is, never what is drawn: the segment mode belongs to
///         <see cref="Resolution.ThemeResolver" />.
///     </para>
///     <para>
///         See decision record DEC0018 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md
///     </para>
/// </remarks>
public sealed class SlugBudget {

    #region Static members

    /// <summary>
    ///     How long these terms come out once written with these options, a token of the full length
    ///     included.
    /// </summary>
    /// <param name="terms">The terms, in order, the noun last.</param>
    /// <param name="options">How the slug will be written out.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static int LengthOf(IReadOnlyList<string> terms, GenerationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        return SlugFormatter.Format(terms, Placeholder(options), options).Length;
    }

    /// <summary>
    ///     A token of the right length and of no particular value: its digits change what the slug
    ///     says, never how long it is.
    /// </summary>
    private static string? Placeholder(GenerationOptions options) {
        return options.TokenLength > 0 ? new string('0', options.TokenLength) : null;
    }

    #endregion

    #region Fields

    private readonly GenerationOptions _options;
    private readonly string?           _token;

    #endregion

    #region Constructors & Destructor

    /// <summary>A limit of <paramref name="maxLength" /> characters for slugs written with these options.</summary>
    /// <param name="maxLength">The most characters the finished slug may have, counted as <see cref="string.Length" /> counts them.</param>
    /// <param name="options">How the slug will be written out, which is what decides its length.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength" /> is zero or negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is null.</exception>
    public SlugBudget(int maxLength, GenerationOptions options) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);
        ArgumentNullException.ThrowIfNull(options);

        MaxLength = maxLength;
        _options  = options;
        _token    = Placeholder(options);
    }

    #endregion

    /// <summary>The most characters the finished slug may have.</summary>
    public int MaxLength { get; }

    /// <summary>Whether these terms, in this order, fit within the limit once written out.</summary>
    /// <param name="terms">The terms, in order, the noun last.</param>
    public bool Fits(params string[] terms) {
        return SlugFormatter.Format(terms, _token, _options).Length <= MaxLength;
    }

}