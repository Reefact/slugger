namespace Slugger.Domain.Validation;

public enum ThemeValidationErrorCode
{
    /// <summary>A category referenced by a noun does not exist as a key in "adjectives".</summary>
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
