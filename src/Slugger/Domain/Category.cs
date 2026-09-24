#region Usings declarations

using System.Diagnostics;

using FirstClassErrors;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     The label a noun and a section of adjectives share, which is how a noun reaches the words
///     that can describe it (DEC0001). It is never drawn and never reaches a slug, so it is no
///     term: a category is a name for a group, not a word.
/// </summary>
/// <remarks>
///     It matches whatever the case, which the theme file does not do today: categories are read
///     raw while exclusions are canonicalised, and the reason given for canonicalising those - a
///     match missed on casing fails open - holds here just as well. A noun labelled "Common" that
///     silently reaches nothing is the failure this closes.
/// </remarks>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Category : ValueType<Category> {

    #region Static members

    /// <summary>The category that value names, or the reason it names none.</summary>
    /// <param name="value">A label, of any characters. Surrounding whitespace is trimmed off.</param>
    public static Outcome<Category> From(string value) {
        ArgumentNullException.ThrowIfNull(value);

        string trimmed = value.Trim();
        if (trimmed.Length == 0) { return Outcome<Category>.Failure(CategoryError.Empty(value)); }

        return Outcome<Category>.Success(new Category(trimmed.ToLowerInvariant()));
    }

    /// <summary>The same, for a caller that has no report to fill.</summary>
    /// <param name="value">A label, of any characters. Surrounding whitespace is trimmed off.</param>
    /// <exception cref="CategoryException">The value names no category; the exception carries the reason.</exception>
    public static Category FromOrThrow(string value) {
        Outcome<Category> outcome = From(value);
        if (outcome.Error is CategoryError refused) { throw refused.ToException(); }

        return outcome.GetResultOrThrow();
    }

    #endregion

    #region Fields

    private readonly string _name;

    #endregion

    #region Constructors & Destructor

    private Category(string name) {
        _name = name;
    }

    #endregion

    /// <summary>The name it holds.</summary>
    [DehydrationMethod]
    public string Dehydrate() {
        return _name;
    }

    /// <summary>The category, for a human reading a watch window.</summary>
    public override string ToString() {
        return _name;
    }

    /// <summary>Two categories of the same name are the same category, whatever case wrote them.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _name;
    }

}
