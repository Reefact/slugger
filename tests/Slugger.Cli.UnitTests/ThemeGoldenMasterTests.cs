#region Usings declarations

using System.Globalization;
using System.Reflection;
using System.Text;

using Slugger.Application.Abstractions;
using Slugger.Application.UseCases;
using Slugger.Cli.CommandLine;
using Slugger.Infrastructure.Configuration;
using Slugger.Infrastructure.ThemeCatalogs;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     What every shipped theme produces, pinned one file per theme. The files were written by the
///     engine as it stood before the vocabulary refactoring, so they are what says the refactoring
///     changed no slug rather than merely kept the suite green.
/// </summary>
/// <remarks>
///     <para>
///         <b>One file per theme, which is what makes it cheap to live with.</b> A theme's words are
///         edited often - nineteen of the twenty-one commits that landed while this branch was open
///         were word-level fixes to two themes - and every such edit moves the slugs it draws. Split
///         per theme, a diff names the one file that moved and the review is the theme's own; kept in
///         one file it would be a wall nobody reads, approved by reflex.
///     </para>
///     <para>
///         <b>Adding a theme adds its file.</b> The theory reads the theme directory rather than a
///         written list, so a new theme is measured the moment its <c>.json</c> lands - and fails,
///         with no <c>.verified</c> to compare against, until its own is written. That is the
///         reminder, and it costs nothing to maintain.
///     </para>
///     <para>
///         <b>Read through the command line, in process.</b> The seeded draw, the option chain and a
///         theme's own defaults all sit between a caller and a slug, and going through the library
///         would pin the engine while leaving that chain unmeasured. The exit code is captured
///         beside the output, because a theme too poor for a run refusing it is behaviour too -
///         <c>cocktails</c> under <c>--max-segment-words 1</c> is the case that says so.
///     </para>
/// </remarks>
public sealed class ThemeGoldenMasterTests : IDisposable {

    #region Static members

    /// <summary>The three themes compiled into the library, which no directory listing reports.</summary>
    private static readonly string[] Embedded = ["docker", "heroku", "slugger"];

    /// <summary>
    ///     What each theme is asked for. Every one carries a seed, so what is pinned is the draw and
    ///     not a coincidence; between them they reach the separator, the casing, the token in each of
    ///     its shapes, the length budget and the segment cap.
    /// </summary>
    private static readonly string[][] Variants = [
        ["--seed", "1", "--count", "20"],
        ["--seed", "42", "--count", "20", "--sep", "_"],
        ["--seed", "7", "--count", "20", "--token-length", "4"],
        ["--seed", "99", "--count", "20", "--token-length", "3", "--token-chance", "50"],
        ["--seed", "5", "--count", "20", "--max-segment-words", "1"],
        ["--seed", "11", "--count", "20", "--casing", "camel"],
        ["--seed", "23", "--count", "20", "--max-length", "30"],
        ["--seed", "64", "--count", "20", "--token-length", "5", "--token-hex", "--token-glued"]
    ];

    private static readonly string RepositoryRoot      = FindTheRepositoryRoot();
    private static readonly string ThemeDirectoryPath  = Path.Combine(RepositoryRoot, "themes");

    /// <summary>
    ///     Where the files live. Read from the repository rather than from the build output, for the
    ///     same reason the themes are: what is guarded is the state of the repository.
    /// </summary>
    private static readonly string GoldenMasterDirectory =
        Path.Combine(RepositoryRoot, "tests", "Slugger.Cli.UnitTests", "GoldenMaster");

    /// <summary>Every theme slugger can draw from: the three compiled in, and the files beside them.</summary>
    public static TheoryData<string> EveryTheme() {
        TheoryData<string> themes = [];
        foreach (string name in Embedded) {
            themes.Add(name);
        }
        foreach (string path in Directory.EnumerateFiles(ThemeDirectoryPath, "*.json").Order(StringComparer.Ordinal)) {
            themes.Add(Path.GetFileNameWithoutExtension(path));
        }

        return themes;
    }

    private static string FindTheRepositoryRoot() {
        string? root = typeof(ThemeGoldenMasterTests).Assembly
                                                     .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                     .FirstOrDefault(attribute => attribute.Key == "RepositoryRoot")
                                                    ?.Value;

        if (root is null) {
            throw new InvalidOperationException(
                "The test assembly carries no RepositoryRoot, so neither the repository's themes nor "
              + "its golden master can be found. It is written in by Slugger.Cli.UnitTests.csproj at "
              + "build time.");
        }

        return root;
    }

    /// <summary>What a mismatch is written beside its file as, so approving one is a rename.</summary>
    private static string ReceivedPathFor(string theme) {
        return Path.Combine(GoldenMasterDirectory, $"{theme}.received.txt");
    }

    private static string VerifiedPathFor(string theme) {
        return Path.Combine(GoldenMasterDirectory, $"{theme}.verified.txt");
    }

    #endregion

    #region Fields

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"slugger-golden-{Guid.NewGuid():N}");

    #endregion

    public void Dispose() {
        if (Directory.Exists(_directory)) { Directory.Delete(_directory, true); }
    }

    [Theory]
    [MemberData(nameof(EveryTheme))]
    public void Draws_what_it_drew_before_the_vocabulary_refactoring(string theme) {
        // Setup - a theme whose file nobody wrote has nothing to be measured against, and saying so
        // is the whole reminder that adding a theme adds its own.
        string verified = VerifiedPathFor(theme);
        if (!File.Exists(verified)) {
            Assert.Fail(
                $"Theme \"{theme}\" has no golden master. Generate {Path.GetFileName(verified)} and "
              + "read what it pins before committing it.");
        }

        // Exercise
        string drawn = Corpus(theme);

        // Verify - the drawn corpus is written out on a mismatch, so approving a change that is
        // meant is a rename rather than a hand-edit of two thousand lines.
        string expected = File.ReadAllText(verified);
        if (drawn != expected) { File.WriteAllText(ReceivedPathFor(theme), drawn); }

        Assert.Equal(expected, drawn);
    }

    /// <summary>
    ///     Every variant run against one theme, written as the file records it: the arguments that
    ///     asked, the exit code they got, and the lines they produced.
    /// </summary>
    /// <param name="theme">The theme to draw from.</param>
    private string Corpus(string theme) {
        StringBuilder written = new();
        foreach (string[] variant in Variants) {
            FakeConsole console = new();
            int         code    = Run(console, ["--theme-dir", ThemeDirectoryPath, "--theme", theme, .. variant]);

            written.Append("--- ").AppendLine(string.Join(' ', variant));
            written.Append("exit ").AppendLine(code.ToString(CultureInfo.InvariantCulture));
            foreach (string line in console.Output) {
                written.AppendLine(line);
            }
            written.AppendLine();
        }

        return written.ToString();
    }

    private int Run(FakeConsole console, params string[] arguments) {
        IConfigStore    config      = new XdgConfigStore(Path.Combine(_directory, "config.json"));
        IThemeDirectory directories = new ThemeDirectory();

        SluggerRunner runner = new(
            console,
            config,
            new GenerateSlugsUseCase(directories, config, new FakeClipboard()),
            new ListThemesUseCase(directories, config),
            new RegisterThemeUseCase(directories, config),
            new UnregisterThemeUseCase(directories, config),
            new SaveDefaultsUseCase(config),
            new AnalyzeThemeUseCase(directories, config),
            new ThemeInfoUseCase(directories, config),
            directories);

        return SluggerApp.Run(runner, console, SluggerApp.Terminal(TextWriter.Null, true), arguments);
    }

}
