namespace Slugger.Domain;

/// <summary>
/// What sits between the optional prefix and the noun (<c>--segment</c>, or the theme's
/// <c>defaults.segmentMode</c>). Four values rather than a visibility boolean.
/// </summary>
public enum SegmentMode
{
    /// <summary>An adjective only. Participles are never used, even when declared.</summary>
    Adjective,

    /// <summary>A participle only, drawn from partPool(noun).</summary>
    Participle,

    /// <summary>One or the other, decided per generation. Reproduces the single word prefix of Heroku and Docker.</summary>
    Either,

    /// <summary>Both, the three segment format slugger uses by default.</summary>
    Both
}
