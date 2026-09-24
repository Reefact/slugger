namespace Slugger.Domain;

/// <summary>Shape of the final slug (<c>--casing</c>).</summary>
public enum Casing {

    /// <summary>Lowercase terms joined by the separator: <c>gorgeous-wandering-khorana</c>.</summary>
    Kebab,

    /// <summary>Lowercase terms joined by the separator, as Docker writes them: <c>focused_turing</c>.</summary>
    Snake,

    /// <summary>No separator at all, each word but the first capitalised: <c>gorgeousWanderingKhorana</c>.</summary>
    Camel

}