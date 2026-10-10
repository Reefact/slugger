namespace Slugger.Application.Options;

/// <summary>
///     How <c>--theme</c> reads its value, and so which theme names it can ever select. A theme is
///     named after its file, and a file name can hold what <c>--theme</c> reads as something else.
/// </summary>
internal static class ThemeSelection {

    /// <summary>What <c>--theme</c> splits its value on, so a name holding one is two names to it.</summary>
    internal const char ListSeparator = ',';

    /// <summary>The value <c>--theme</c> reads as every theme in scope rather than as one of them.</summary>
    internal const string EveryTheme = "*";

    #region Static members

    /// <summary>
    ///     Why <c>--theme</c> could never select a theme of that name, or null where it can: it splits
    ///     its value on commas, and reads the wildcard as every theme rather than as one of them.
    /// </summary>
    /// <param name="name">The theme name, taken from the file name.</param>
    internal static string? WhyItCannotBeSelected(string name) {
        ArgumentNullException.ThrowIfNull(name);

        if (name.Contains(ListSeparator, StringComparison.Ordinal)) { return "--theme splits its value on commas"; }
        if (name == EveryTheme) { return $"--theme reads \"{EveryTheme}\" as every theme"; }

        return null;
    }

    /// <summary>Whether <c>--theme</c> can select a theme of that name.</summary>
    /// <param name="name">The theme name, taken from the file name.</param>
    internal static bool CanBeSelected(string name) {
        return WhyItCannotBeSelected(name) is null;
    }

    #endregion

}
