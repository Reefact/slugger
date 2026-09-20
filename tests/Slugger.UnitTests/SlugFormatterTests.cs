using Slugger.Domain;
using Slugger.Domain.Generation;

namespace Slugger.UnitTests;

public sealed class SlugFormatterTests
{
    [Theory]
    [InlineData(Casing.Kebab, '-', "gorgeous-khorana")]
    [InlineData(Casing.Snake, '_', "gorgeous_khorana")]
    public void Joins_the_segments_with_the_separator(Casing casing, char separator, string expected)
    {
        // Setup
        GenerationOptions options = new() { Casing = casing, Separator = separator };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "khorana"], token: null, options);

        // Verify
        Assert.Equal(expected, slug);
    }

    /// <summary>
    /// Step 4 of normalization, applied here rather than at load time (DEC0005) because the
    /// separator is only known now - see WordNormalizer for why that matters.
    /// </summary>
    [Fact]
    public void Replaces_the_internal_spaces_of_a_compound_value_with_the_separator()
    {
        // Setup
        GenerationOptions options = new() { Separator = '-' };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], token: null, options);

        // Verify
        Assert.Equal("gorgeous-john-doe", slug);
    }

    /// <summary>
    /// The reason the option exists: a theme that writes "john doe" as two words can still hand
    /// out the one-word slug it had before the space was there.
    /// </summary>
    [Fact]
    public void Glues_the_words_of_a_compound_value_when_the_word_separator_is_empty()
    {
        // Setup
        GenerationOptions options = new() { Separator = '-', WordSeparator = "" };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], token: null, options);

        // Verify - one separator left, and it is the segment boundary.
        Assert.Equal("gorgeous-johndoe", slug);
    }

    /// <summary>
    /// The other reason: two different separators say in the text itself where the noun begins,
    /// which one separator for both leaves to be guessed.
    /// </summary>
    [Fact]
    public void Keeps_the_segment_boundary_apart_from_the_word_separator()
    {
        // Setup
        GenerationOptions options = new() { Separator = '-', WordSeparator = "_" };

        // Exercise
        string slug = SlugFormatter.Format(["big league", "john doe"], token: null, options);

        // Verify
        Assert.Equal("big_league-john_doe", slug);
    }

    [Fact]
    public void Camel_has_nowhere_to_put_a_word_separator_either()
    {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel, WordSeparator = "_" };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], token: null, options);

        // Verify - unchanged from the same slug without the option.
        Assert.Equal("gorgeousJohnDoe", slug);
    }

    /// <summary>
    /// Gluing the words does not glue the token: what rides the last segment is still decided by
    /// tokenGlued alone.
    /// </summary>
    [Fact]
    public void Appends_the_token_behind_the_separator_even_with_the_words_glued()
    {
        // Setup
        GenerationOptions options = new() { Separator = '-', WordSeparator = "", TokenGlued = false };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], "1337", options);

        // Verify
        Assert.Equal("gorgeous-johndoe-1337", slug);
    }

    /// <summary>
    /// Literal on purpose: the diacritics are the whole case. A theme writes its words as they
    /// are written, and the consumer folds them when the slug has to live somewhere that cannot
    /// take them.
    /// </summary>
    [Fact]
    public void Folds_a_letters_diacritic_away_when_asked_to()
    {
        // Setup
        GenerationOptions options = new() { FoldAccents = true };

        // Exercise
        string slug = SlugFormatter.Format(["risqué", "françois sagat"], token: null, options);

        // Verify
        Assert.Equal("risque-francois-sagat", slug);
    }

    /// <summary>
    /// The default has to stay where it was: a theme's own spelling reaches the slug untouched
    /// unless the run asks otherwise.
    /// </summary>
    [Fact]
    public void Leaves_a_diacritic_alone_unless_the_fold_was_asked_for()
    {
        // Exercise
        string slug = SlugFormatter.Format(["risqué", "françois sagat"], token: null, GenerationOptions.Default);

        // Verify
        Assert.Equal("risqué-françois-sagat", slug);
    }

    /// <summary>
    /// Folding happens before the casing, not instead of it, so camel gets folded words too.
    /// </summary>
    [Fact]
    public void Camel_folds_before_it_capitalises()
    {
        // Setup
        GenerationOptions options = new() { FoldAccents = true, Casing = Casing.Camel };

        // Exercise
        string slug = SlugFormatter.Format(["risqué", "françois sagat"], token: null, options);

        // Verify
        Assert.Equal("risqueFrancoisSagat", slug);
    }

    /// <summary>
    /// Literal on purpose, and the honest limit of the option: only a letter that decomposes
    /// folds. These three are Latin and none of them has a decomposition, so the flag cannot
    /// promise an ASCII slug - which is why it is named for the fold, not for the result.
    /// </summary>
    [Fact]
    public void Leaves_a_letter_that_does_not_decompose_exactly_as_written()
    {
        // Setup
        GenerationOptions options = new() { FoldAccents = true };

        // Exercise
        string slug = SlugFormatter.Format(["naïve", "søren straße"], token: null, options);

        // Verify - the diaeresis folds, the slashed o and the eszett do not.
        Assert.Equal("naive-søren-straße", slug);
    }

    /// <summary>
    /// Literal on purpose, and the reason the fold recomposes when it is done: Hangul decomposes
    /// into jamo, none of which is a combining mark, so the fold strips nothing - but handing the
    /// result back decomposed would return six code points where the theme wrote two. A flag that
    /// promises to fold accents must not quietly take a script apart.
    /// </summary>
    [Fact]
    public void Leaves_a_syllabic_script_composed_exactly_as_the_theme_wrote_it()
    {
        // Setup
        GenerationOptions options = new() { FoldAccents = true };

        // Exercise
        string slug = SlugFormatter.Format(["한글"], token: null, options);

        // Verify - as written, not the six code points its decomposition holds.
        Assert.Equal("한글", slug);
        Assert.Equal(2, slug.Length);
    }

    /// <summary>
    /// Literal on purpose: these are exactly the letters the fold cannot reach. Where
    /// --fold-accents leaves them as written, --ascii promises the result instead of the
    /// mechanism, so it disfigures them rather than give up.
    /// </summary>
    [Fact]
    public void Ascii_drops_a_letter_the_fold_cannot_reach()
    {
        // Setup
        GenerationOptions options = new() { Ascii = true };

        // Exercise
        string slug = SlugFormatter.Format(["søren straße"], token: null, options);

        // Verify - two words still, which a boundary in place of each letter would not have left.
        Assert.Equal("sren-strae", slug);
    }

    [Fact]
    public void Ascii_folds_an_accent_without_being_asked_to_fold_as_well()
    {
        // Setup - Ascii alone, FoldAccents left off.
        GenerationOptions options = new() { Ascii = true };

        // Exercise
        string slug = SlugFormatter.Format(["risqué", "françois sagat"], token: null, options);

        // Verify
        Assert.Equal("risque-francois-sagat", slug);
    }

    /// <summary>
    /// A segment that folds to nothing is dropped rather than joined as an empty one - the
    /// difference between disfiguring a slug and opening a hole in it.
    /// </summary>
    [Fact]
    public void Ascii_drops_a_segment_that_comes_back_empty_instead_of_joining_a_hole()
    {
        // Setup
        GenerationOptions options = new() { Ascii = true };

        // Exercise
        string slug = SlugFormatter.Format(["risqué", "한글 서울"], token: null, options);

        // Verify - no trailing separator where the second segment used to be.
        Assert.Equal("risque", slug);
    }

    /// <summary>
    /// The limit of the trade, pinned so it is a known outcome rather than a surprise: ask for
    /// ASCII from words that hold none and nothing is what comes back.
    /// </summary>
    [Fact]
    public void Ascii_returns_nothing_when_no_segment_survives_it()
    {
        // Setup
        GenerationOptions options = new() { Ascii = true };

        // Exercise
        string slug = SlugFormatter.Format(["москва", "한글"], token: null, options);

        // Verify
        Assert.Equal(string.Empty, slug);
    }

    [Fact]
    public void Camel_drops_the_separator_and_capitalises_every_word_but_the_first()
    {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "wandering", "john doe"], token: null, options);

        // Verify - the compound value's own words are capitalised too, having no separator left.
        Assert.Equal("gorgeousWanderingJohnDoe", slug);
    }

    [Fact]
    public void Appends_a_token_behind_the_separator()
    {
        // Setup
        GenerationOptions options = new() { Separator = '-', TokenGlued = false };

        // Exercise
        string slug = SlugFormatter.Format(["wispy", "dust"], "1337", options);

        // Verify
        Assert.Equal("wispy-dust-1337", slug);
    }

    /// <summary>Docker's collision digit rides on the noun with nothing between them.</summary>
    [Fact]
    public void Glues_a_token_straight_onto_the_last_segment()
    {
        // Setup
        GenerationOptions options = new() { Separator = '_', Casing = Casing.Snake, TokenGlued = true };

        // Exercise
        string slug = SlugFormatter.Format(["focused", "turing"], "3", options);

        // Verify
        Assert.Equal("focused_turing3", slug);
    }

    [Fact]
    public void Camel_appends_the_token_directly_having_no_separator_to_glue_it_with()
    {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel, TokenGlued = false };

        // Exercise
        string slug = SlugFormatter.Format(["wispy", "dust"], "1337", options);

        // Verify
        Assert.Equal("wispyDust1337", slug);
    }

    [Fact]
    public void Draws_no_token_when_the_length_is_zero()
    {
        // Setup
        GenerationOptions options = new() { TokenLength = 0 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource());

        // Verify
        Assert.Null(token);
    }

    [Fact]
    public void Draws_no_token_when_the_chance_is_zero()
    {
        // Setup
        GenerationOptions options = new() { TokenLength = Any.Int32().Between(1, 8).Generate(), TokenChance = 0 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource());

        // Verify
        Assert.Null(token);
    }

    [Fact]
    public void Draws_a_token_of_the_asked_length_in_decimal()
    {
        // Setup
        GenerationOptions options = new() { TokenLength = 4, TokenHex = false };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(1, 3, 3, 7));

        // Verify
        Assert.Equal("1337", token);
    }

    [Fact]
    public void Draws_a_hexadecimal_token_from_the_wider_alphabet()
    {
        // Setup
        GenerationOptions options = new() { TokenLength = 3, TokenHex = true };

        // Exercise - 10, 11, 15 index into "0123456789abcdef".
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(10, 11, 15));

        // Verify
        Assert.Equal("abf", token);
    }

    /// <summary>
    /// Next(100) lands in 0..99, so a chance of 1 draws only on a roll of 0. Docker leans on
    /// that to make its collision digit as rare as a real collision would be.
    /// </summary>
    [Theory]
    [InlineData(0, "7")]
    [InlineData(1, null)]
    [InlineData(99, null)]
    public void Weighs_the_token_against_the_chance_before_drawing_it(int roll, string? expected)
    {
        // Setup
        GenerationOptions options = new() { TokenLength = 1, TokenChance = 1 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(roll, 7));

        // Verify
        Assert.Equal(expected, token);
    }
}
