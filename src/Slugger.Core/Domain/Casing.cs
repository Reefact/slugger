namespace Slugger.Domain;

/// <summary>Shape of the final slug (<c>--casing</c>).</summary>
public enum Casing
{
    /// <summary>Lowercase segments joined by the separator: <c>gorgeous-wandering-khorana</c>.</summary>
    Kebab,

    /// <summary>Lowercase segments joined by the separator, as Docker writes them: <c>focused_turing</c>.</summary>
    Snake,

    /// <summary>No separator between segments, each but the first capitalised: <c>gorgeousWanderingKhorana</c>.</summary>
    Camel
}
