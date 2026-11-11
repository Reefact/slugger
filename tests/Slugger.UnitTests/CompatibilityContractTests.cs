#region Usings declarations

using System.Reflection;

using Slugger.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     DEC0037: what every <c>1.x</c> version keeps. The floors do not move, and a code a consumer
///     branches on is never removed or renamed - a new one may be added beside them.
/// </summary>
/// <remarks>
///     Literal on purpose, all of it: the values are the contract. A red test here is a decision to
///     break it, and belongs in a decision record and a major version before it belongs in this file.
/// </remarks>
public sealed class CompatibilityContractTests {

    #region Static members

    private static Dictionary<string, string> PublishedCodes() {
        return typeof(SluggerThemeError.Codes).GetFields(BindingFlags.Public | BindingFlags.Static)
                                               .ToDictionary(field => field.Name, field => ((ErrorCode)field.GetValue(null)!).Dehydrate(), StringComparer.Ordinal);
    }

    #endregion

    [Fact]
    public void Keeps_the_floors_where_they_are() {
        // Verify
        Assert.Equal(100, ThemeFloors.MinimumNouns);
        Assert.Equal(100, ThemeFloors.MinimumPoolPerNoun);
        Assert.Equal(20, ThemeFloors.MinimumParticiplePoolPerNoun);
        Assert.Equal(40_000, ThemeFloors.MinimumCombinationsPerCategory);
    }

    [Fact]
    public void Keeps_every_published_code_under_its_name_and_its_value() {
        // Setup
        Dictionary<string, string> promised = new(StringComparer.Ordinal) {
            ["Rejected"]                          = "THEME_REJECTED",
            ["NotFound"]                          = "THEME_NOT_FOUND",
            ["NoNounToDrawFrom"]                  = "THEME_NO_NOUN",
            ["MalformedJson"]                     = "THEME_MALFORMED_JSON",
            ["MalformedSection"]                  = "THEME_MALFORMED_SECTION",
            ["MalformedNoun"]                     = "THEME_MALFORMED_NOUN",
            ["UnknownCategory"]                   = "THEME_UNKNOWN_CATEGORY",
            ["ExclusionMatchesNothing"]           = "THEME_EXCLUSION_MATCHES_NOTHING",
            ["TooFewNouns"]                       = "THEME_TOO_FEW_NOUNS",
            ["PoolTooSmall"]                      = "THEME_POOL_TOO_SMALL",
            ["ParticiplePoolTooSmall"]            = "THEME_PARTICIPLE_POOL_TOO_SMALL",
            ["CombinedPoolTooSmall"]              = "THEME_COMBINED_POOL_TOO_SMALL",
            ["IncompatibleAdjectiveNotDeclared"]  = "THEME_INCOMPATIBLE_ADJECTIVE_ABSENT",
            ["IncompatibleParticipleNotDeclared"] = "THEME_INCOMPATIBLE_PARTICIPLE_ABSENT",
            ["IncompatibilityStarvesTheNoun"]     = "THEME_INCOMPATIBILITY_STARVES_NOUN",
            ["LongerThanPromised"]                = "THEME_LONGER_THAN_PROMISED",
            ["NothingFitsTheLimit"]               = "THEME_NOTHING_FITS_THE_LIMIT",
            ["NoValueIsShortEnough"]              = "THEME_NO_VALUE_SHORT_ENOUGH",
            ["TheLimitStarvesTheNoun"]            = "THEME_LIMIT_STARVES_NOUN",
            ["CategoryTooPoor"]                   = "THEME_CATEGORY_TOO_POOR",
            ["ParticiplesRequestedButAbsent"]     = "THEME_PARTICIPLES_ABSENT"
        };

        // Exercise
        Dictionary<string, string> published = PublishedCodes();

        // Verify - every promised code is still there, with the same value; new ones may sit beside them.
        Assert.All(promised, code => Assert.Equal(code.Value, published.GetValueOrDefault(code.Key)));
    }

}
