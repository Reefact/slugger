using Slugger.Cli.Rendering;
using Slugger.Domain.Validation;

namespace Slugger.Cli;

/// <summary>
/// The composition root. Flag parsing, the REPL and oneshot loops, and the commands that run
/// and exit without generating anything - --init, --register, --unregister, --list-themes -
/// all land here; the wiring of the ports happens here too.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine($"slugger {ThisVersion}");

        // Until argument parsing lands, a single existing path is checked and reported on.
        // It exercises the real loading pipeline that --register and --theme will both use.
        if (args is [string path] && File.Exists(path))
        {
            return Report(Themes.LoadFromFileResult(path));
        }

        Console.WriteLine($"built-in themes: {string.Join(", ", Themes.ListEmbedded())}");

        if (args.Length > 0)
        {
            Console.Error.WriteLine("argument parsing is not implemented yet; pass a theme file path to have it checked");

            return 1;
        }

        return 0;
    }

    private static int Report(ThemeLoadResult result)
    {
        foreach (string line in ThemeReportRenderer.Render(result))
        {
            if (result.IsLoaded) { Console.WriteLine(line); } else { Console.Error.WriteLine(line); }
        }

        return result.IsLoaded ? 0 : 1;
    }

    private static string ThisVersion => typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
