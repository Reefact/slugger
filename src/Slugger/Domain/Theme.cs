#region Usings declarations

using System.Diagnostics;

#endregion

namespace Slugger.Domain;

/// <summary>
///     A theme as the domain works with it: the nouns a slug is drawn from, under the name the
///     theme is known by.
/// </summary>
/// <remarks>
///     <para>
///         <b>An entity, so it is its name and not its contents.</b> A theme edited between two
///         reads is the same theme; two files that happen to carry the same nouns are two.
///         <see cref="ThemeDocument" /> is the other side of that - the shape of the file, values
///         all the way down - and the two are not the same thing written twice.
///     </para>
///     <para>
///         <b>Valid because it exists.</b> Nothing builds one from outside the assembly: a theme is
///         read, refused where it breaks a rule, and only then made. That is what lets
///         <see cref="ICatalog" /> promise what it promises, and it is why the guards below are
///         technical rather than first-class errors - by the time one fires, the refusal a reader
///         should have seen has already been skipped.
///     </para>
///     <para>
///         <b>A noun carries no exclusion.</b> What a noun refuses beside it is how a theme file
///         spells a pool conveniently, not something the word itself knows, so it is resolved when
///         the theme is read and never travels this far.
///     </para>
/// </remarks>
[Entity]
[DebuggerDisplay("{ToString()}")]
public sealed class Theme {

    #region Fields

    private readonly IReadOnlyList<Noun> _nouns;

    #endregion

    #region Constructors & Destructor

    /// <param name="name">The name the theme is known by, which is its identity.</param>
    /// <param name="nouns">What a slug is drawn from, already resolved and already validated.</param>
    internal Theme(ThemeName name, IReadOnlyList<Noun> nouns) {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(nouns);
        if (nouns.Count == 0) { throw new ArgumentException("A theme holds at least one noun to draw from.", nameof(nouns)); }

        Name   = name;
        _nouns = nouns;
    }

    #endregion

    /// <summary>The name it is known by, and the whole of what makes it this theme rather than another.</summary>
    public ThemeName Name { get; }

    /// <summary>How many nouns there are to draw from, which is never none.</summary>
    public int NounCount => _nouns.Count;

    /// <summary>The noun at that position, for a draw that has picked one.</summary>
    /// <param name="index">A position between zero and <see cref="NounCount" />, excluded.</param>
    /// <exception cref="ArgumentOutOfRangeException">The position names no noun of this theme.</exception>
    public Noun GetNoun(int index) {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _nouns.Count);

        return _nouns[index];
    }

    /// <summary>The name it is known by, for a human reading a watch window.</summary>
    public override string ToString() {
        return Name.ToString();
    }

    /// <summary>Two themes of the same name are the same theme, whatever either one currently holds.</summary>
    public override bool Equals(object? obj) {
        return obj is Theme other && Name.Equals(other.Name);
    }

    /// <summary>Hashed on the identity, for the same reason equality is decided by it.</summary>
    public override int GetHashCode() {
        return Name.GetHashCode();
    }

}
