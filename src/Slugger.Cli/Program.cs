using Slugger.Application.Abstractions;
using Slugger.Application.UseCases;
using Slugger.Cli.Adapters;
using Slugger.Infrastructure.Serialization;
using Slugger.Infrastructure.Configuration;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger.Cli;

/// <summary>The composition root: builds the ports, hands them to the runner, returns its exit code.</summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        // One intern pool for the whole run, created here and passed to every load. A pool per
        // file would only deduplicate inside that file, and the words that repeat are the ones
        // across themes.
        StringInternPool pool = new();
        IThemeDirectory directories = new ThemeDirectory(pool);
        IConfigStore config = new XdgConfigStore();
        IClipboard clipboard = new TextCopyClipboard();
        IConsole console = new SystemConsole();

        SluggerRunner runner = new(
            console,
            config,
            new GenerateSlugsUseCase(directories, config, clipboard),
            new ListThemesUseCase(directories, config),
            new RegisterThemeUseCase(directories, config),
            new UnregisterThemeUseCase(directories, config),
            new SaveDefaultsUseCase(config));

        return runner.Run(args);
    }
}
