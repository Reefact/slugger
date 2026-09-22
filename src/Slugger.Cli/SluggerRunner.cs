#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Cli.CommandLine;
using Slugger.Cli.Rendering;
using Slugger.Domain;
using Slugger.Domain.Analysis;

using Spectre.Console;

#endregion

namespace Slugger.Cli;

/// <summary>
///     Reads the command line, dispatches it, and writes what came back. Everything it does is a
///     decision about the terminal - which is why it lives here and not in a use case.
/// </summary>
internal sealed class SluggerRunner(
    IConsole               console,
    IConfigStore           config,
    GenerateSlugsUseCase   generate,
    ListThemesUseCase      listThemes,
    RegisterThemeUseCase   register,
    UnregisterThemeUseCase unregister,
    SaveDefaultsUseCase    saveDefaults,
    AnalyzeThemeUseCase    analyze,
    ThemeInfoUseCase       themeInfo,
    IThemeDirectory        directories) {

    /// <summary>The process exit code: zero when it did what was asked, one when it refused.</summary>
    internal const int Refused = 1;

    /// <param name="request">The command line, already understood.</param>
    internal int Run(CommandLineRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        SluggerOptions session = OptionResolver.Merge(request.Options, config.Load());

        return request.Command switch {
            CliCommand.ListThemes   => List(session),
            CliCommand.SaveDefaults => Save(request.Options),
            CliCommand.Register     => Register(request.Argument!, session),
            CliCommand.Analyze      => Analyze(request.Argument!, request.Options),
            CliCommand.Unregister   => Unregister(request.Argument!, session),
            CliCommand.ThemeInfo    => ThemeInfo(request.Argument!, request.Options),
            _                       => Generate(request.Options, session)
        };
    }

    /// <summary>
    ///     A round of slugs, then another on every Enter. Standard input that is not a terminal -
    ///     a pipe, a script, a CI runner - turns the loop off by itself, because a ReadLine nobody
    ///     will answer is a hang rather than a prompt.
    /// </summary>
    /// <param name="commandLine">
    ///     What this invocation asked for explicitly, and nothing else. The use case lays the saved
    ///     config under it itself - handing it the merged view instead would give a saved option the
    ///     standing of an explicit argument, and it would then beat the drawn theme's own defaults,
    ///     which DEC0004 puts above it.
    /// </param>
    /// <param name="session">The merged view, for the decisions the terminal makes rather than the engine.</param>
    private int Generate(SluggerOptions commandLine, SluggerOptions session) {
        bool once = session.Oneshot == true || console.IsInputRedirected;

        do {
            Outcome<IReadOnlyList<string>> outcome = generate.Execute(commandLine);
            if (outcome.Error is { } refused) { return Report(refused); }

            foreach (string slug in outcome.GetResultOrThrow()) {
                console.WriteLine(slug);
            }
        } while (!once && console.ReadLine() is not null);

        return 0;
    }

    /// <summary>
    ///     One name per line and nothing else. A table would read better and pipe worse, and this
    ///     list exists to be piped: "slugger --list-themes | xargs -n1 slugger --theme" is what a
    ///     name on its own line is for.
    /// </summary>
    /// <param name="session">The merged view, for --theme-dir.</param>
    private int List(SluggerOptions session) {
        foreach (string name in listThemes.Execute(session)) {
            console.WriteLine(name);
        }

        return 0;
    }

    private int Save(SluggerOptions commandLine) {
        saveDefaults.Execute(commandLine);
        console.WriteLine("defaults saved.");

        return 0;
    }

    /// <summary>
    ///     Measures the file and writes the report beside it. Exit code 0 even for a refused theme:
    ///     the analysis succeeded, and what it found is in the report.
    /// </summary>
    /// <param name="path">The theme file to measure.</param>
    /// <param name="commandLine">What this invocation asked for, for --theme-dir.</param>
    private int Analyze(string path, SluggerOptions commandLine) {
        ThemeAnalysis analysis = analyze.Execute(path, commandLine);
        string        report   = ThemeAnalysisRenderer.Render(analysis);

        // Beside the theme rather than in the theme directory: the file measured may not be
        // registered at all, and a report belongs next to what it is about.
        string destination = Path.Combine(
            Path.GetDirectoryName(path) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(path.AsSpan())}-analysis.md");

        directories.StoreFor(commandLine.ThemeDirectory).WriteFileText(destination, report);

        // The verdict on the terminal, the measurements in the file: knowing a theme is refused
        // is what the next command depends on, and it should not cost opening a document.
        console.Write(ThemeAnalysisRenderer.Summary(analysis));
        console.WriteLine($"analysis of \"{analysis.Name}\" written to {destination}");

        return 0;
    }

    private int Register(string path, SluggerOptions session) {
        RegisterThemeResult result = register.Execute(path, session);
        if (result.Outcome.Error is { } refused) { return Report(refused); }

        console.WriteLine($"theme \"{result.Name}\" registered.");

        // Allowed - a custom file is meant to be able to shadow a built-in theme - but never
        // silent, so nobody wonders later why docker stopped looking like docker.
        if (result.Shadows) {
            Warn($"warning: \"{result.Name}\" now shadows the built-in theme of the same name.");
        }

        foreach (string remark in result.Remarks ?? []) {
            Warn($"warning: {remark}");
        }

        return 0;
    }

    private int Unregister(string name, SluggerOptions session) {
        Outcome outcome = unregister.Execute(name, session);
        if (outcome.Error is { } refused) { return Report(refused); }

        console.WriteLine($"theme \"{name}\" unregistered.");

        return 0;
    }

    /// <summary>
    ///     The theme's own "meta" block, one field per line. Skips a field the file left unset
    ///     rather than printing it empty, and says so plainly when none is declared at all.
    /// </summary>
    /// <param name="name">The theme to describe.</param>
    /// <param name="commandLine">What this invocation asked for, for --theme-dir.</param>
    private int ThemeInfo(string name, SluggerOptions commandLine) {
        Outcome<Theme> outcome = themeInfo.Execute(name, commandLine);
        if (outcome.Error is { } refused) { return Report(refused); }

        Theme         theme    = outcome.GetResultOrThrow();
        ThemeMetadata metadata = theme.Metadata;
        (string Label, string? Value)[] fields = [
            ("title", metadata.Title),
            ("description", metadata.Description),
            ("version", metadata.Version),
            ("author", metadata.Author),
            ("createdAt", metadata.CreatedAt),
            ("publishedAt", metadata.PublishedAt),
            ("source", metadata.Source)
        ];

        console.WriteLine($"theme \"{theme.Name}\"");
        foreach ((string label, string? value) in fields) {
            if (value is not null) {
                console.WriteLine($"  {label}: {value}");
            }
        }

        if (Array.TrueForAll(fields, field => field.Value is null)) {
            console.WriteLine("  (no metadata declared)");
        }

        return 0;
    }

    /// <summary>
    ///     Something that went through and should not pass unread. Its own colour, because a warning
    ///     beside a refusal in the same stream would otherwise read as one.
    /// </summary>
    /// <param name="warning">The whole line, as it will be read.</param>
    private void Warn(string warning) {
        console.WriteError(ReportRenderer.Drawn([warning], Color.Yellow));
    }

    private int Report(Error rejection) {
        console.WriteError(ReportRenderer.Draw(rejection));

        return Refused;
    }

}