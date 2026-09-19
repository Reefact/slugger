using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Domain.Validation;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--unregister</c>: delete a custom theme file. A built-in theme with no custom file of
/// the same name cannot be unregistered - there is no file to remove, only a name to leave
/// out of <c>--theme</c>.
/// </summary>
internal sealed class UnregisterThemeUseCase(IThemeCatalog embedded, IThemeStore store)
{
    private IThemeCatalog Embedded { get; } = embedded;
    private IThemeStore Store { get; } = store;

    /// <summary>Deletes a custom theme file.</summary>
    /// <param name="name">The theme to unregister.</param>
    /// <param name="options">Unused for now; the theme directory is already baked into the store.</param>
    internal Outcome Execute(string name, SluggerOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);

        if (Store.Contains(name))
        {
            Store.Delete(name);

            return Outcome.Success;
        }

        // Two different refusals on purpose: a name nobody carries is a typo, while a built-in
        // one is a misunderstanding of what unregistering means, and the messages differ.
        return Outcome.Failure(Embedded.Contains(name)
            ? ThemeErrors.NotAFile(name)
            : ThemeErrors.NotFound(name, Embedded.ListNames()));
    }
}
