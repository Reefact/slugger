#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Infrastructure.Serialization;

/// <summary>
///     Parse, then validate, then report both together. The one path every theme takes, whichever
///     catalog it came from - which is what makes DEC0006 hold, that a theme refused by
///     <c>--register</c> and the same theme refused at runtime say the same thing.
/// </summary>
internal static class ThemeLoader {

    #region Static members

    /// <param name="name">The theme name, which comes from the file name rather than the JSON.</param>
    /// <param name="json">The raw document.</param>
    /// <param name="allowSmall">Waive the minimum size rules for this load.</param>
    /// <param name="pool">The run's shared intern pool, when there is one.</param>
    internal static Outcome<Theme> Load(string name, string json, bool allowSmall = false, StringInternPool? pool = null) {
        ThemeParseResult parsed = new JsonThemeSerializer(pool).Deserialize(name, json);
        if (parsed.Theme is not { } theme) { return Refuse(name, parsed.ShapeErrors); }

        // The shape complaints and the rule failures are reported together: fixing four
        // malformed sections only to be told about twenty rule failures on the next run is
        // the same "stops at the first error" the pipeline exists to avoid, one stage up.
        List<DomainError> reasons = [.. parsed.ShapeErrors];
        if (parsed.RulesCanRun) {
            reasons.AddRange(ThemeValidator.Validate(theme, allowSmall));
        }

        return reasons.Count == 0 ? Outcome<Theme>.Success(theme) : Refuse(name, reasons);
    }

    /// <summary>
    ///     The shape alone, deliberately stopping short of <see cref="ThemeValidator" /> - for a
    ///     reader who wants what the file declares about itself, not a verdict on whether it would
    ///     be accepted at runtime.
    /// </summary>
    /// <param name="name">The theme name, which comes from the file name rather than the JSON.</param>
    /// <param name="json">The raw document.</param>
    /// <param name="pool">The run's shared intern pool, when there is one.</param>
    internal static Outcome<Theme> Parse(string name, string json, StringInternPool? pool = null) {
        ThemeParseResult parsed = new JsonThemeSerializer(pool).Deserialize(name, json);

        return parsed.Theme is { } theme ? Outcome<Theme>.Success(theme) : Refuse(name, parsed.ShapeErrors);
    }

    /// <summary>Wraps every reason into the one error an <see cref="Outcome{T}" /> can carry.</summary>
    /// <param name="name">What the theme would have been called.</param>
    /// <param name="reasons">Every reason, not just the first.</param>
    internal static Outcome<Theme> Refuse(string name, IReadOnlyList<DomainError> reasons) {
        return Outcome<Theme>.Failure(ThemeErrors.Rejected(name, reasons));
    }

    #endregion

}