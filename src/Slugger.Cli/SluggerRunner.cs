#region Usings declarations

using FirstClassErrors;

using Slugger.Application.Abstractions;
using Slugger.Application.Options;
using Slugger.Application.UseCases;
using Slugger.Cli.CommandLine;
using Slugger.Cli.Rendering;
using Slugger.Domain;
using Slugger.Domain.Analysis;
using Slugger.Domain.Validation;

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

    /// <summary>What a run refused for a theme named by a path adds to the refusal.</summary>
    private const string ThemeTakesAName = "--theme takes a theme name; to draw from a folder, use --theme-dir <folder> --theme <name>";

    #region Static members

    /// <summary>
    ///     Whether the run was refused for one theme that could not be found, under a name that is
    ///     what a path to a theme file looks like - someone handing --theme the file rather than the
    ///     name it is registered under.
    /// </summary>
    /// <param name="rejection">The refusal of the run.</param>
    private static bool AsksForAThemeByItsPath(Error rejection) {
        if (rejection.InnerErrors is not [{ } reason]) { return false; }
        if (reason.Code != ThemeErrors.Codes.NotFound) { return false; }
        if (!reason.Context.TryGet(ThemeErrors.ThemeName, out string? name)) { return false; }
        if (name is null) { return false; }

        return LooksLikeAPath(name);
    }

    /// <summary>A separator of either platform, or the extension of a theme file.</summary>
    /// <param name="name">The theme name that was asked for.</param>
    private static bool LooksLikeAPath(string name) {
        if (name.Contains('/', StringComparison.Ordinal)) { return true; }
        if (name.Contains('\\', StringComparison.Ordinal)) { return true; }

        return name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    /// <param name="request">The command line, already understood.</param>
    internal int Run(CommandLineRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        // Said before anything else runs, and once: a saved default that was silently dropped is
        // what leaves a user wondering why the run did not look the way they set it up.
        SavedConfig saved = config.Read();
        foreach (string remark in saved.Remarks) {
            Warn($"warning: {remark}");
        }

        SluggerOptions session = OptionResolver.Merge(request.Options, saved.Options);
        WarnAboutAMissingThemeDirectory(request.Command, session.ThemeDirectory);

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
    ///     will answer is a hang rather than a prompt. So does standard output that is not one -
    ///     <c>$(slugger)</c>, <c>slugger | head -1</c> - because whoever reads it cannot see that
    ///     slugger is waiting for an Enter.
    /// </summary>
    /// <remarks>
    ///     One random source for the whole session, made before the first round: a seed then fixes the
    ///     session rather than each round, so the second round carries on where the first stopped, and
    ///     <c>--seed 5</c> on three Enters prints what <c>--seed 5 --count 3</c> does. A source per
    ///     round would replay the first round on every Enter.
    /// </remarks>
    /// <param name="commandLine">
    ///     What this invocation asked for explicitly, and nothing else. The use case lays the saved
    ///     config under it itself - handing it the merged view instead would give a saved option the
    ///     standing of an explicit argument, and it would then beat the drawn theme's own defaults,
    ///     which DEC0004 puts above it.
    /// </param>
    /// <param name="session">The merged view, for the decisions the terminal makes rather than the engine.</param>
    private int Generate(SluggerOptions commandLine, SluggerOptions session) {
        bool          once   = session.Oneshot == true || console.IsInputRedirected || console.IsOutputRedirected;
        IRandomSource random = new DefaultRandomSource(session.Seed);

        do {
            Outcome<GeneratedSlugs> outcome = generate.Execute(commandLine, random);
            if (outcome.Error is { } refused) { return ReportDrawing(refused); }

            GeneratedSlugs generated = outcome.GetResultOrThrow();
            foreach (string slug in generated.Slugs) {
                console.WriteLine(slug);
            }

            // After the slugs and beside them, never instead of them: a machine with no clipboard
            // tool is still one that asked for a slug, and the copy is all it goes without.
            if (generated.ClipboardFailure is { } reason) {
                Warn($"warning: could not copy to the clipboard: {reason}");
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
        console.WriteLine("Defaults saved.");

        return 0;
    }

    /// <summary>
    ///     Measures the file and writes the report beside it. Exit code 0 even for a refused theme:
    ///     the analysis succeeded, and what it found is in the report. A path with no file behind it
    ///     is the exception - nothing was analysed, so no report is written, the reason goes to
    ///     standard error and the exit code is the refusal's.
    /// </summary>
    /// <param name="path">The theme file to measure.</param>
    /// <param name="commandLine">What this invocation asked for, for --theme-dir.</param>
    private int Analyze(string path, SluggerOptions commandLine) {
        Outcome<ThemeAnalysis> outcome = analyze.Execute(path, commandLine);
        if (outcome.Error is { } refused) { return Report(refused); }

        ThemeAnalysis analysis = outcome.GetResultOrThrow();
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
        console.WriteLine($"Analysis of \"{analysis.Name}\" written to {destination}");

        return 0;
    }

    private int Register(string path, SluggerOptions session) {
        RegisterThemeResult result = register.Execute(path, session);
        if (result.Outcome.Error is { } refused) { return Report(refused); }

        console.WriteLine($"Theme \"{result.Name}\" registered.");

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

        console.WriteLine($"Theme \"{name}\" unregistered.");

        return 0;
    }

    /// <summary>
    ///     The theme's own "meta" block, one field per line. Skips a field the file left unset
    ///     rather than printing it empty, and says so plainly when none is declared at all.
    /// </summary>
    /// <param name="name">The theme to describe.</param>
    /// <param name="commandLine">What this invocation asked for, for --theme-dir.</param>
    private int ThemeInfo(string name, SluggerOptions commandLine) {
        Outcome<ThemeDocument> outcome = themeInfo.Execute(name, commandLine);
        if (outcome.Error is { } refused) { return Report(refused); }

        ThemeDocument         theme    = outcome.GetResultOrThrow();
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
    ///     A theme directory someone named - on the command line or in the saved defaults - that is
    ///     not there. Without a word, a typo in it falls back to the built-in themes and nobody knows
    ///     why their own went missing. Not for the default directory, which nobody named, nor for
    ///     <c>--register</c>, which creates the directory it writes into.
    /// </summary>
    /// <param name="command">What this run does.</param>
    /// <param name="directory">The theme directory in effect, or null for the default one.</param>
    private void WarnAboutAMissingThemeDirectory(CliCommand command, string? directory) {
        if (command == CliCommand.Register) { return; }
        if (directory is null) { return; }
        if (Directory.Exists(directory)) { return; }

        Warn($"warning: the theme directory \"{directory}\" does not exist");
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

    /// <summary>
    ///     A refused run, with one line more where it was refused for a theme named by its path: the
    ///     refusal says the theme is not there, and this says where the path should have gone.
    /// </summary>
    /// <param name="rejection">The refusal of the run.</param>
    private int ReportDrawing(Error rejection) {
        if (!AsksForAThemeByItsPath(rejection)) { return Report(rejection); }

        console.WriteError(ReportRenderer.Drawn([.. ReportRenderer.Render(rejection), ThemeTakesAName], Color.Red));

        return Refused;
    }

}