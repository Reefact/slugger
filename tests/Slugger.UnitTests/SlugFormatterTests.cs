#region Usings declarations

using Slugger.Domain;
using Slugger.Domain.Generation;

#endregion

namespace Slugger.UnitTests;

public sealed class SlugFormatterTests {

    /// <summary>
    ///     DEC0030, the whole grid - every casing crossed with no separator, an empty one and three
    ///     characters: a casing sets the capitals and a joiner of its own, and a separator, when there
    ///     is one, replaces the joiner and nothing else, nothing included. Literal on purpose: the
    ///     capitals and the joiner are the case.
    /// </summary>
    [Theory]
    [InlineData(Casing.Kebab, null, "robust-rising-jacob")]
    [InlineData(Casing.Kebab, "", "robustrisingjacob")]
    [InlineData(Casing.Kebab, "-", "robust-rising-jacob")]
    [InlineData(Casing.Kebab, "_", "robust_rising_jacob")]
    [InlineData(Casing.Kebab, ".", "robust.rising.jacob")]
    [InlineData(Casing.Snake, null, "robust_rising_jacob")]
    [InlineData(Casing.Snake, "", "robustrisingjacob")]
    [InlineData(Casing.Snake, "-", "robust-rising-jacob")]
    [InlineData(Casing.Snake, "_", "robust_rising_jacob")]
    [InlineData(Casing.Snake, ".", "robust.rising.jacob")]
    [InlineData(Casing.Constant, null, "ROBUST_RISING_JACOB")]
    [InlineData(Casing.Constant, "", "ROBUSTRISINGJACOB")]
    [InlineData(Casing.Constant, "-", "ROBUST-RISING-JACOB")]
    [InlineData(Casing.Constant, "_", "ROBUST_RISING_JACOB")]
    [InlineData(Casing.Constant, ".", "ROBUST.RISING.JACOB")]
    [InlineData(Casing.Camel, null, "robustRisingJacob")]
    [InlineData(Casing.Camel, "", "robustRisingJacob")]
    [InlineData(Casing.Camel, "-", "robust-Rising-Jacob")]
    [InlineData(Casing.Camel, "_", "robust_Rising_Jacob")]
    [InlineData(Casing.Camel, ".", "robust.Rising.Jacob")]
    [InlineData(Casing.Pascal, null, "RobustRisingJacob")]
    [InlineData(Casing.Pascal, "", "RobustRisingJacob")]
    [InlineData(Casing.Pascal, "-", "Robust-Rising-Jacob")]
    [InlineData(Casing.Pascal, "_", "Robust_Rising_Jacob")]
    [InlineData(Casing.Pascal, ".", "Robust.Rising.Jacob")]
    public void A_casing_sets_the_capitals_and_a_separator_replaces_its_joiner(Casing casing, string? separator, string expected) {
        // Setup
        GenerationOptions options = new() { Casing = casing, Separator = separator };

        // Exercise
        string slug = SlugFormatter.Format(["robust", "rising", "jacob"], null, options);

        // Verify
        Assert.Equal(expected, slug);
    }

    /// <summary>
    ///     Constant is written in capitals to the letter, the token's hexadecimal digits included -
    ///     a lowercase "3fa" at the end of an uppercase slug would read as two conventions.
    /// </summary>
    [Fact]
    public void Constant_writes_the_token_in_capitals_too() {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Constant };

        // Exercise
        string slug = SlugFormatter.Format(["wispy", "dust"], "3fa", options);

        // Verify
        Assert.Equal("WISPY_DUST_3FA", slug);
    }

    /// <summary>Literal on purpose: the accent is the case - slugger keeps it (DEC0009), so it is capitalised too.</summary>
    [Fact]
    public void Constant_and_pascal_capitalise_an_accented_letter() {
        // Exercise
        string constant = SlugFormatter.Format(["érudit", "élan"], null, new GenerationOptions { Casing = Casing.Constant });
        string pascal   = SlugFormatter.Format(["érudit", "élan"], null, new GenerationOptions { Casing = Casing.Pascal });

        // Verify
        Assert.Equal("ÉRUDIT_ÉLAN", constant);
        Assert.Equal("ÉruditÉlan", pascal);
    }

    /// <summary>
    ///     The token follows the joiner, whatever the casing: camel given a separator writes one before
    ///     the token too, unless the token is glued.
    /// </summary>
    [Fact]
    public void Camel_given_a_separator_writes_it_before_the_token() {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel, Separator = "-" };

        // Exercise
        string slug = SlugFormatter.Format(["wispy", "dust"], "1337", options);

        // Verify
        Assert.Equal("wispy-Dust-1337", slug);
    }

    /// <summary>
    ///     The door a slug goes through, and the reason the type is called a slug formatter: what
    ///     comes out is this run's string for that slug, separator and casing included - none of
    ///     which the slug carries.
    /// </summary>
    [Fact]
    public void Writes_a_slug_out_with_this_runs_separator_and_casing() {
        // Setup
        Slug slug = new(
            Noun.Of(Term.FromOrThrow("john doe")),
            new Epithet(Adjective.Of(Term.FromOrThrow("gorgeous"))));

        // Exercise
        string written = SlugFormatter.Format(slug, new GenerationOptions { Separator = "-", WordSeparator = "_" });

        // Verify
        Assert.Equal("gorgeous-john_doe", written);
    }

    /// <summary>
    ///     A slug carrying no epithet writes its noun alone, and one carrying a token writes it
    ///     behind - neither needing the formatter to ask whether they are there.
    /// </summary>
    [Fact]
    public void Writes_what_a_slug_carries_and_leaves_out_what_it_does_not() {
        // Setup
        Noun  olivine = Noun.Of(Term.FromOrThrow("olivine"));
        Token token   = Token.Draw(
            TokenMould.Of(TokenAlphabet.Decimal, TokenLength.FromOrThrow(2)),
            new ScriptedRandomSource(4, 2));

        // Exercise
        GenerationOptions options = new() { Separator = "-" };

        // Verify
        Assert.Equal("olivine", SlugFormatter.Format(new Slug(olivine), options));
        Assert.Equal("olivine-42", SlugFormatter.Format(new Slug(olivine, token: token), options));
    }

    /// <summary>
    ///     Step 4 of normalization, applied here rather than at load time (DEC0005) because the
    ///     separator is only known now - see WordNormalizer for why that matters.
    /// </summary>
    [Fact]
    public void Replaces_the_internal_spaces_of_a_compound_value_with_the_separator() {
        // Setup
        GenerationOptions options = new() { Separator = "-" };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], null, options);

        // Verify
        Assert.Equal("gorgeous-john-doe", slug);
    }

    /// <summary>
    ///     The reason the option exists: a theme that writes "john doe" as two words can still hand
    ///     out the one-word slug it had before the space was there.
    /// </summary>
    [Fact]
    public void Glues_the_words_of_a_compound_value_when_the_word_separator_is_empty() {
        // Setup
        GenerationOptions options = new() { Separator = "-", WordSeparator = "" };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], null, options);

        // Verify - one separator left, and it is the segment boundary.
        Assert.Equal("gorgeous-johndoe", slug);
    }

    /// <summary>
    ///     The other reason: two different separators say in the text itself where the noun begins,
    ///     which one separator for both leaves to be guessed.
    /// </summary>
    [Fact]
    public void Keeps_the_segment_boundary_apart_from_the_word_separator() {
        // Setup
        GenerationOptions options = new() { Separator = "-", WordSeparator = "_" };

        // Exercise
        string slug = SlugFormatter.Format(["big league", "john doe"], null, options);

        // Verify
        Assert.Equal("big_league-john_doe", slug);
    }

    /// <summary>
    ///     Camel joins nothing by default, but a word separator still goes where it is asked to go
    ///     (DEC0030): the casing sets the capitals, the separators set what joins them.
    /// </summary>
    [Fact]
    public void Camel_writes_a_word_separator_between_the_words_of_a_compound_term() {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel, WordSeparator = "_" };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], null, options);

        // Verify
        Assert.Equal("gorgeousJohn_Doe", slug);
    }

    /// <summary>
    ///     Gluing the words does not glue the token: what rides the last segment is still decided by
    ///     tokenGlued alone.
    /// </summary>
    [Fact]
    public void Appends_the_token_behind_the_separator_even_with_the_words_glued() {
        // Setup
        GenerationOptions options = new() { Separator = "-", WordSeparator = "", TokenGlued = false };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "john doe"], "1337", options);

        // Verify
        Assert.Equal("gorgeous-johndoe-1337", slug);
    }

    /// <summary>
    ///     Literal on purpose: the diacritics are the whole case. A theme writes its words as they
    ///     are written, and the consumer folds them when the slug has to live somewhere that cannot
    ///     take them.
    /// </summary>
    [Fact]
    public void Folds_a_letters_diacritic_away_when_asked_to() {
        // Setup
        GenerationOptions options = new() { FoldAccents = true };

        // Exercise
        string slug = SlugFormatter.Format(["risqué", "françois sagat"], null, options);

        // Verify
        Assert.Equal("risque-francois-sagat", slug);
    }

    /// <summary>
    ///     The default has to stay where it was: a theme's own spelling reaches the slug untouched
    ///     unless the run asks otherwise.
    /// </summary>
    [Fact]
    public void Leaves_a_diacritic_alone_unless_the_fold_was_asked_for() {
        // Exercise
        string slug = SlugFormatter.Format(["risqué", "françois sagat"], null, GenerationOptions.Default);

        // Verify
        Assert.Equal("risqué-françois-sagat", slug);
    }

    /// <summary>
    ///     Folding happens before the casing, not instead of it, so camel gets folded words too.
    /// </summary>
    [Fact]
    public void Camel_folds_before_it_capitalises() {
        // Setup
        GenerationOptions options = new() { FoldAccents = true, Casing = Casing.Camel };

        // Exercise
        string slug = SlugFormatter.Format(["risqué", "françois sagat"], null, options);

        // Verify
        Assert.Equal("risqueFrancoisSagat", slug);
    }

    /// <summary>
    ///     Literal on purpose, and the honest limit of the option: only a letter that decomposes
    ///     folds. These three are Latin and none of them has a decomposition, so the flag cannot
    ///     promise an ASCII slug - which is why it is named for the fold, not for the result.
    /// </summary>
    [Fact]
    public void Leaves_a_letter_that_does_not_decompose_exactly_as_written() {
        // Setup
        GenerationOptions options = new() { FoldAccents = true };

        // Exercise
        string slug = SlugFormatter.Format(["naïve", "søren straße"], null, options);

        // Verify - the diaeresis folds, the slashed o and the eszett do not.
        Assert.Equal("naive-søren-straße", slug);
    }

    /// <summary>
    ///     Literal on purpose, and the reason the fold recomposes when it is done: Hangul decomposes
    ///     into jamo, none of which is a combining mark, so the fold strips nothing - but handing the
    ///     result back decomposed would return six code points where the theme wrote two. A flag that
    ///     promises to fold accents must not quietly take a script apart.
    /// </summary>
    [Fact]
    public void Leaves_a_syllabic_script_composed_exactly_as_the_theme_wrote_it() {
        // Setup
        GenerationOptions options = new() { FoldAccents = true };

        // Exercise
        string slug = SlugFormatter.Format(["한글"], null, options);

        // Verify - as written, not the six code points its decomposition holds.
        Assert.Equal("한글", slug);
        Assert.Equal(2, slug.Length);
    }

    /// <summary>
    ///     Literal on purpose: these are exactly the letters the fold cannot reach. Where
    ///     --fold-accents leaves them as written, --ascii promises the result instead of the
    ///     mechanism, so it disfigures them rather than give up.
    /// </summary>
    [Fact]
    public void Ascii_drops_a_letter_the_fold_cannot_reach() {
        // Setup
        GenerationOptions options = new() { Ascii = true };

        // Exercise
        string slug = SlugFormatter.Format(["søren straße"], null, options);

        // Verify - two words still, which a boundary in place of each letter would not have left.
        Assert.Equal("sren-strae", slug);
    }

    [Fact]
    public void Ascii_folds_an_accent_without_being_asked_to_fold_as_well() {
        // Setup - Ascii alone, FoldAccents left off.
        GenerationOptions options = new() { Ascii = true };

        // Exercise
        string slug = SlugFormatter.Format(["risqué", "françois sagat"], null, options);

        // Verify
        Assert.Equal("risque-francois-sagat", slug);
    }

    /// <summary>
    ///     A segment that folds to nothing is dropped rather than joined as an empty one - the
    ///     difference between disfiguring a slug and opening a hole in it.
    /// </summary>
    [Fact]
    public void Ascii_drops_a_segment_that_comes_back_empty_instead_of_joining_a_hole() {
        // Setup
        GenerationOptions options = new() { Ascii = true };

        // Exercise
        string slug = SlugFormatter.Format(["risqué", "한글 서울"], null, options);

        // Verify - no trailing separator where the second segment used to be.
        Assert.Equal("risque", slug);
    }

    /// <summary>
    ///     The limit of the trade, pinned so it is a known outcome rather than a surprise: ask for
    ///     ASCII from words that hold none and nothing is what comes back.
    /// </summary>
    [Fact]
    public void Ascii_returns_nothing_when_no_segment_survives_it() {
        // Setup
        GenerationOptions options = new() { Ascii = true };

        // Exercise
        string slug = SlugFormatter.Format(["москва", "한글"], null, options);

        // Verify
        Assert.Equal(string.Empty, slug);
    }

    [Fact]
    public void Camel_drops_the_separator_and_capitalises_every_word_but_the_first() {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel };

        // Exercise
        string slug = SlugFormatter.Format(["gorgeous", "wandering", "john doe"], null, options);

        // Verify - the compound value's own words are capitalised too, having no separator left.
        Assert.Equal("gorgeousWanderingJohnDoe", slug);
    }

    [Fact]
    public void Appends_a_token_behind_the_separator() {
        // Setup
        GenerationOptions options = new() { Separator = "-", TokenGlued = false };

        // Exercise
        string slug = SlugFormatter.Format(["wispy", "dust"], "1337", options);

        // Verify
        Assert.Equal("wispy-dust-1337", slug);
    }

    /// <summary>Docker's collision digit rides on the noun with nothing between them.</summary>
    [Fact]
    public void Glues_a_token_straight_onto_the_last_segment() {
        // Setup
        GenerationOptions options = new() { Separator = "_", Casing = Casing.Snake, TokenGlued = true };

        // Exercise
        string slug = SlugFormatter.Format(["focused", "turing"], "3", options);

        // Verify
        Assert.Equal("focused_turing3", slug);
    }

    [Fact]
    public void Camel_appends_the_token_directly_having_no_separator_to_glue_it_with() {
        // Setup
        GenerationOptions options = new() { Casing = Casing.Camel, TokenGlued = false };

        // Exercise
        string slug = SlugFormatter.Format(["wispy", "dust"], "1337", options);

        // Verify
        Assert.Equal("wispyDust1337", slug);
    }

    [Fact]
    public void Draws_no_token_when_the_length_is_zero() {
        // Setup
        GenerationOptions options = new() { TokenLength = 0 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource());

        // Verify
        Assert.Null(token);
    }

    [Fact]
    public void Draws_no_token_when_the_chance_is_zero() {
        // Setup
        GenerationOptions options = new() { TokenLength = Any.Int32().Between(1, 8).Generate(), TokenChance = 0 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource());

        // Verify
        Assert.Null(token);
    }

    [Fact]
    public void Draws_a_token_of_the_asked_length_in_decimal() {
        // Setup
        GenerationOptions options = new() { TokenLength = 4, TokenHex = false };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(1, 3, 3, 7));

        // Verify
        Assert.Equal("1337", token);
    }

    [Fact]
    public void Draws_a_hexadecimal_token_from_the_wider_alphabet() {
        // Setup
        GenerationOptions options = new() { TokenLength = 3, TokenHex = true };

        // Exercise - 10, 11, 15 index into "0123456789abcdef".
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(10, 11, 15));

        // Verify
        Assert.Equal("abf", token);
    }

    /// <summary>
    ///     Next(100) lands in 0..99, so a chance of 1 draws only on a roll of 0. Docker leans on
    ///     that to make its collision digit as rare as a real collision would be.
    /// </summary>
    [Theory]
    [InlineData(0, "7")]
    [InlineData(1, null)]
    [InlineData(99, null)]
    public void Weighs_the_token_against_the_chance_before_drawing_it(int roll, string? expected) {
        // Setup
        GenerationOptions options = new() { TokenLength = 1, TokenChance = 1 };

        // Exercise
        string? token = SlugFormatter.DrawToken(options, new ScriptedRandomSource(roll, 7));

        // Verify
        Assert.Equal(expected, token);
    }

}