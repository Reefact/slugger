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
internal sealed class UnregisterThemeUseCase(IThemeDirectory directories, IConfigStore config)
{
    private IThemeDirectory Directories { get; } = directories;
    private IConfigStore Config { get; } = config;

    /// <summary>Deletes a custom theme file.</summary>
    /// <param name="name">The theme to unregister.</param>
    /// <param name="requested">What the command line asked for, which may point --theme-dir elsewhere.</param>
    internal Outcome Execute(string name, SluggerOptions requested)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(requested);

        SluggerOptions session = OptionResolver.Merge(requested, Config.Load());
        IThemeStore store = Directories.StoreFor(session.ThemeDirectory);

        if (store.Contains(name))
        {
            store.Delete(name);

            return Outcome.Success;
        }

        // Two different refusals on purpose: a name nobody carries is a typo, while a built-in
        // one is a misunderstanding of what unregistering means, and the messages differ.
        return Outcome.Failure(Directories.Embedded.Contains(name)
            ? ThemeErrors.NotAFile(name)
            : ThemeErrors.NotFound(name, Directories.CatalogFor(session.ThemeDirectory).ListNames()));
    }
}
