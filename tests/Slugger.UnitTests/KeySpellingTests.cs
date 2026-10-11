#region Usings declarations

using Slugger.Infrastructure.Serialization;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     The guess behind "did you mean": close enough to name what was meant, never so loose that it
///     names a word nobody meant.
/// </summary>
/// <remarks>Literal on purpose: each key is the typo the case is about.</remarks>
public sealed class KeySpellingTests {

    private static readonly string[] NounKeys = ["value", "categories", "except"];

    [Fact]
    public void Names_a_key_written_in_another_case() {
        Assert.Equal("maxLength", KeySpelling.Nearest("maxlength", ["maxLength", "meta"]));
    }

    [Fact]
    public void Names_a_key_with_two_letters_swapped() {
        Assert.Equal("except", KeySpelling.Nearest("excpet", NounKeys));
    }

    [Fact]
    public void Names_a_key_with_a_letter_missing() {
        Assert.Equal("categories", KeySpelling.Nearest("categorie", NounKeys));
    }

    [Fact]
    public void Names_nothing_when_no_key_is_close() {
        Assert.Null(KeySpelling.Nearest("license", ["title", "description", "version", "author", "source", "createdAt", "publishedAt"]));
    }

    /// <summary>One slip in three letters is already another word: "set" is not a misspelt "sep".</summary>
    [Fact]
    public void Allows_a_short_key_one_slip_and_no_more() {
        Assert.Equal("sep", KeySpelling.Nearest("spe", ["sep"]));
        Assert.Null(KeySpelling.Nearest("ps", ["sep"]));
    }

}
