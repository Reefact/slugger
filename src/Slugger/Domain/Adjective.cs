#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     An epithet drawn from the theme's adjectives. The noun's categories decide which of them it
///     reaches.
/// </summary>
/// <remarks>
///     <para>
///         <b>Not used by the engine yet.</b> This type belongs to an ongoing refactoring of the
///         library's vocabulary: loading and generation still work with <see cref="ThemeDocument" /> and
///         strings, and this type may change or disappear before they use it. Do not build on it yet.
///     </para>
///     <para>
///         The same term can be declared in both sections, so what tells an adjective from a participle
///         is never the term - it is which list it was drawn from. That is what this type holds, and why
///         <c>adjective.Value == participle.Value</c> can be true while the two are not the same thing.
///     </para>
///     <para>
///         See decision records DEC0001 and DEC0013 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0001-restriction-des-adjectifs-par-categorie.md and
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0013-mot-declare-dans-les-deux-sections.md
///     </para>
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

    /// <summary>The term it reads, dehydrated.</summary>
    [DehydrationMethod]
    public string Dehydrate() {
        return Value.Dehydrate();
    }

    /// <summary>The term, for a human reading a watch window.</summary>
    public override string ToString() {
        return Value.ToString();
    }

    /// <summary>Two adjectives of the same term are the same adjective.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return Value;
    }

}
