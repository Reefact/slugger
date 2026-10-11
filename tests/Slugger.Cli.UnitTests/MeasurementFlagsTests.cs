#region Usings declarations

using Slugger.Application.Options;
using Slugger.Cli.CommandLine;
using Slugger.Domain;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The options an analysis report says it was measured under, written back as the flags that
///     set them, so that whoever reads the report can run the same measurement again.
/// </summary>
/// <remarks>Literal on purpose: the flags are what a reader copies.</remarks>
public sealed class MeasurementFlagsTests {

    [Fact]
    public void Spells_every_option_that_changes_the_measurement_in_the_order_the_help_lists_them() {
        // Setup
        SluggerOptions options = new() {
            Separator       = '_',
            WordSeparator   = "",
            Casing          = Casing.Snake,
            SegmentMode     = SegmentMode.ThreeOrTwo,
            MaxLength       = 30,
            MaxSegmentWords = SegmentWordsCap.Of(2),
            TokenLength     = 4,
            TokenGlued      = true,
            FoldAccents     = true,
            Ascii           = true,
            AllowSmallTheme = true
        };

        // Exercise
        string flags = MeasurementFlags.Of(options);

        // Verify
        Assert.Equal(
            "--sep '_' --word-sep '' --casing snake --segment threeOrTwo --max-length 30 --max-segment-words 2 "
          + "--token-length 4 --token-glued --fold-accents --ascii --allow-small-theme",
            flags);
    }

    /// <summary>A count, a seed or the clipboard change nothing a report measures.</summary>
    [Fact]
    public void Leaves_out_the_options_that_change_nothing_measured() {
        // Setup
        SluggerOptions options = new() { Count = 5, Seed = 42, Clipboard = true, Oneshot = true, TokenChance = 10, TokenHex = true };

        // Exercise
        string flags = MeasurementFlags.Of(options);

        // Verify
        Assert.Equal(string.Empty, flags);
    }

    [Fact]
    public void Spells_a_switch_turned_off_and_a_cap_removed_explicitly() {
        // Setup
        SluggerOptions options = new() { Ascii = false, MaxSegmentWords = SegmentWordsCap.None };

        // Exercise
        string flags = MeasurementFlags.Of(options);

        // Verify
        Assert.Equal("--max-segment-words none --ascii false", flags);
    }

    [Fact]
    public void Spells_nothing_for_no_options_at_all() {
        Assert.Equal(string.Empty, MeasurementFlags.Of(null));
    }

}
