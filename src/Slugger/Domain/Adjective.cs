#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     An epithet drawn from the theme's adjectives. The noun's categories decide which of them it reaches (DEC0001), and nothing else narrows it.
/// </summary>
/// <remarks>
///     The same term can be declared in both sections (DEC0013), so what tells an adjective from a
///     participle is never the term - it is which list it was drawn from. That is what this type
///     carries, and why <c>adjective.Value == participle.Value</c> can be true while the two are
///     not the same thing.
/// </remarks>
[SemanticObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Adjective : ValueType<Adjective> {

    #region Static members

    /// <summary>That term, read as an adjective.</summary>
    /// <param name="term">The term drawn.</param>
    public static Adjective Of(Term term) {
        ArgumentNullException.ThrowIfNull(term);

        return new Adjective(term);
    }

    #endregion

    #region Constructors & Destructor

    private Adjective(Term term) {
        Value = term;
    }

    #endregion

    /// <summary>The term it is, which a formatter and a budget both want.</summary>
    public Term Value { get; }

    /// <summary>The term, for a human reading a watch window.</summary>
    public override string ToString() {
        return Value.ToString();
    }

    /// <summary>Two adjectives of the same term are the same adjective.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return Value;
    }

}
