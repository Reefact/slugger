#region Usings declarations

using Slugger.Application.Options;
using Slugger.Domain;
using Slugger.Infrastructure.Configuration;

#endregion

namespace Slugger.UnitTests;

public sealed class XdgConfigStoreTests : IDisposable {

    #region Fields

    private readonly TemporaryDirectory _temp = new();

    #endregion

    private string Directory => _temp.Path;

    public void Dispose() {
        _temp.Dispose();
    }

    [Fact]
    public void Reads_back_what_it_wrote() {
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
    ///     A saved config has to be able to say nothing about an option, not just say "the default":
    ///     the precedence chain reads null as "let the layer below speak".
    /// </summary>
    [Fact]
    public void An_option_it_says_nothing_about_reads_back_as_nothing() {
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
    public void No_file_at_all_is_simply_no_config() {
        // Exercise
        SluggerOptions? read = new XdgConfigStore(Path.Combine(Directory, "absent.json")).Load();

        // Verify
        Assert.Null(read);
    }

    /// <summary>
    ///     A broken file in the home directory must not make the tool unusable, and the fix -
    ///     running --init again - is one command away.
    /// </summary>
    [Fact]
    public void A_config_that_will_not_parse_is_treated_as_none() {
        // Setup
        string path = Path.Combine(Directory, "config.json");
        File.WriteAllText(path, "{ not json at all");

        // Exercise
        SluggerOptions? read = new XdgConfigStore(path).Load();

        // Verify
        Assert.Null(read);
    }

}