#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain.Validation;

/// <summary>
///     Every way a theme can be refused, declared once: one factory per situation, each with its code
///     in <see cref="Codes" />. A refused load returns one error with the code <see cref="Codes.Rejected" />,
///     whose inner errors are built here.
/// </summary>
/// <remarks>
///     <para>
///         Branch on an error's <see cref="Error.Code" />, compared with a constant of <see cref="Codes" />,
///         rather than on its message: messages may be reworded from one version to the next.
///     </para>
///     <para>
///         A factory per situation is what makes every caller report a situation the same way: the
///         command line and a library load produce the same error because they call the same factory.
///         A few factories - <see cref="NotFound" />, <see cref="AlreadyRegistered" />,
///         <see cref="NotAFile" /> - serve the command line's theme directory, and no library method
///         returns them.
///     </para>
///     <para>
///         See decision record DEC0006 (in French):
///         https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0006-rapport-groupe-des-refus.md
///     </para>
/// </remarks>
public static class ThemeErrors {

    #region Static members

    /// <summary>The noun a refusal is about.</summary>
    public static readonly ErrorContextKey<string> Noun = ErrorContextKey.Create<string>("Noun", "The noun the rule was evaluated for.");

    /// <summary>The category a refusal is about.</summary>
    public static readonly ErrorContextKey<string> Category = ErrorContextKey.Create<string>("Category", "The category the rule was evaluated for.");

    /// <summary>The categories the theme does declare, so a message can list the alternatives.</summary>
    public static readonly ErrorContextKey<string> KnownCategories = ErrorContextKey.Create<string>("KnownCategories", "Every category the theme declares.");

    /// <summary>What was counted: a pool size, a noun count, a combination total.</summary>
    public static readonly ErrorContextKey<long> Counted = ErrorContextKey.Create<long>("Counted", "What the rule counted.");

    /// <summary>The floor that count had to clear.</summary>
    public static readonly ErrorContextKey<long> Minimum = ErrorContextKey.Create<long>("Minimum", "The floor the count had to clear.");

    /// <summary>The segment mode the floor was chosen for, since the floors differ by mode.</summary>
    public static readonly ErrorContextKey<string> Mode = ErrorContextKey.Create<string>("Mode", "The segment mode the floor was chosen for.");

    /// <summary>The section of the document at fault.</summary>
    public static readonly ErrorContextKey<string> Section = ErrorContextKey.Create<string>("Section", "The section of the theme document at fault.");

    /// <summary>The theme the report is about.</summary>
    public static readonly ErrorContextKey<string> ThemeName = ErrorContextKey.Create<string>("ThemeName", "The theme the report is about.");

    /// <summary>The whole report: one error holding every reason the theme was refused, as its inner errors.</summary>
    /// <param name="themeName">What the theme would have been called.</param>
    /// <param name="reasons">Every reason, not just the first.</param>
    public static DomainError Rejected(string themeName, IEnumerable<DomainError> reasons) {
        return DomainError.Create(
                               Codes.Rejected,
                               $"Theme \"{themeName}\" was refused",
                               reasons,
                               context => context.Add(ThemeName, themeName))
                          .WithPublicMessage("The theme cannot be used.", "See the reasons it carries.");
    }

    /// <summary>No theme of that name is available. Used by the command line.</summary>
    /// <param name="name">The theme that was asked for.</param>
    /// <param name="available">The themes that are available, so the message can list them.</param>
    public static DomainError NotFound(string name, IReadOnlyList<string> available) {
        return DomainError.Create(
                               Codes.NotFound,
                               available.Count == 0
                                   ? $"No theme named \"{name}\", and no theme is available at all."
                                   : $"No theme named \"{name}\". Available: {string.Join(", ", available)}.",
                               context => context.Add(ThemeName, name).Add(KnownCategories, string.Join(", ", available)))
                          .WithPublicMessage("That theme does not exist.");
    }

    /// <summary>
    ///     No theme compiled into the library has that name. Its code is <see cref="Codes.NotFound" />,
    ///     the same as a theme the command line cannot find.
    /// </summary>
    /// <param name="name">The theme that was asked for.</param>
    /// <param name="builtIn">The themes compiled into the library, so the message can list them.</param>
    public static DomainError NotBuiltIn(string name, IReadOnlyList<string> builtIn) {
        return DomainError.Create(
                               Codes.NotFound,
                               $"There is no built-in theme named \"{name}\". Built-in themes: {string.Join(", ", builtIn)}.",
                               context => context.Add(ThemeName, name).Add(KnownCategories, string.Join(", ", builtIn)))
                          .WithPublicMessage("That theme does not exist.");
    }

    /// <summary>
    ///     The theme file does not exist. Its code is <see cref="Codes.NotFound" />, the same as a theme
    ///     that cannot be found by name.
    /// </summary>
    /// <param name="path">The file, as it was given.</param>
    public static DomainError NoSuchFile(string path) {
        return DomainError.Create(
                               Codes.NotFound,
                               $"\"{path}\" does not exist.")
                          .WithPublicMessage("That theme file does not exist.");
    }

    /// <summary>
    ///     A theme of that name is already in the theme directory, and nothing is overwritten by accident.
    ///     Used by the command line.
    /// </summary>
    /// <param name="name">The theme that already exists.</param>
    public static DomainError AlreadyRegistered(string name) {
        return DomainError.Create(
                               Codes.AlreadyRegistered,
                               $"A theme \"{name}\" already exists in the theme directory; unregister it first to replace it.",
                               context => context.Add(ThemeName, name))
                          .WithPublicMessage("That theme is already registered.");
    }

    /// <summary>
    ///     A theme compiled into the library has no file to remove from a theme directory. Used by the
    ///     command line.
    /// </summary>
    /// <param name="name">The theme that has no file to remove.</param>
    public static DomainError NotAFile(string name) {
        return DomainError.Create(
                               Codes.NotAFile,
                               $"\"{name}\" is embedded in the binary, so there is nothing to unregister - to stop using it, leave it out of --theme.",
                               context => context.Add(ThemeName, name))
                          .WithPublicMessage("That theme is built in and cannot be unregistered.");
    }

    /// <summary>
    ///     The file is not valid JSON. It comes alone: no other rule can run on a file that does not parse.
    /// </summary>
    /// <param name="reason">What is wrong there, in a few words ending with a full stop.</param>
    /// <param name="line">The line it is on, counted from one as an editor counts it.</param>
    /// <param name="column">The column it is at, counted from one in characters.</param>
    public static DomainError MalformedJson(string reason, long line, long column) {
        return DomainError.Create(
                               Codes.MalformedJson,
                               $"The file is not valid JSON at line {line}, column {column}: {reason}",
                               context => context.Add(Counted, line))
                          .WithPublicMessage("The theme file is not valid JSON.");
    }

    /// <summary>A section is missing, or does not have the shape a theme file calls for.</summary>
    /// <param name="section">The section at fault, as it is spelled in the file.</param>
    /// <param name="expected">The shape it had to have.</param>
    public static DomainError MalformedSection(string section, string expected) {
        return DomainError.Create(
                               Codes.MalformedSection,
                               $"\"{section}\" must be {expected}.",
                               context => context.Add(Section, section))
                          .WithPublicMessage("A section of the theme file has the wrong shape.");
    }

    /// <summary>An entry of "nouns" is not an object with a non-empty "value".</summary>
    /// <param name="index">Its position in the array, since it has no name to be called by.</param>
    /// <param name="detail">What is wrong with it.</param>
    public static DomainError MalformedNoun(int index, string detail) {
        return DomainError.Create(
                               Codes.MalformedNoun,
                               $"nouns[{index}]: {detail}.",
                               context => context.Add(Section, $"nouns[{index}]").Add(Counted, index))
                          .WithPublicMessage("An entry of \"nouns\" is malformed.");
    }

    /// <summary>
    ///     A noun excludes a word the theme declares nowhere. Refused rather than ignored: an
    ///     exclusion that matches nothing fails open, so the theme reads as protected and is not,
    ///     and a typo would be the likeliest cause.
    /// </summary>
    /// <param name="noun">The noun that lists the exclusion.</param>
    /// <param name="word">The word that matches nothing.</param>
    public static DomainError ExclusionMatchesNothing(string noun, string word) {
        return DomainError.Create(
                               Codes.ExclusionMatchesNothing,
                               $"\"{noun}\" excludes \"{word}\", which the theme declares nowhere.",
                               context => context.Add(Noun, noun).Add(Category, word))
                          .WithPublicMessage("A noun excludes a word the theme does not declare.");
    }

    /// <summary>
    ///     An "incompatible" key names a word the theme declares nowhere in "adjectives". Refused
    ///     for the reason <see cref="ExclusionMatchesNothing" /> gives: a pair that matches nothing
    ///     fails open, so the theme reads as protected and is not.
    /// </summary>
    /// <param name="word">The key that matches no adjective.</param>
    /// <param name="declaredAsAParticiple">
    ///     Whether the theme declares it in "participles", which makes a reversed pair by far the
    ///     likeliest cause - and is worth saying rather than leaving the author to find.
    /// </param>
    public static DomainError IncompatibleAdjectiveNotDeclared(string word, bool declaredAsAParticiple) {
        return DomainError.Create(
                               Codes.IncompatibleAdjectiveNotDeclared,
                               $"\"incompatible\" names \"{word}\" as an adjective, which the theme declares nowhere in \"adjectives\"."
                             + (declaredAsAParticiple
                                   ? " It is declared as a participle, so the pair may be the wrong way round: the key refuses, the words are refused."
                                   : string.Empty),
                               context => context.Add(Category, word).Add(Section, "incompatible"))
                          .WithPublicMessage("An incompatibility names an adjective the theme does not declare.");
    }

    /// <summary>An "incompatible" entry refuses a word the theme declares nowhere in "participles".</summary>
    /// <param name="adjective">The adjective that refuses the word.</param>
    /// <param name="word">The word that matches no participle.</param>
    /// <param name="declaredAsAnAdjective">Whether it is an adjective, which again suggests a reversed pair.</param>
    public static DomainError IncompatibleParticipleNotDeclared(string adjective, string word, bool declaredAsAnAdjective) {
        return DomainError.Create(
                               Codes.IncompatibleParticipleNotDeclared,
                               $"incompatible[\"{adjective}\"] refuses \"{word}\", which the theme declares nowhere in \"participles\"."
                             + (declaredAsAnAdjective
                                   ? " It is declared as an adjective, so the pair may be the wrong way round: the key refuses, the words are refused."
                                   : string.Empty),
                               context => context.Add(Noun, adjective).Add(Category, word).Add(Section, "incompatible"))
                          .WithPublicMessage("An incompatibility refuses a participle the theme does not declare.");
    }

    /// <summary>
    ///     An incompatibility takes a noun under the participle floor for one of the adjectives it
    ///     can draw. Reported against the worst adjective of that noun rather than every offending
    ///     one: a theme with a dozen pairs would otherwise report a dozen lines per noun, and the
    ///     worst is the one that says how far there is to go.
    /// </summary>
    /// <param name="noun">The noun left short.</param>
    /// <param name="adjective">The adjective it is left short after.</param>
    /// <param name="poolSize">What it still reaches with that adjective in front of it.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError IncompatibilityStarvesTheNoun(string noun, string adjective, int poolSize, int minimum) {
        return DomainError.Create(
                               Codes.IncompatibilityStarvesTheNoun,
                               $"\"{noun}\" reaches {Plural(poolSize, "participle")} after \"{adjective}\", but a theme drawing "
                             + $"\"both\" needs at least {minimum:N0} per noun for every adjective it can draw - "
                             + "either declare more participles for it, or drop the incompatibility.",
                               context => context
                                         .Add(Noun, noun)
                                         .Add(Category, adjective)
                                         .Add(Counted, poolSize)
                                         .Add(Minimum, minimum)
                                         .Add(Mode, Spelled(SegmentMode.Both)))
                          .WithPublicMessage("An incompatibility leaves a noun too few participles.");
    }

    /// <summary>
    ///     The theme can produce a slug longer than its own "maxLength" says. Refused rather than trimmed
    ///     at the draw: the promise is the theme's, so a promise it cannot keep is a fact about the file,
    ///     and the slug that breaks it is named so that a word can be shortened or dropped.
    /// </summary>
    /// <remarks>
    ///     See decision record DEC0018 (in French):
    ///     https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md
    /// </remarks>
    /// <param name="shape">The key that holds the promise, as it is spelled in the file.</param>
    /// <param name="longest">The longest slug the theme can actually produce in that shape.</param>
    /// <param name="promised">The ceiling the theme declared.</param>
    public static DomainError LongerThanPromised(string shape, string longest, int promised) {
        return DomainError.Create(
                               Codes.LongerThanPromised,
                               $"maxLength.{shape} promises {Plural(promised, "character")}, but the theme can produce "
                             + $"\"{longest}\" at {longest.Length:N0}.",
                               context => context
                                         .Add(Section, $"maxLength.{shape}")
                                         .Add(Counted, longest.Length)
                                         .Add(Minimum, promised))
                          .WithPublicMessage("The theme can produce a slug longer than it promises.");
    }

    /// <summary>
    ///     A length limit takes a noun under the participle floor for one of the adjectives it can draw.
    ///     Its own factory rather than the incompatibility one, because the fix is not the same: nothing
    ///     here is refused by a pair, the words simply no longer fit together.
    /// </summary>
    /// <remarks>
    ///     See decision record DEC0018 (in French):
    ///     https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md
    /// </remarks>
    /// <param name="noun">The noun left short.</param>
    /// <param name="adjective">The adjective that leaves it least room.</param>
    /// <param name="poolSize">What still fits after that adjective.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    /// <param name="maxLength">The ceiling that left it no room.</param>
    public static DomainError TheLimitStarvesTheNoun(string noun,
                                                     string adjective,
                                                     int    poolSize,
                                                     int    minimum,
                                                     int    maxLength) {
        return DomainError.Create(
                               Codes.TheLimitStarvesTheNoun,
                               $"Under {Plural(maxLength, "character")}, \"{noun}\" reaches {Plural(poolSize, "participle")} after "
                             + $"\"{adjective}\", but a theme drawing \"both\" needs at least {minimum:N0} per noun for every "
                             + "adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.",
                               context => context
                                         .Add(Noun, noun)
                                         .Add(Category, adjective)
                                         .Add(Counted, poolSize)
                                         .Add(Minimum, minimum)
                                         .Add(Mode, Spelled(SegmentMode.Both)))
                          .WithPublicMessage("The length asked for leaves a noun too few participles.");
    }

    /// <summary>A noun references a category that neither "adjectives" nor "participles" declares.</summary>
    /// <param name="noun">The noun that lists the unknown category.</param>
    /// <param name="category">The category that does not exist.</param>
    /// <param name="knownCategories">Every category the theme declares.</param>
    public static DomainError UnknownCategory(string noun, string category, IReadOnlyList<string> knownCategories) {
        return DomainError.Create(
                               Codes.UnknownCategory,
                               $"\"{noun}\" references category \"{category}\", which the theme does not declare "
                             + $"(it declares {string.Join(", ", knownCategories)}).",
                               context => context
                                         .Add(Noun, noun)
                                         .Add(Category, category)
                                         .Add(KnownCategories, string.Join(", ", knownCategories)))
                          .WithPublicMessage("A noun references a category the theme does not declare.");
    }

    /// <summary>
    ///     A theme with no noun at all cannot produce a single slug. Unlike the size floors, this is
    ///     never waived by <c>allowSmall</c>: that flag lets an author accept a small theme, not an
    ///     empty one, and the difference is the difference between a judgement call and a file that
    ///     cannot work.
    /// </summary>
    /// <param name="themeName">The theme holding nothing to draw.</param>
    public static DomainError NoNounToDrawFrom(string themeName) {
        return DomainError.Create(
                               Codes.NoNounToDrawFrom,
                               $"Theme \"{themeName}\" holds no noun, so it cannot produce a slug.",
                               context => context.Add(ThemeName, themeName))
                          .WithPublicMessage("That theme holds no noun.");
    }

    /// <summary>
    ///     A length limit leaves room for no noun at all, so the theme can produce nothing under it.
    ///     Never waived: a theme that can produce nothing is not a small theme.
    /// </summary>
    /// <remarks>
    ///     <see cref="Validation.ThemeValidator" /> reports it; generation itself, in the current version,
    ///     reports the same situation as <see cref="NoNounToDrawFrom" />.
    ///     See decision record DEC0018 (in French):
    ///     https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md
    /// </remarks>
    /// <param name="themeName">The theme nothing fits in.</param>
    /// <param name="maxLength">The ceiling that left no room.</param>
    public static DomainError NothingFitsTheLimit(string themeName, int maxLength) {
        return DomainError.Create(
                               Codes.NothingFitsTheLimit,
                               $"No slug of theme \"{themeName}\" fits in {Plural(maxLength, "character")}.",
                               context => context.Add(ThemeName, themeName).Add(Minimum, maxLength))
                          .WithPublicMessage("No slug of that theme fits the length asked for.");
    }

    /// <summary>
    ///     A words-per-term limit leaves the theme no noun to draw: every one of them is written in more
    ///     words than the limit allows.
    /// </summary>
    /// <remarks>
    ///     See decision record DEC0023 (in French):
    ///     https://github.com/Reefact/slugger/blob/main/docs/idr/DEC0023-plafond-de-mots-par-segment.md
    /// </remarks>
    /// <param name="themeName">The theme the run asked for.</param>
    /// <param name="maxSegmentWords">The cap the run set.</param>
    public static DomainError NoValueIsShortEnough(string themeName, int maxSegmentWords) {
        return DomainError.Create(
                               Codes.NoValueIsShortEnough,
                               $"No noun of theme \"{themeName}\" is written in {Plural(maxSegmentWords, "word")} or fewer.",
                               context => context.Add(ThemeName, themeName).Add(Minimum, maxSegmentWords))
                          .WithPublicMessage("No noun of that theme is short enough in words for the limit asked for.");
    }

    /// <summary>The theme holds fewer distinct nouns than the floor.</summary>
    /// <param name="count">How many nouns the theme declares.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError TooFewNouns(int count, int minimum) {
        return DomainError.Create(
                               Codes.TooFewNouns,
                               $"{Plural(count, "noun")}, but a theme needs at least {minimum:N0}.",
                               context => context.Add(Counted, count).Add(Minimum, minimum))
                          .WithPublicMessage("The theme holds too few nouns.");
    }

    /// <summary>
    ///     A noun reaches too few participles, in a theme whose segment mode draws them. The mode is
    ///     named rather than implied: it is what chose the floor, and an author asking why the number is
    ///     20 here and 100 there has the answer in the sentence.
    /// </summary>
    /// <param name="noun">The noun whose participle pool is too thin.</param>
    /// <param name="poolSize">What it actually reaches.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    /// <param name="mode">The segment mode that set that floor.</param>
    public static DomainError ParticiplePoolTooSmall(string noun, int poolSize, int minimum, SegmentMode mode) {
        return DomainError.Create(
                               Codes.ParticiplePoolTooSmall,
                               $"\"{noun}\" reaches {Plural(poolSize, "participle")}, but a theme drawing "
                             + $"\"{Spelled(mode)}\" needs at least {minimum:N0} per noun.",
                               context => context
                                         .Add(Noun, noun)
                                         .Add(Counted, poolSize)
                                         .Add(Minimum, minimum)
                                         .Add(Mode, Spelled(mode)))
                          .WithPublicMessage("A noun reaches too few participles.");
    }

    /// <summary>
    ///     A noun reaches too few words of any kind, in a theme drawing "either". That mode puts one
    ///     word in front of the noun and draws it from the two pools at once, so neither pool has a
    ///     floor of its own and the floor applies to their sum.
    /// </summary>
    /// <param name="noun">The noun whose two pools are too thin between them.</param>
    /// <param name="adjectives">What it reaches in "adjectives".</param>
    /// <param name="participles">What it reaches in "participles".</param>
    /// <param name="minimum">The floor the two had to clear together.</param>
    public static DomainError CombinedPoolTooSmall(string noun, int adjectives, int participles, int minimum) {
        return DomainError.Create(
                               Codes.CombinedPoolTooSmall,
                               $"\"{noun}\" reaches {Plural(adjectives + participles, "word")} to put in front of it "
                             + $"({adjectives:N0} in \"adjectives\", {participles:N0} in \"participles\"), but a theme drawing "
                             + $"\"either\" needs at least {minimum:N0} per noun.",
                               context => context
                                         .Add(Noun, noun)
                                         .Add(Counted, adjectives + participles)
                                         .Add(Minimum, minimum)
                                         .Add(Mode, Spelled(SegmentMode.Either)))
                          .WithPublicMessage("A noun reaches too few words to put in front of it.");
    }

    /// <summary>Some noun resolves to fewer adjectives than the floor.</summary>
    /// <param name="noun">The noun whose pool is too small - named, because a global count would hide it.</param>
    /// <param name="poolSize">How many adjectives that noun can reach.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError PoolTooSmall(string noun, int poolSize, int minimum) {
        return DomainError.Create(
                               Codes.PoolTooSmall,
                               $"\"{noun}\" reaches {Plural(poolSize, "adjective")}, but every noun needs at least {minimum:N0}.",
                               context => context.Add(Noun, noun).Add(Counted, poolSize).Add(Minimum, minimum))
                          .WithPublicMessage("A noun reaches too few adjectives.");
    }

    /// <summary>A whole branch of the theme is poor in combinations, even if each of its nouns clears the pool floor.</summary>
    /// <param name="category">The category that is too poor.</param>
    /// <param name="combinations">What its nouns total between them.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError CategoryTooPoor(string category, long combinations, long minimum) {
        return DomainError.Create(
                               Codes.CategoryTooPoor,
                               $"Category \"{category}\" totals {Plural(combinations, "combination")}, but every category needs at least {minimum:N0}.",
                               context => context.Add(Category, category).Add(Counted, combinations).Add(Minimum, minimum))
                          .WithPublicMessage("A category of the theme is too poor in combinations.");
    }

    /// <summary>The defaults ask for participles the theme declares nowhere.</summary>
    /// <param name="requestedMode">The segment mode the defaults asked for.</param>
    public static DomainError ParticiplesRequestedButAbsent(SegmentMode requestedMode) {
        return DomainError.Create(
                               Codes.ParticiplesRequestedButAbsent,
                               $"defaults.segmentMode asks for \"{Spelled(requestedMode)}\", "
                             + "but the theme declares no participle anywhere.",
                               context => context.Add(Section, "defaults.segmentMode"))
                          .WithPublicMessage("The theme asks for participles it does not declare.");
    }

    /// <summary>How a mode is written in a theme file, which is how a message must name it.</summary>
    private static string Spelled(SegmentMode mode) {
        return Spelling.Of(mode);
    }

    private static string Plural(long value, string singular) {
        return value == 1 ? $"1 {singular}" : $"{value:N0} {singular}s";
    }

    #endregion

    #region Nested types

    /// <summary>
    ///     The identifier of every situation, declared once so that a caller branching on one - or a
    ///     test asserting one - references a constant the compiler resolves rather than a string
    ///     nothing validates.
    /// </summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="ThemeErrors.Rejected" />.</summary>
        public static readonly ErrorCode Rejected = ErrorCode.Create("THEME_REJECTED");

        /// <summary>See <see cref="ThemeErrors.NotFound" />, <see cref="ThemeErrors.NotBuiltIn" /> and <see cref="ThemeErrors.NoSuchFile" />.</summary>
        public static readonly ErrorCode NotFound = ErrorCode.Create("THEME_NOT_FOUND");

        /// <summary>See <see cref="ThemeErrors.AlreadyRegistered" />.</summary>
        public static readonly ErrorCode AlreadyRegistered = ErrorCode.Create("THEME_ALREADY_REGISTERED");

        /// <summary>See <see cref="ThemeErrors.NotAFile" />.</summary>
        public static readonly ErrorCode NotAFile = ErrorCode.Create("THEME_NOT_A_FILE");

        /// <summary>See <see cref="ThemeErrors.NoNounToDrawFrom" />.</summary>
        public static readonly ErrorCode NoNounToDrawFrom = ErrorCode.Create("THEME_NO_NOUN");

        /// <summary>See <see cref="ThemeErrors.MalformedJson" />.</summary>
        public static readonly ErrorCode MalformedJson = ErrorCode.Create("THEME_MALFORMED_JSON");

        /// <summary>See <see cref="ThemeErrors.MalformedSection" />.</summary>
        public static readonly ErrorCode MalformedSection = ErrorCode.Create("THEME_MALFORMED_SECTION");

        /// <summary>See <see cref="ThemeErrors.MalformedNoun" />.</summary>
        public static readonly ErrorCode MalformedNoun = ErrorCode.Create("THEME_MALFORMED_NOUN");

        /// <summary>See <see cref="ThemeErrors.UnknownCategory" />.</summary>
        public static readonly ErrorCode UnknownCategory = ErrorCode.Create("THEME_UNKNOWN_CATEGORY");

        /// <summary>See <see cref="ThemeErrors.ExclusionMatchesNothing" />.</summary>
        public static readonly ErrorCode ExclusionMatchesNothing = ErrorCode.Create("THEME_EXCLUSION_MATCHES_NOTHING");

        /// <summary>See <see cref="ThemeErrors.TooFewNouns" />.</summary>
        public static readonly ErrorCode TooFewNouns = ErrorCode.Create("THEME_TOO_FEW_NOUNS");

        /// <summary>See <see cref="ThemeErrors.PoolTooSmall" />.</summary>
        public static readonly ErrorCode PoolTooSmall = ErrorCode.Create("THEME_POOL_TOO_SMALL");

        /// <summary>See <see cref="ThemeErrors.ParticiplePoolTooSmall" />.</summary>
        public static readonly ErrorCode ParticiplePoolTooSmall = ErrorCode.Create("THEME_PARTICIPLE_POOL_TOO_SMALL");

        /// <summary>See <see cref="ThemeErrors.CombinedPoolTooSmall" />.</summary>
        public static readonly ErrorCode CombinedPoolTooSmall = ErrorCode.Create("THEME_COMBINED_POOL_TOO_SMALL");

        /// <summary>See <see cref="ThemeErrors.IncompatibleAdjectiveNotDeclared" />.</summary>
        public static readonly ErrorCode IncompatibleAdjectiveNotDeclared = ErrorCode.Create("THEME_INCOMPATIBLE_ADJECTIVE_ABSENT");

        /// <summary>See <see cref="ThemeErrors.IncompatibleParticipleNotDeclared" />.</summary>
        public static readonly ErrorCode IncompatibleParticipleNotDeclared = ErrorCode.Create("THEME_INCOMPATIBLE_PARTICIPLE_ABSENT");

        /// <summary>See <see cref="ThemeErrors.IncompatibilityStarvesTheNoun" />.</summary>
        public static readonly ErrorCode IncompatibilityStarvesTheNoun = ErrorCode.Create("THEME_INCOMPATIBILITY_STARVES_NOUN");

        /// <summary>See <see cref="ThemeErrors.LongerThanPromised" />.</summary>
        public static readonly ErrorCode LongerThanPromised = ErrorCode.Create("THEME_LONGER_THAN_PROMISED");

        /// <summary>See <see cref="ThemeErrors.NothingFitsTheLimit" />.</summary>
        public static readonly ErrorCode NothingFitsTheLimit = ErrorCode.Create("THEME_NOTHING_FITS_THE_LIMIT");

        /// <summary>See <see cref="ThemeErrors.NoValueIsShortEnough" />.</summary>
        public static readonly ErrorCode NoValueIsShortEnough = ErrorCode.Create("THEME_NO_VALUE_SHORT_ENOUGH");

        /// <summary>See <see cref="ThemeErrors.TheLimitStarvesTheNoun" />.</summary>
        public static readonly ErrorCode TheLimitStarvesTheNoun = ErrorCode.Create("THEME_LIMIT_STARVES_NOUN");

        /// <summary>See <see cref="ThemeErrors.CategoryTooPoor" />.</summary>
        public static readonly ErrorCode CategoryTooPoor = ErrorCode.Create("THEME_CATEGORY_TOO_POOR");

        /// <summary>See <see cref="ThemeErrors.ParticiplesRequestedButAbsent" />.</summary>
        public static readonly ErrorCode ParticiplesRequestedButAbsent = ErrorCode.Create("THEME_PARTICIPLES_ABSENT");

        #endregion

    }

    #endregion

}