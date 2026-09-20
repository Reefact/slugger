using FirstClassErrors;
using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Cli.CommandLine;
using Slugger.Cli.Rendering;

namespace Slugger.Cli;

/// <summary>
/// Reads the command line, dispatches it, and writes what came back. Everything it does is a
/// decision about the terminal - which is why it lives here and not in a use case.
/// </summary>
internal sealed class SluggerRunner(
    IConsole console,
    IConfigStore config,
    GenerateSlugsUseCase generate,
    ListThemesUseCase listThemes,
    RegisterThemeUseCase register,
    UnregisterThemeUseCase unregister,
    SaveDefaultsUseCase saveDefaults)
{
    /// <summary>The process exit code: zero when it did what was asked, one when it refused.</summary>
    internal const int Refused = 1;

    /// <param name="arguments">The command line as the runtime handed it over.</param>
    internal int Run(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        Outcome<CommandLineRequest> parsed = CommandLineParser.Parse(arguments);
        if (parsed.Error is { } refused)
        {
            return Report(refused);
        }

        CommandLineRequest request = parsed.GetResultOrThrow();
        SluggerOptions session = OptionResolver.Merge(request.Options, config.Load());

        return request.Command switch
        {
            CliCommand.ListThemes => List(session),
            CliCommand.SaveDefaults => Save(request.Options),
            CliCommand.Register => Register(request.Argument!, session),
            CliCommand.Unregister => Unregister(request.Argument!, session),
            _ => Generate(request.Options, session),
        };
    }

    /// <summary>
    /// A round of slugs, then another on every Enter. Standard input that is not a terminal -
    /// a pipe, a script, a CI runner - turns the loop off by itself, because a ReadLine nobody
    /// will answer is a hang rather than a prompt.
    /// </summary>
    /// <param name="commandLine">
    /// What this invocation asked for explicitly, and nothing else. The use case lays the saved
    /// config under it itself - handing it the merged view instead would give a saved option the
    /// standing of an explicit argument, and it would then beat the drawn theme's own defaults,
    /// which ADR 0004 puts above it.
    /// </param>
    /// <param name="session">The merged view, for the decisions the terminal makes rather than the engine.</param>
    private int Generate(SluggerOptions commandLine, SluggerOptions session)
    {
        bool once = session.Oneshot == true || console.IsInputRedirected;

        do
        {
            Outcome<IReadOnlyList<string>> outcome = generate.Execute(commandLine);
            if (outcome.Error is { } refused)
            {
                return Report(refused);
            }

            foreach (string slug in outcome.GetResultOrThrow())
            {
                console.WriteLine(slug);
            }
        }
        while (!once && console.ReadLine() is not null);

        return 0;
    }

    private int List(SluggerOptions session)
    {
        foreach (string name in listThemes.Execute(session))
        {
            console.WriteLine(name);
        }

        return 0;
    }

    private int Save(SluggerOptions commandLine)
    {
        saveDefaults.Execute(commandLine);
        console.WriteLine("defaults saved.");

        return 0;
    }

    private int Register(string path, SluggerOptions session)
    {
        RegisterThemeResult result = register.Execute(path, session);
        if (result.Outcome.Error is { } refused)
        {
            return Report(refused);
        }

        console.WriteLine($"theme \"{result.Name}\" registered.");

        // Allowed - a custom file is meant to be able to shadow a built-in theme - but never
        // silent, so nobody wonders later why docker stopped looking like docker.
        if (result.Shadows)
        {
            console.WriteError($"warning: \"{result.Name}\" now shadows the built-in theme of the same name.");
        }

        return 0;
    }

    private int Unregister(string name, SluggerOptions session)
    {
        Outcome outcome = unregister.Execute(name, session);
        if (outcome.Error is { } refused)
        {
            return Report(refused);
        }

        console.WriteLine($"theme \"{name}\" unregistered.");

        return 0;
    }

    private int Report(Error rejection)
    {
        foreach (string line in ReportRenderer.Render(rejection))
        {
            console.WriteError(line);
        }

        return Refused;
    }
}
