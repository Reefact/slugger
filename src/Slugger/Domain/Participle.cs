#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     An epithet drawn from the theme's participles. Its pool follows the noun and the adjective already drawn, which can refuse some of it (DEC0017).
/// </summary>
/// <remarks>
///     The same term can be declared in both sections (DEC0013), so what tells an adjective from a
///     participle is never the term - it is which list it was drawn from. That is what this type
///     carries, and why <c>adjective.Value == participle.Value</c> can be true while the two are
///     not the same thing.
/// </remarks>
[SemanticObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Participle : ValueType<Participle> {

    #region Static members

    /// <summary>That term, read as a participle.</summary>
    /// <param name="term">The term drawn.</param>
    public static Participle Of(Term term) {
        ArgumentNullException.ThrowIfNull(term);

        return new Participle(term);
    }

    #endregion

    #region Constructors & Destructor

    private Participle(Term term) {
        Value = term;
    }

    #endregion

    /// <summary>The term it is, which a formatter and a budget both want.</summary>
    public Term Value { get; }

    /// <summary>The term, for a human reading a watch window.</summary>
    public override string ToString() {
        return Value.ToString();
    }

    /// <summary>Two participles of the same term are the same participle.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return Value;
    }

}
