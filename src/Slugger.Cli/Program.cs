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
        // Scaffolding: proves the chain Cli -> Core -> Infrastructure -> embedded resources
        // is wired, until argument parsing lands.
        Console.WriteLine($"slugger {ThisVersion}");
        Console.WriteLine($"built-in themes: {string.Join(", ", Themes.ListEmbedded())}");

        if (args.Length > 0)
        {
            Console.Error.WriteLine("argument parsing is not implemented yet");

            return 1;
        }

        return 0;
    }

    private static string ThisVersion => typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
