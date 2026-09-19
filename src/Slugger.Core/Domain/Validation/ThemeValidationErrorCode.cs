namespace Slugger.Domain.Validation;

/// <summary>Why a theme was refused, as something a caller can branch on rather than a string to match.</summary>
public enum ThemeValidationErrorCode
{
    /// <summary>The file is not JSON at all. Terminal: nothing after it can be checked.</summary>
    MalformedJson,

    /// <summary>A section the schema requires is absent or has the wrong shape.</summary>
    MalformedSection,

    /// <summary>An entry of "nouns" is not an object with a non-empty "value".</summary>
    MalformedNoun,

    /// <summary>A category referenced by a noun exists in neither "adjectives" nor "participles".</summary>
    UnknownCategory,

    /// <summary>Fewer than 100 distinct nouns in the file.</summary>
    TooFewNouns,

    /// <summary>Some noun resolves to fewer than 100 adjectives.</summary>
    PoolTooSmall,

    /// <summary>Some category totals fewer than 40 000 combinations.</summary>
    CategoryTooPoor,

    /// <summary>The defaults ask for participles the theme never declares anywhere.</summary>
    ParticiplesRequestedButAbsent
}
