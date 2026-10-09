#region Usings declarations

using Slugger.Application.Abstractions;
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

    /// <summary>
    ///     Treated as none, and never silently: whoever edited the file by hand has to learn that the
    ///     run ignored it, and which file it was.
    /// </summary>
    [Fact]
    public void Says_that_a_config_that_will_not_parse_was_ignored() {
        // Setup
        string path = Path.Combine(Directory, "config.json");
        File.WriteAllText(path, "{ not json at all");

        // Exercise
        SavedConfig read = new XdgConfigStore(path).Read();

        // Verify
        Assert.Null(read.Options);
        Assert.Equal($"{path} is not valid JSON and was ignored", Assert.Single(read.Remarks));
    }

    /// <summary>The spelling a theme's defaults use, which is what a hand-edited file most often carries.</summary>
    [Fact]
    public void Reads_a_key_whatever_its_case() {
        // Setup
        string path = Path.Combine(Directory, "config.json");
        File.WriteAllText(path, """{ "casing": "camel", "count": 3 }""");

        // Exercise
        SavedConfig read = new XdgConfigStore(path).Read();

        // Verify
        Assert.Equal(Casing.Camel, read.Options!.Casing);
        Assert.Equal(3, read.Options.Count);
        Assert.Empty(read.Remarks);
    }

    [Fact]
    public void Names_a_key_that_means_nothing_and_reads_the_rest() {
        // Setup
        string path = Path.Combine(Directory, "config.json");
        File.WriteAllText(path, """{ "colour": "red", "Count": 2 }""");

        // Exercise
        SavedConfig read = new XdgConfigStore(path).Read();

        // Verify
        Assert.Equal($"{path}: unknown key \"colour\"", Assert.Single(read.Remarks));
        Assert.Equal(2, read.Options!.Count);
    }

    /// <summary>
    ///     A theme's spelling and the command line's, where they differ from the key by more than case:
    ///     the reader typed what they had seen elsewhere, and the remark says what it is called here.
    /// </summary>
    /// <param name="written">The key as a theme file or the command line spells it.</param>
    /// <param name="meant">The key the config calls it.</param>
    [Theory]
    [InlineData("sep", "Separator")]
    [InlineData("wordSep", "WordSeparator")]
    [InlineData("allowSmall", "AllowSmallTheme")]
    [InlineData("theme", "Themes")]
    [InlineData("theme-dir", "ThemeDirectory")]
    [InlineData("segment", "SegmentMode")]
    [InlineData("max-length", "MaxLength")]
    [InlineData("token_glued", "TokenGlued")]
    public void Suggests_the_key_another_spelling_stands_for(string written, string meant) {
        // Setup
        string path = Path.Combine(Directory, "config.json");
        File.WriteAllText(path, $$"""{ "{{written}}": null }""");

        // Exercise
        SavedConfig read = new XdgConfigStore(path).Read();

        // Verify
        Assert.Equal($"{path}: unknown key \"{written}\"; did you mean \"{meant}\"?", Assert.Single(read.Remarks));
    }

    [Fact]
    public void Says_which_value_it_could_not_read_when_that_left_the_file_ignored() {
        // Setup
        string path = Path.Combine(Directory, "config.json");
        File.WriteAllText(path, """{ "Count": 2, "Casing": "SHOUT" }""");

        // Exercise
        SavedConfig read = new XdgConfigStore(path).Read();

        // Verify
        Assert.Null(read.Options);
        Assert.Equal($"{path}: the value of \"Casing\" cannot be read, so the file was ignored", Assert.Single(read.Remarks));
    }

    [Fact]
    public void Says_that_a_document_holding_no_options_was_ignored() {
        // Setup
        string path = Path.Combine(Directory, "config.json");
        File.WriteAllText(path, """["docker"]""");

        // Exercise
        SavedConfig read = new XdgConfigStore(path).Read();

        // Verify
        Assert.Null(read.Options);
        Assert.Equal($"{path} does not hold slugger's defaults and was ignored", Assert.Single(read.Remarks));
    }

    [Fact]
    public void Has_nothing_to_say_about_a_file_it_wrote_itself() {
        // Setup
        XdgConfigStore store = new(Path.Combine(Directory, "config.json"));
        store.Save(new SluggerOptions { Count = 5, Clipboard = false, MaxSegmentWords = SegmentWordsCap.None });

        // Exercise
        SavedConfig read = store.Read();

        // Verify
        Assert.Empty(read.Remarks);
        Assert.Equal(5, read.Options!.Count);
    }

}