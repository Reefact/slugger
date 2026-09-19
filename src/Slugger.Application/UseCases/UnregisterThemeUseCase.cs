using Slugger.Application.Abstractions;
using Slugger.Application.Options;

namespace Slugger.Application.UseCases;

/// <summary>
/// <c>--unregister</c>: delete a custom theme file. A built-in theme with no custom file of
/// the same name cannot be unregistered - there is no file to remove, only a name to leave
/// out of <c>--theme</c>.
/// </summary>
public sealed class UnregisterThemeUseCase(IThemeStore store)
{
    private IThemeStore Store { get; } = store;

    public void Execute(string name, SluggerOptions options) => throw new NotImplementedException();
}
