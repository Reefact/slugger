#region Usings declarations

using System.Globalization;

using Slugger.Application.Options;
using Slugger.Domain;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     The options that change what <c>--analyze</c> measures, written back as the flags that set
///     them: the segment mode, the two limits, everything that decides a slug's length, and whether
///     the size floors are waived. The count, the seed and the clipboard change nothing in a report.
/// </summary>
internal static class MeasurementFlags {

    #region Static members

    /// <summary>The flags, in the order <c>--help</c> lists them, or an empty string when there are none.</summary>
    /// <param name="options">One layer of options - the command line, or the saved defaults - or null for none.</param>
    internal static string Of(SluggerOptions? options) {
        if (options is null) { return string.Empty; }

        List<string> flags = [];
        if (options.Separator is { } separator) { flags.Add($"--sep '{separator}'"); }
        if (options.WordSeparator is { } wordSeparator) { flags.Add($"--word-sep '{wordSeparator}'"); }
        if (options.Casing is { } casing) { flags.Add($"--casing {Spelling.Of(casing)}"); }
        if (options.SegmentMode is { } mode) { flags.Add($"--segment {Spelling.Of(mode)}"); }
        if (options.MaxLength is { } length) { flags.Add(string.Create(CultureInfo.InvariantCulture, $"--max-length {length}")); }
        if (options.MaxSegmentWords is { } cap) { flags.Add(WordCap(cap)); }
        if (options.TokenLength is { } tokenLength) { flags.Add(string.Create(CultureInfo.InvariantCulture, $"--token-length {tokenLength}")); }
        if (options.TokenGlued is { } glued) { flags.Add(Switch("--token-glued", glued)); }
        if (options.FoldAccents is { } fold) { flags.Add(Switch("--fold-accents", fold)); }
        if (options.Ascii is { } ascii) { flags.Add(Switch("--ascii", ascii)); }
        if (options.AllowSmallTheme is { } allowSmall) { flags.Add(Switch("--allow-small-theme", allowSmall)); }

        return string.Join(' ', flags);
    }

    private static string WordCap(SegmentWordsCap cap) {
        if (cap.Words is not { } words) { return "--max-segment-words none"; }

        return string.Create(CultureInfo.InvariantCulture, $"--max-segment-words {words}");
    }

    /// <summary>A switch on is the flag alone; one turned off says so, as a saved switch is turned off.</summary>
    private static string Switch(string flag, bool on) {
        return on ? flag : $"{flag} false";
    }

    #endregion

}
