namespace Slugger;

/// <summary>
///     One rule, measured on a theme: its worst case, what it is held to, and the room left between the
///     two - one row of <see cref="ThemeMeasurements.Margins" />.
/// </summary>
public sealed class ThemeMargin {

    #region Constructors & Destructor

    internal ThemeMargin(ThemeRule rule, long worstCase, long limit, string? subject = null, string? beside = null) {
        Rule      = rule;
        WorstCase = worstCase;
        Limit     = limit;
        Subject   = subject;
        Beside    = beside;
    }

    #endregion

    /// <summary>The rule measured.</summary>
    public ThemeRule Rule { get; }

    /// <summary>The worst value the theme reaches for this rule: the poorest noun, the longest slug...</summary>
    public long WorstCase { get; }

    /// <summary>
    ///     What the rule holds the theme to: a floor to reach, or, for <see cref="ThemeRule.CharactersInTwoWords" />
    ///     and <see cref="ThemeRule.CharactersInThreeWords" />, a ceiling to stay under.
    /// </summary>
    public long Limit { get; }

    /// <summary>
    ///     The room left: positive when the theme clears the rule, negative when it misses it. Counted
    ///     the way the rule points - above a floor, below a ceiling.
    /// </summary>
    public long Margin => IsCeiling ? Limit - WorstCase : WorstCase - Limit;

    /// <summary>
    ///     What the worst case is about - the noun, the category or the slug - or null where the rule
    ///     measures the theme as a whole.
    /// </summary>
    public string? Subject { get; }

    /// <summary>Whether the limit is a ceiling to stay under rather than a floor to reach.</summary>
    private bool IsCeiling => Rule is ThemeRule.CharactersInTwoWords or ThemeRule.CharactersInThreeWords;

    /// <summary>
    ///     The adjective the noun is measured beside, for <see cref="ThemeRule.ParticiplesBesideAnAdjective" />
    ///     only; null otherwise.
    /// </summary>
    public string? Beside { get; }

}
