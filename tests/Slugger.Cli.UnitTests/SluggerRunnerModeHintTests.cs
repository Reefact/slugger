#region Usings declarations

using FirstClassErrors;

using Slugger.Domain;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The line a refusal in a mixed draw gains: a theme written for one segment mode, refused under
///     the one a mixed draw falls back to, and the two ways that already exist to keep its own.
/// </summary>
/// <remarks>Literal on purpose: the sentence is what a reader gets.</remarks>
public sealed class SluggerRunnerModeHintTests {

    [Fact]
    public void Names_both_ways_to_keep_a_themes_own_mode_when_a_mixed_draw_refused_it_under_another() {
        // Setup
        DomainError rejection = ThemeErrors.RejectedAmongOthers(
            "spices", SegmentMode.Either, SegmentMode.Both, [ThemeErrors.ParticiplePoolTooSmall("saffron", 5, 20, SegmentMode.Both)]);

        // Exercise
        string? hint = SluggerRunner.KeepingItsOwnMode(rejection);

        // Verify
        Assert.Equal(
            "Drawn with other themes, \"spices\" is drawn \"both\" rather than its own \"either\": "
          + "add --mimic-style true to keep each theme's own style, or --segment either to draw them all that way.",
            hint);
    }

    [Fact]
    public void Says_nothing_of_modes_for_a_theme_refused_on_its_own() {
        // Exercise
        string? hint = SluggerRunner.KeepingItsOwnMode(ThemeErrors.Rejected("spices", [ThemeErrors.TooFewNouns(3, 100)]));

        // Verify
        Assert.Null(hint);
    }

}
