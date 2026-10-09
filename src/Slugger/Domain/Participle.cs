#region Usings declarations

using System.Diagnostics;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     An epithet drawn from the theme's participles. Which ones it can be depends on the noun, and on
///     the adjective already drawn, which can refuse some of them.
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
///         See decision records DEC0013 and DEC0017 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0013-mot-declare-dans-les-deux-sections.md and
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md
///     </para>
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

    /// <summary>The term it reads, dehydrated.</summary>
    [DehydrationMethod]
    public string Dehydrate() {
        return Value.Dehydrate();
    }

    /// <summary>The term, for a human reading a watch window.</summary>
    public override string ToString() {
        return Value.ToString();
    }

    /// <summary>Two participles of the same term are the same participle.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return Value;
    }

}
