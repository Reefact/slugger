#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Validation;

#endregion

namespace Slugger.Application.UseCases;

/// <summary>
///     <c>--register</c>: validate the file exactly as a runtime load would - same rules, same
///     error messages - then copy it into the theme directory under its own file name. Refuses a
///     name <c>--theme</c> could never select, and refuses rather than overwrite an existing custom
///     theme; warns, but proceeds, when the name shadows a built-in one.
/// </summary>
internal sealed class RegisterThemeUseCase(IThemeDirectory directories, IConfigStore config) {

    /// <summary>What <c>--theme</c> splits its value on, so a name holding one is two names to it.</summary>
    private const char ThemeListSeparator = ',';

    #region Static members

    /// <summary>
    ///     Why <c>--theme</c> could never select a theme of that name, or null where it can: it splits
    ///     its value on commas, and reads the wildcard as every theme rather than as one of them.
    /// </summary>
    /// <param name="name">The theme name, taken from the file name.</param>
    private static string? WhyItCannotBeSelected(string name) {
        if (name.Contains(ThemeListSeparator, StringComparison.Ordinal)) { return "--theme splits its value on commas"; }
        if (name == GenerateSlugsUseCase.EveryThemeName) { return $"--theme reads \"{GenerateSlugsUseCase.EveryThemeName}\" as every theme"; }

        return null;
    }

    #endregion

    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore    Config      { get; } = config;

    /// <summary>Validates the file and, if it passes, copies it into the theme directory.</summary>
    /// <param name="path">The theme file to register.</param>
    /// <param name="requested">Whether --allow-small-theme was passed, and where --theme-dir points.</param>
    internal RegisterThemeResult Execute(string path, SluggerOptions requested) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions session = OptionResolver.Merge(requested, Config.Load());
        IThemeStore    store   = Directories.StoreFor(session.ThemeDirectory);

        string name = Path.GetFileNameWithoutExtension(path.AsSpan()).ToString();
        if (WhyItCannotBeSelected(name) is { } rule) { return new RegisterThemeResult(Outcome.Failure(ThemeErrors.NotSelectable(name, rule)), name, false); }
        if (store.Contains(name)) { return new RegisterThemeResult(Outcome.Failure(ThemeErrors.AlreadyRegistered(name)), name, false); }

        Outcome<ThemeDocument> loaded = store.LoadFile(path, session.AllowSmallTheme ?? false);
        if (loaded.Error is { } refused) { return new RegisterThemeResult(Outcome.Failure(refused), name, false); }

        // The file is copied as it was validated rather than re-serialised, so the author gets
        // their own formatting and comments-in-spirit back rather than a machine's rendering.
        store.Save(name, store.ReadFileText(path));

        return new RegisterThemeResult(
            Outcome.Success,
            name,
            Directories.Embedded.Contains(name),
            ThemeValidator.Remarks(loaded.GetResultOrThrow()));
    }

}