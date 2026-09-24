#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     The term a slug is built around: always present, always last.
/// </summary>
[SemanticObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Noun : ValueType<Noun> {

    #region Static members

    /// <summary>That term, read as a noun.</summary>
    /// <param name="term">The term drawn.</param>
    public static Noun Of(Term term) {
        ArgumentNullException.ThrowIfNull(term);

        return new Noun(term);
    }

    #endregion

    #region Constructors & Destructor

    private Noun(Term term) {
        Value = term;
    }

    #endregion

    /// <summary>The term it is, which a formatter and a budget both want.</summary>
    public Term Value { get; }

    /// <summary>The term it reads, dehydrated.</summary>
    [DehydrationMethod]
    public string Dehydrate() {
        return Value.Dehydrate();
    }

    /// <summary>The term, for a human reading a watch window.</summary>
    public override string ToString() {
        return Value.ToString();
    }

    /// <summary>Two nouns of the same term are the same noun.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return Value;
    }

}
