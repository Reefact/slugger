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

    #region Static members

    /// <summary>
    ///     What a refused file still has to say. A misspelt key is often why it was refused -
    ///     "participels" leaves a theme asking for participles it no longer declares - so its remark is
    ///     worth more beside the refusal than after the author has fixed everything else (DEC0027). Only
    ///     a file whose shape is sound holds a theme to read remarks from.
    /// </summary>
    private static IReadOnlyList<string> RemarksOnARefusedFile(IThemeStore store, string path) {
        if (!store.FileExists(path)) { return []; }

        Outcome<ThemeDocument> wellFormed = store.ReadWellFormed(path);
        if (wellFormed.IsFailure) { return []; }

        return ThemeValidator.Remarks(wellFormed.GetResultOrThrow());
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
        if (ThemeSelection.WhyItCannotBeSelected(name) is { } rule) { return new RegisterThemeResult(Outcome.Failure(ThemeErrors.NotSelectable(name, rule)), name, false); }
        if (store.Contains(name)) { return new RegisterThemeResult(Outcome.Failure(ThemeErrors.AlreadyRegistered(name)), name, false); }

        Outcome<ThemeDocument> loaded = store.LoadFile(path, session.AllowSmallTheme ?? false);
        if (loaded.Error is { } refused) { return new RegisterThemeResult(Outcome.Failure(refused), name, false, RemarksOnARefusedFile(store, path)); }

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