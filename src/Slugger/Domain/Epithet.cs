#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     What qualifies the noun, whole: an adjective, a participle, or both. One or two terms,
///     never none - a slug that carries no qualification has no epithet rather than an empty one.
/// </summary>
/// <remarks>
///     Its three shapes are the five segment modes seen from the other end: "adjective" and
///     "participle" each make one of the single forms, "either" makes one or the other, "both"
///     makes the pair, and "threeOrTwo" makes the pair or the adjective alone when the noun reaches
///     no participle (DEC0020).
/// </remarks>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Epithet : ValueType<Epithet> {

    #region Fields

    private readonly Adjective?  _adjective;
    private readonly Participle? _participle;

    #endregion

    #region Constructors & Destructor

    /// <summary>An adjective, alone in front of the noun.</summary>
    /// <param name="adjective">The adjective drawn.</param>
    public Epithet(Adjective adjective) {
        ArgumentNullException.ThrowIfNull(adjective);

        _adjective = adjective;
    }

    /// <summary>A participle, alone in front of the noun.</summary>
    /// <param name="participle">The participle drawn.</param>
    public Epithet(Participle participle) {
        ArgumentNullException.ThrowIfNull(participle);

        _participle = participle;
    }

    /// <summary>Both, in the order they reach the slug - the adjective first, as DEC0017 draws them.</summary>
    /// <param name="adjective">The adjective drawn.</param>
    /// <param name="participle">The participle drawn from what that adjective leaves.</param>
    public Epithet(Adjective adjective, Participle participle) {
        ArgumentNullException.ThrowIfNull(adjective);
        ArgumentNullException.ThrowIfNull(participle);

        _adjective  = adjective;
        _participle = participle;
    }

    #endregion

    /// <summary>How many terms it puts in front of the noun: one, or two.</summary>
    public int TermCount => _adjective is not null && _participle is not null ? 2 : 1;

    /// <summary>Its terms, dehydrated, in the order they reach the slug.</summary>
    [DehydrationMethod]
    public IReadOnlyList<string> Dehydrate() {
        List<string> terms = [];
        if (_adjective is not null) { terms.Add(_adjective.Dehydrate()); }
        if (_participle is not null) { terms.Add(_participle.Dehydrate()); }

        return terms;
    }

    /// <summary>The terms it carries, spaced, for a human reading a watch window.</summary>
    public override string ToString() {
        if (_adjective is null) { return _participle!.ToString(); }
        if (_participle is null) { return _adjective.ToString(); }

        return $"{_adjective} {_participle}";
    }

    /// <summary>
    ///     The roles it holds, in order. An adjective and a participle of the same term make two
    ///     different epithets, which is what the roles are there to say.
    /// </summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        if (_adjective is not null) { yield return _adjective; }
        if (_participle is not null) { yield return _participle; }
    }

}
