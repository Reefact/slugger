namespace Slugger.Domain.Validation;

/// <summary>
/// <code>
/// combos(noun)     = |pool(noun)| * max(1, |partPool(noun)|)
/// combos(category) = sum of combos(noun) over every noun carrying that category
/// total            = sum of combos(noun) over every noun
/// </code>
/// The max(1, ...) keeps a noun with no participle from zeroing the count. A noun in several
/// categories contributes to each: this is a coverage check per branch, not a partition.
/// The participle is counted whether or not segmentMode ends up using it, because
/// segmentMode changes behaviour, not the theme's real combinatorial space.
/// </summary>
public sealed class ThemeCombinatorics
{
    public ThemeCombinatorics(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Theme = theme;
    }

    public Theme Theme { get; }

    public long CombinationsFor(Noun noun) => throw new NotImplementedException();

    public long CombinationsForCategory(string category) => throw new NotImplementedException();

    public long Total() => throw new NotImplementedException();
}
