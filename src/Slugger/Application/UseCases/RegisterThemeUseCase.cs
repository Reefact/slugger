using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Validation;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--register</c>: validate the file exactly as a runtime load would - same rules, same
/// error messages - then copy it into the theme directory under its own file name. Refuses
/// rather than overwrite an existing custom theme; warns, but proceeds, when the name
/// shadows a built-in one.
/// </summary>
internal sealed class RegisterThemeUseCase(IThemeCatalog embedded, IThemeStore store)
{
    private IThemeCatalog Embedded { get; } = embedded;
    private IThemeStore Store { get; } = store;

    /// <summary>Validates the file and, if it passes, copies it into the theme directory.</summary>
    /// <param name="path">The theme file to register.</param>
    /// <param name="options">Whether --allow-small-theme was passed.</param>
    internal RegisterThemeResult Execute(string path, SluggerOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);

        string name = Path.GetFileNameWithoutExtension(path.AsSpan()).ToString();
        if (Store.Contains(name))
        {
            return new RegisterThemeResult(Outcome.Failure(ThemeErrors.AlreadyRegistered(name)), name, Shadows: false);
        }

        Outcome<Theme> loaded = Store.LoadFile(path, options.AllowSmallTheme ?? false);
        if (loaded.Error is { } refused)
        {
            return new RegisterThemeResult(Outcome.Failure(refused), name, Shadows: false);
        }

        // The file is copied as it was validated rather than re-serialised, so the author gets
        // their own formatting and comments-in-spirit back rather than a machine's rendering.
        Store.Save(name, Store.ReadFileText(path));

        return new RegisterThemeResult(Outcome.Success, name, Shadows: Embedded.Contains(name));
    }
}

/// <summary>
/// What registering produced: the outcome, the name it went under, and whether that name now
/// shadows a built-in theme - allowed, but never silently, so the caller can say so.
/// </summary>
/// <param name="Outcome">Success, or every reason the theme was refused.</param>
/// <param name="Name">The theme name, taken from the file name.</param>
/// <param name="Shadows">Whether a built-in theme of the same name is now overridden.</param>
internal sealed record RegisterThemeResult(Outcome Outcome, string Name, bool Shadows);
