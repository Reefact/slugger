using FirstClassErrors;
using Slugger.Cli.Rendering;
using Slugger.Domain;
using Slugger.Domain.Generation;

namespace Slugger.Cli;

/// <summary>
/// The composition root. Flag parsing, the REPL and oneshot loops, and the commands that run
/// and exit without generating anything - --init, --register, --unregister, --list-themes -
/// all land here; the wiring of the ports happens here too.
/// </summary>
internal static class Program
{
    private const int SampleCount = 5;

    private static int Main(string[] args)
    {
        Console.WriteLine($"slugger {ThisVersion}");

        // Until argument parsing lands, one argument is understood: a path to check, or the name
        // of a built-in theme to draw from. Both exercise the real pipeline the flags will use.
        switch (args)
        {
            case [string path] when File.Exists(path):
                return Report(Themes.LoadFromFileResult(path));

            case [string name] when Themes.ListEmbedded().Contains(name, StringComparer.Ordinal):
                return Draw(name);

            case []:
                Console.WriteLine($"built-in themes: {string.Join(", ", Themes.ListEmbedded())}");

                return 0;

            default:
                Console.Error.WriteLine("argument parsing is not implemented yet; pass a theme file path or a built-in theme name");

                return 1;
        }
    }

    private static int Draw(string name)
    {
        Theme theme = Themes.LoadEmbedded(name);
        GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(theme);

        for (int drawn = 0; drawn < SampleCount; drawn++)
        {
            Console.WriteLine(SlugGenerator.Generate(theme, options));
        }

        return 0;
    }

    private static int Report(Outcome<Theme> outcome)
    {
        foreach (string line in ThemeReportRenderer.Render(outcome))
        {
            if (outcome.IsSuccess) { Console.WriteLine(line); } else { Console.Error.WriteLine(line); }
        }

        return outcome.IsSuccess ? 0 : 1;
    }

    private static string ThisVersion => typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
