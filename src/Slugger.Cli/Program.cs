using Slugger.Application.Abstractions;
using Slugger.Application.UseCases;
using Slugger.Cli.Adapters;
using Slugger.Cli.CommandLine;
using Slugger.Infrastructure.Serialization;
using Slugger.Infrastructure.Configuration;
using Slugger.Infrastructure.ThemeCatalogs;
using Spectre.Console;

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

        // Two, because the streams are redirected independently: a slug piped onwards must not
        // carry a colour code, and a refusal drawn on the pipe would be read as one.
        IAnsiConsole output = SluggerApp.Terminal(Console.Out, Console.IsOutputRedirected);
        IAnsiConsole error = SluggerApp.Terminal(Console.Error, Console.IsErrorRedirected);
        IConsole console = new SystemConsole(output, error);

        SluggerRunner runner = new(
            console,
            config,
            new GenerateSlugsUseCase(directories, config, clipboard),
            new ListThemesUseCase(directories, config),
            new RegisterThemeUseCase(directories, config),
            new UnregisterThemeUseCase(directories, config),
            new SaveDefaultsUseCase(config),
            new AnalyzeThemeUseCase(directories, config),
            new ThemeInfoUseCase(directories, config),
            directories);

        return SluggerApp.Run(runner, console, output, args);
    }
}
