using FirstClassErrors;
using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Domain.Validation;
using Slugger.Infrastructure.Configuration;
using Slugger.Infrastructure.ThemeCatalogs;

namespace Slugger.UnitTests;

/// <summary>
/// A directory of its own per test, removed afterwards, so nothing leaks between them. Held
/// rather than inherited: a sealed test class disposing a field is the whole pattern, where an
/// abstract base would owe the virtual one.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    internal TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"slugger-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(Path);
    }

    internal string Path { get; }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Path))
        {
            System.IO.Directory.Delete(Path, recursive: true);
        }
    }

    /// <summary>Writes a theme that clears every rule, so a test can be about the file system rather than validation.</summary>
    /// <param name="name">The theme name, which the file is named after.</param>
    internal string WriteValidTheme(string name)
    {
        string path = System.IO.Path.Combine(Path, $"{name}.json");
        File.WriteAllText(path, ThemeFiles.Valid());

        return path;
    }
}

public sealed class FileSystemThemeCatalogTests : IDisposable
{
    private readonly TemporaryDirectory _temp = new();

    private string Directory => _temp.Path;

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Loads_a_theme_from_its_file_name()
    {
        // Setup
        _temp.WriteValidTheme("porno");
        FileSystemThemeCatalog catalog = new(Directory);

        // Exercise
        Outcome<Theme> outcome = catalog.Load("porno");

        // Verify - the name comes from the file, never from a field inside it.
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
        Assert.Equal("porno", outcome.GetResultOrThrow().Name);
    }

    [Fact]
    public void Lists_the_json_files_it_finds()
    {
        // Setup
        _temp.WriteValidTheme("beta");
        _temp.WriteValidTheme("alpha");
        File.WriteAllText(Path.Combine(Directory, "notes.txt"), "ignored");

        // Exercise
        IReadOnlyList<string> names = new FileSystemThemeCatalog(Directory).ListNames();

        // Verify
        Assert.Equal(["alpha", "beta"], names);
    }

    [Fact]
    public void A_directory_that_does_not_exist_simply_carries_nothing()
    {
        // Exercise
        IReadOnlyList<string> names = new FileSystemThemeCatalog(Path.Combine(Directory, "absent")).ListNames();

        // Verify
        Assert.Empty(names);
    }

    [Fact]
    public void Reports_a_theme_it_does_not_carry_rather_than_returning_nothing()
    {
        // Exercise
        Outcome<Theme> outcome = new FileSystemThemeCatalog(Directory).Load("absent");

        // Verify
        Error only = Assert.Single(outcome.Error!.InnerErrors);
        Assert.Equal(ThemeErrors.Codes.NotFound, only.Code);
    }
}

public sealed class ChainedThemeCatalogTests : IDisposable
{
    private readonly TemporaryDirectory _temp = new();

    private string Directory => _temp.Path;

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void A_custom_file_shadows_the_built_in_theme_of_the_same_name()
    {
        // Setup - a custom "docker" beside the embedded one.
        _temp.WriteValidTheme("docker");
        ChainedThemeCatalog catalog = new(new FileSystemThemeCatalog(Directory), new EmbeddedThemeCatalog());

        // Exercise
        Theme docker = catalog.Load("docker").GetResultOrThrow();

        // Verify - the custom file's 120 nouns, not the built-in theme's 236.
        Assert.Equal(120, docker.Nouns.Count);
    }

    /// <summary>
    /// The chain stops at the catalog that carries the name even when the theme is refused.
    /// Falling through would hand back the built-in docker and leave the author's broken file
    /// unmentioned.
    /// </summary>
    [Fact]
    public void A_broken_custom_file_is_reported_rather_than_skipped()
    {
        // Setup
        File.WriteAllText(Path.Combine(Directory, "docker.json"), """{ "adjectives": {}, "nouns": [] }""");
        ChainedThemeCatalog catalog = new(new FileSystemThemeCatalog(Directory), new EmbeddedThemeCatalog());

        // Exercise
        Outcome<Theme> outcome = catalog.Load("docker");

        // Verify
        Assert.True(outcome.IsFailure);
    }

    [Fact]
    public void Lists_both_origins_without_repeating_a_name()
    {
        // Setup
        _temp.WriteValidTheme("docker");
        _temp.WriteValidTheme("porno");
        ChainedThemeCatalog catalog = new(new FileSystemThemeCatalog(Directory), new EmbeddedThemeCatalog());

        // Exercise
        IReadOnlyList<string> names = catalog.ListNames();

        // Verify
        Assert.Equal(["docker", "heroku", "porno", "slugger"], names);
    }
}

public sealed class FileSystemThemeStoreTests : IDisposable
{
    private readonly TemporaryDirectory _temp = new();

    private string Directory => _temp.Path;

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Creates_the_directory_on_a_first_save()
    {
        // Setup - a path that does not exist yet, as it would not on a fresh machine.
        string fresh = Path.Combine(Directory, "themes");
        FileSystemThemeStore store = new(fresh);

        // Exercise
        store.Save("porno", "{}");

        // Verify
        Assert.True(File.Exists(Path.Combine(fresh, "porno.json")));
    }

    [Fact]
    public void Knows_what_it_holds_and_forgets_what_it_deletes()
    {
        // Setup
        FileSystemThemeStore store = new(Directory);
        store.Save("porno", "{}");

        // Exercise
        bool held = store.Contains("porno");
        store.Delete("porno");

        // Verify
        Assert.True(held);
        Assert.False(store.Contains("porno"));
    }

    [Fact]
    public void Validates_a_file_handed_to_it_by_path()
    {
        // Setup
        string path = _temp.WriteValidTheme("porno");

        // Exercise
        Outcome<Theme> outcome = new FileSystemThemeStore(Directory).LoadFile(path);

        // Verify
        Assert.True(outcome.IsSuccess, outcome.Error?.DiagnosticMessage);
    }

    [Fact]
    public void Refuses_a_path_that_leads_nowhere()
    {
        // Exercise
        Outcome<Theme> outcome = new FileSystemThemeStore(Directory).LoadFile(Path.Combine(Directory, "absent.json"));

        // Verify
        Assert.True(outcome.IsFailure);
    }
}

public sealed class XdgConfigStoreTests : IDisposable
{
    private readonly TemporaryDirectory _temp = new();

    private string Directory => _temp.Path;

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Reads_back_what_it_wrote()
    {
        // Setup
        XdgConfigStore store = new(Path.Combine(Directory, "slugger", "config.json"));
        SluggerOptions saved = new() { Count = 5, Separator = '_', Casing = Casing.Snake, Themes = ["docker", "heroku"] };

        // Exercise
        store.Save(saved);
        SluggerOptions? read = store.Load();

        // Verify
        Assert.Equal(5, read!.Count);
        Assert.Equal('_', read.Separator);
        Assert.Equal(Casing.Snake, read.Casing);
        Assert.Equal(["docker", "heroku"], read.Themes);
    }

    /// <summary>
    /// A saved config has to be able to say nothing about an option, not just say "the default":
    /// the precedence chain reads null as "let the layer below speak".
    /// </summary>
    [Fact]
    public void An_option_it_says_nothing_about_reads_back_as_nothing()
    {
        // Setup
        XdgConfigStore store = new(Path.Combine(Directory, "config.json"));

        // Exercise
        store.Save(new SluggerOptions { Count = 3 });
        SluggerOptions? read = store.Load();

        // Verify
        Assert.Null(read!.Separator);
        Assert.Null(read.Seed);
        Assert.Null(read.Oneshot);
    }

    [Fact]
    public void No_file_at_all_is_simply_no_config()
    {
        // Exercise
        SluggerOptions? read = new XdgConfigStore(Path.Combine(Directory, "absent.json")).Load();

        // Verify
        Assert.Null(read);
    }

    /// <summary>
    /// A broken file in the home directory must not make the tool unusable, and the fix -
    /// running --init again - is one command away.
    /// </summary>
    [Fact]
    public void A_config_that_will_not_parse_is_treated_as_none()
    {
        // Setup
        string path = Path.Combine(Directory, "config.json");
        File.WriteAllText(path, "{ not json at all");

        // Exercise
        SluggerOptions? read = new XdgConfigStore(path).Load();

        // Verify
        Assert.Null(read);
    }
}
