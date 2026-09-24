#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     What a draw produces: a <see cref="Noun" />, an <see cref="Epithet" /> in front of it where
///     there is one, and a <see cref="Token" /> behind it where one was drawn.
/// </summary>
/// <remarks>
///     A slug is not a string. It becomes one at rendering, which chooses the separator, the casing
///     and the fold - so the same slug rendered twice differently gives two strings, and none of
///     them lives here.
/// </remarks>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Slug : ValueType<Slug> {

    #region Fields

    private readonly Epithet? _epithet;
    private readonly Noun     _noun;
    private readonly Token?   _token;

    #endregion

    #region Constructors & Destructor

    /// <summary>
    ///     A noun, what qualifies it where anything does, and the token that follows it where one
    ///     was drawn - which is the page's sentence in the page's order.
    /// </summary>
    /// <param name="noun">The noun drawn. The one part a slug cannot be without.</param>
    /// <param name="epithet">What was drawn in front of the noun, or null where nothing was.</param>
    /// <param name="token">The token drawn, or null where none was.</param>
    public Slug(Noun noun, Epithet? epithet = null, Token? token = null) {
        ArgumentNullException.ThrowIfNull(noun);

        _noun    = noun;
        _epithet = epithet;
        _token   = token;
    }

    #endregion

    /// <summary>
    ///     How many terms it carries: the noun, and whatever the epithet puts in front of it. One,
    ///     two or three - which is the figure a promise about a theme's length is made against.
    /// </summary>
    public int TermCount => _epithet is null ? 1 : _epithet.TermCount + 1;

    /// <summary>Whether a token follows the noun, which nothing about the terms can say.</summary>
    public bool HasToken => _token is not null;

    /// <summary>
    ///     Its parts dehydrated: the terms in the order they are written, and the token where one
    ///     was drawn. Which is what a formatter takes, and all it takes.
    /// </summary>
    [DehydrationMethod]
    public (IReadOnlyList<string> Segments, string? Token) Dehydrate() {
        List<string> segments = _epithet is null ? [] : [.. _epithet.Dehydrate()];
        segments.Add(_noun.Dehydrate());

        return (segments, _token?.Dehydrate());
    }

    /// <summary>
    ///     Its parts spaced, for a human reading a watch window. Not the slug a destination
    ///     receives: that one needs a separator, and a slug does not carry one.
    /// </summary>
    public override string ToString() {
        string written = _epithet is null ? _noun.ToString() : $"{_epithet} {_noun}";

        return _token is null ? written : $"{written} {_token}";
    }

    /// <summary>Two slugs of the same parts are the same slug, whatever draw produced them.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        if (_epithet is not null) { yield return _epithet; }

        yield return _noun;

        if (_token is not null) { yield return _token; }
    }

}
