using FirstClassErrors;
using Slugger.Domain;

namespace Slugger.Infrastructure.Serialization;

/// <summary>
/// What reading the document produced: whatever theme could be built from it, everything that
/// was wrong with its shape, and whether the rules are worth running on the result.
/// </summary>
/// <remarks>
/// The theme comes back even when the shape had complaints, so that a file with four malformed
/// sections and twenty rule failures reports all twenty-four in one run rather than four now
/// and twenty after they are fixed. <see cref="RulesCanRun"/> is what stops that turning into
/// noise: when "nouns" itself did not parse as an array, "0 nouns, at least 100 required" says
/// nothing the shape error did not already say.
/// </remarks>
/// <param name="Theme">The theme built from what parsed, or null when nothing could be built.</param>
/// <param name="ShapeErrors">Everything wrong with the document's structure.</param>
/// <param name="RulesCanRun">Whether the sections the rules read parsed well enough to judge.</param>
internal sealed record ThemeParseResult(
    Theme? Theme,
    IReadOnlyList<DomainError> ShapeErrors,
    bool RulesCanRun);
