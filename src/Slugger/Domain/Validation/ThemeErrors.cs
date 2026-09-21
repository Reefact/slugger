using System.Diagnostics.CodeAnalysis;
using DiagnosticCatalog.Sonar;
using FirstClassErrors;

namespace Slugger.Domain.Validation;

/// <summary>
/// Every way a theme can be refused, declared once. A factory per situation is what makes the
/// promise of DEC0006 hold: <c>--register</c> and a runtime load produce the same error because
/// they call the same factory, not because two call sites were written to match.
/// </summary>
public static class ThemeErrors
{
    /// <summary>
    /// The identifier of every situation, declared once so that a caller branching on one - or a
    /// test asserting one - references a constant the compiler resolves rather than a string
    /// nothing validates.
    /// </summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes
    {
        /// <summary>See <see cref="ThemeErrors.Rejected"/>.</summary>
        public static readonly ErrorCode Rejected = ErrorCode.Create("THEME_REJECTED");

        /// <summary>See <see cref="ThemeErrors.NotFound"/>.</summary>
        public static readonly ErrorCode NotFound = ErrorCode.Create("THEME_NOT_FOUND");

        /// <summary>See <see cref="ThemeErrors.AlreadyRegistered"/>.</summary>
        public static readonly ErrorCode AlreadyRegistered = ErrorCode.Create("THEME_ALREADY_REGISTERED");

        /// <summary>See <see cref="ThemeErrors.NotAFile"/>.</summary>
        public static readonly ErrorCode NotAFile = ErrorCode.Create("THEME_NOT_A_FILE");

        /// <summary>See <see cref="ThemeErrors.NoNounToDrawFrom"/>.</summary>
        public static readonly ErrorCode NoNounToDrawFrom = ErrorCode.Create("THEME_NO_NOUN");

        /// <summary>See <see cref="ThemeErrors.MalformedJson"/>.</summary>
        public static readonly ErrorCode MalformedJson = ErrorCode.Create("THEME_MALFORMED_JSON");

        /// <summary>See <see cref="ThemeErrors.MalformedSection"/>.</summary>
        public static readonly ErrorCode MalformedSection = ErrorCode.Create("THEME_MALFORMED_SECTION");

        /// <summary>See <see cref="ThemeErrors.MalformedNoun"/>.</summary>
        public static readonly ErrorCode MalformedNoun = ErrorCode.Create("THEME_MALFORMED_NOUN");

        /// <summary>See <see cref="ThemeErrors.UnknownCategory"/>.</summary>
        public static readonly ErrorCode UnknownCategory = ErrorCode.Create("THEME_UNKNOWN_CATEGORY");

        /// <summary>See <see cref="ThemeErrors.ExclusionMatchesNothing"/>.</summary>
        public static readonly ErrorCode ExclusionMatchesNothing = ErrorCode.Create("THEME_EXCLUSION_MATCHES_NOTHING");

        /// <summary>See <see cref="ThemeErrors.TooFewNouns"/>.</summary>
        public static readonly ErrorCode TooFewNouns = ErrorCode.Create("THEME_TOO_FEW_NOUNS");

        /// <summary>See <see cref="ThemeErrors.PoolTooSmall"/>.</summary>
        public static readonly ErrorCode PoolTooSmall = ErrorCode.Create("THEME_POOL_TOO_SMALL");

        /// <summary>See <see cref="ThemeErrors.ParticiplePoolTooSmall"/>.</summary>
        public static readonly ErrorCode ParticiplePoolTooSmall = ErrorCode.Create("THEME_PARTICIPLE_POOL_TOO_SMALL");

        /// <summary>See <see cref="ThemeErrors.CombinedPoolTooSmall"/>.</summary>
        public static readonly ErrorCode CombinedPoolTooSmall = ErrorCode.Create("THEME_COMBINED_POOL_TOO_SMALL");

        /// <summary>See <see cref="ThemeErrors.IncompatibleAdjectiveNotDeclared"/>.</summary>
        public static readonly ErrorCode IncompatibleAdjectiveNotDeclared = ErrorCode.Create("THEME_INCOMPATIBLE_ADJECTIVE_ABSENT");

        /// <summary>See <see cref="ThemeErrors.IncompatibleParticipleNotDeclared"/>.</summary>
        public static readonly ErrorCode IncompatibleParticipleNotDeclared = ErrorCode.Create("THEME_INCOMPATIBLE_PARTICIPLE_ABSENT");

        /// <summary>See <see cref="ThemeErrors.IncompatibilityStarvesTheNoun"/>.</summary>
        public static readonly ErrorCode IncompatibilityStarvesTheNoun = ErrorCode.Create("THEME_INCOMPATIBILITY_STARVES_NOUN");

        /// <summary>See <see cref="ThemeErrors.LongerThanPromised"/>.</summary>
        public static readonly ErrorCode LongerThanPromised = ErrorCode.Create("THEME_LONGER_THAN_PROMISED");

        /// <summary>See <see cref="ThemeErrors.NothingFitsTheLimit"/>.</summary>
        public static readonly ErrorCode NothingFitsTheLimit = ErrorCode.Create("THEME_NOTHING_FITS_THE_LIMIT");

        /// <summary>See <see cref="ThemeErrors.NoValueIsShortEnough"/>.</summary>
        public static readonly ErrorCode NoValueIsShortEnough = ErrorCode.Create("THEME_NO_VALUE_SHORT_ENOUGH");

        /// <summary>See <see cref="ThemeErrors.TheLimitStarvesTheNoun"/>.</summary>
        public static readonly ErrorCode TheLimitStarvesTheNoun = ErrorCode.Create("THEME_LIMIT_STARVES_NOUN");

        /// <summary>See <see cref="ThemeErrors.CategoryTooPoor"/>.</summary>
        public static readonly ErrorCode CategoryTooPoor = ErrorCode.Create("THEME_CATEGORY_TOO_POOR");

        /// <summary>See <see cref="ThemeErrors.ParticiplesRequestedButAbsent"/>.</summary>
        public static readonly ErrorCode ParticiplesRequestedButAbsent = ErrorCode.Create("THEME_PARTICIPLES_ABSENT");
    }
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

    /// <summary>The whole report: one error carrying every reason the theme was refused.</summary>
    /// <param name="themeName">What the theme would have been called.</param>
    /// <param name="reasons">Every reason, not just the first.</param>
    public static DomainError Rejected(string themeName, IEnumerable<DomainError> reasons) =>
        DomainError.Create(
                Codes.Rejected,
                $"Theme \"{themeName}\" was refused",
                reasons,
                context => context.Add(ThemeName, themeName))
            .WithPublicMessage("The theme cannot be used.", "See the reasons it carries.");

    /// <summary>No catalog in scope carries a theme of that name.</summary>
    /// <param name="name">The theme that was asked for.</param>
    /// <param name="available">The themes that are in scope, so the message can list them.</param>
    public static DomainError NotFound(string name, IReadOnlyList<string> available) =>
        DomainError.Create(
                Codes.NotFound,
                available.Count == 0
                    ? $"No theme named \"{name}\", and no theme is available at all."
                    : $"No theme named \"{name}\". Available: {string.Join(", ", available)}.",
                context => context.Add(ThemeName, name).Add(KnownCategories, string.Join(", ", available)))
            .WithPublicMessage("That theme does not exist.");

    /// <summary>A theme of that name is already in the theme directory, and nothing is overwritten by accident.</summary>
    /// <param name="name">The theme that already exists.</param>
    public static DomainError AlreadyRegistered(string name) =>
        DomainError.Create(
                Codes.AlreadyRegistered,
                $"A theme \"{name}\" already exists in the theme directory; unregister it first to replace it.",
                context => context.Add(ThemeName, name))
            .WithPublicMessage("That theme is already registered.");

    /// <summary>Only a file can be unregistered; a built-in theme is left out of --theme instead.</summary>
    /// <param name="name">The theme that has no file to remove.</param>
    public static DomainError NotAFile(string name) =>
        DomainError.Create(
                Codes.NotAFile,
                $"\"{name}\" is embedded in the binary, so there is nothing to unregister - leave it out of --theme not to use it.",
                context => context.Add(ThemeName, name))
            .WithPublicMessage("That theme is built in and cannot be unregistered.");

    /// <summary>The file is not JSON. Terminal: no later rule can run on something that did not parse.</summary>
    /// <param name="detail">What the parser objected to.</param>
    /// <param name="lineNumber">Where, when the parser knows.</param>
    public static DomainError MalformedJson(string detail, long? lineNumber)
    {
        long? line = lineNumber + 1;

        return DomainError.Create(
                Codes.MalformedJson,
                line is { } at
                    ? $"The file is not valid JSON at line {at}: {detail}"
                    : $"The file is not valid JSON: {detail}",
                context =>
                {
                    if (line is { } known)
                    {
                        context.Add(Counted, known);
                    }
                })
            .WithPublicMessage("The theme file is not valid JSON.");
    }

    /// <summary>A section is missing, or is not the shape the schema calls for.</summary>
    /// <param name="section">The section at fault, as it is spelled in the file.</param>
    /// <param name="expected">The shape it had to have.</param>
    public static DomainError MalformedSection(string section, string expected) =>
        DomainError.Create(
                Codes.MalformedSection,
                $"\"{section}\" must be {expected}.",
                context => context.Add(Section, section))
            .WithPublicMessage("A section of the theme file has the wrong shape.");

    /// <summary>An entry of "nouns" is not an object carrying a non-empty "value".</summary>
    /// <param name="index">Its position in the array, since it has no name to be called by.</param>
    /// <param name="detail">What is wrong with it.</param>
    public static DomainError MalformedNoun(int index, string detail) =>
        DomainError.Create(
                Codes.MalformedNoun,
                $"nouns[{index}]: {detail}.",
                context => context.Add(Section, $"nouns[{index}]").Add(Counted, index))
            .WithPublicMessage("An entry of \"nouns\" is malformed.");

    /// <summary>
    /// A noun excludes a word the theme declares nowhere. Refused rather than ignored: an
    /// exclusion that matches nothing fails open, so the theme reads as protected and is not,
    /// and a typo would be the likeliest cause.
    /// </summary>
    /// <param name="noun">The noun carrying the exclusion.</param>
    /// <param name="word">The word that matches nothing.</param>
    public static DomainError ExclusionMatchesNothing(string noun, string word) =>
        DomainError.Create(
                Codes.ExclusionMatchesNothing,
                $"\"{noun}\" excludes \"{word}\", which the theme declares nowhere.",
                context => context.Add(Noun, noun).Add(Category, word))
            .WithPublicMessage("A noun excludes a word the theme does not declare.");

    /// <summary>
    /// An "incompatible" key names a word the theme declares nowhere in "adjectives". Refused
    /// for the reason <see cref="ExclusionMatchesNothing"/> gives: a pair that matches nothing
    /// fails open, so the theme reads as protected and is not.
    /// </summary>
    /// <param name="word">The key that matches no adjective.</param>
    /// <param name="declaredAsAParticiple">
    /// Whether the theme declares it in "participles", which makes a reversed pair by far the
    /// likeliest cause - and is worth saying rather than leaving the author to find.
    /// </param>
    public static DomainError IncompatibleAdjectiveNotDeclared(string word, bool declaredAsAParticiple) =>
        DomainError.Create(
                Codes.IncompatibleAdjectiveNotDeclared,
                $"incompatible names \"{word}\" as an adjective, which the theme declares nowhere in \"adjectives\"."
                + (declaredAsAParticiple
                    ? " It is declared as a participle, so the pair may be the wrong way round: the key refuses, the words are refused."
                    : string.Empty),
                context => context.Add(Category, word).Add(Section, "incompatible"))
            .WithPublicMessage("An incompatibility names an adjective the theme does not declare.");

    /// <summary>An "incompatible" entry refuses a word the theme declares nowhere in "participles".</summary>
    /// <param name="adjective">The adjective carrying the refusal.</param>
    /// <param name="word">The word that matches no participle.</param>
    /// <param name="declaredAsAnAdjective">Whether it is an adjective, which again suggests a reversed pair.</param>
    public static DomainError IncompatibleParticipleNotDeclared(string adjective, string word, bool declaredAsAnAdjective) =>
        DomainError.Create(
                Codes.IncompatibleParticipleNotDeclared,
                $"incompatible[\"{adjective}\"] refuses \"{word}\", which the theme declares nowhere in \"participles\"."
                + (declaredAsAnAdjective
                    ? " It is declared as an adjective, so the pair may be the wrong way round: the key refuses, the words are refused."
                    : string.Empty),
                context => context.Add(Noun, adjective).Add(Category, word).Add(Section, "incompatible"))
            .WithPublicMessage("An incompatibility refuses a participle the theme does not declare.");

    /// <summary>
    /// An incompatibility takes a noun under the participle floor for one of the adjectives it
    /// can draw. Reported against the worst adjective of that noun rather than every offending
    /// one: a theme with a dozen pairs would otherwise report a dozen lines per noun, and the
    /// worst is the one that says how far there is to go.
    /// </summary>
    /// <param name="noun">The noun left short.</param>
    /// <param name="adjective">The adjective it is left short beside.</param>
    /// <param name="poolSize">What it still reaches with that adjective in front of it.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError IncompatibilityStarvesTheNoun(string noun, string adjective, int poolSize, int minimum) =>
        DomainError.Create(
                Codes.IncompatibilityStarvesTheNoun,
                $"\"{noun}\" reaches {Plural(poolSize, "participle")} beside \"{adjective}\", but a theme drawing "
                + $"\"both\" needs at least {minimum:N0} per noun for every adjective it can draw - "
                + "either declare more participles for it, or drop the incompatibility.",
                context => context
                    .Add(Noun, noun)
                    .Add(Category, adjective)
                    .Add(Counted, poolSize)
                    .Add(Minimum, minimum)
                    .Add(Mode, Spelled(SegmentMode.Both)))
            .WithPublicMessage("An incompatibility leaves a noun too few participles.");

    /// <summary>
    /// The theme can produce a slug longer than its own "maxLength" says (DEC0018). Refused
    /// rather than trimmed at the draw: the promise is the theme's, so an unkeepable one is a
    /// fact about the file, and the word that broke it is named so it can be shortened or dropped.
    /// </summary>
    /// <param name="shape">The key that carries the promise, as it is spelled in the file.</param>
    /// <param name="longest">The longest slug the theme can actually produce in that shape.</param>
    /// <param name="promised">The ceiling the theme declared.</param>
    public static DomainError LongerThanPromised(string shape, string longest, int promised) =>
        DomainError.Create(
                Codes.LongerThanPromised,
                $"maxLength.{shape} promises {promised:N0} characters, but the theme can produce "
                + $"\"{longest}\" at {longest.Length:N0}.",
                context => context
                    .Add(Section, $"maxLength.{shape}")
                    .Add(Counted, longest.Length)
                    .Add(Minimum, promised))
            .WithPublicMessage("The theme can produce a slug longer than it promises.");

    /// <summary>
    /// A length budget takes a noun under the participle floor for one of the adjectives it can
    /// draw (DEC0018). Its own factory rather than the incompatibility one, because the fix is
    /// not the same: nothing here is refused by a pair, the words simply no longer fit together.
    /// </summary>
    /// <param name="noun">The noun left short.</param>
    /// <param name="adjective">The adjective that leaves it least room.</param>
    /// <param name="poolSize">What still fits behind that adjective.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    /// <param name="maxLength">The ceiling that left it no room.</param>
    public static DomainError TheLimitStarvesTheNoun(
        string noun,
        string adjective,
        int poolSize,
        int minimum,
        int maxLength) =>
        DomainError.Create(
                Codes.TheLimitStarvesTheNoun,
                $"Under {maxLength:N0} characters, \"{noun}\" reaches {Plural(poolSize, "participle")} behind "
                + $"\"{adjective}\", but a theme drawing \"both\" needs at least {minimum:N0} per noun for every "
                + "adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.",
                context => context
                    .Add(Noun, noun)
                    .Add(Category, adjective)
                    .Add(Counted, poolSize)
                    .Add(Minimum, minimum)
                    .Add(Mode, Spelled(SegmentMode.Both)))
            .WithPublicMessage("The length asked for leaves a noun too few participles.");

    /// <summary>A noun references a category that neither "adjectives" nor "participles" declares.</summary>
    /// <param name="noun">The noun carrying the unknown category.</param>
    /// <param name="category">The category that does not exist.</param>
    /// <param name="knownCategories">Every category the theme declares.</param>
    public static DomainError UnknownCategory(string noun, string category, IReadOnlyList<string> knownCategories) =>
        DomainError.Create(
                Codes.UnknownCategory,
                $"\"{noun}\" references category \"{category}\", which the theme does not declare "
                + $"(it declares {string.Join(", ", knownCategories)}).",
                context => context
                    .Add(Noun, noun)
                    .Add(Category, category)
                    .Add(KnownCategories, string.Join(", ", knownCategories)))
            .WithPublicMessage("A noun references a category the theme does not declare.");

    /// <summary>
    /// A theme with no noun at all cannot produce a single slug. Unlike the size floors, this is
    /// never waived by <c>allowSmall</c>: that flag lets an author accept a small theme, not an
    /// empty one, and the difference is the difference between a judgement call and a file that
    /// cannot work.
    /// </summary>
    /// <param name="themeName">The theme holding nothing to draw.</param>
    public static DomainError NoNounToDrawFrom(string themeName) =>
        DomainError.Create(
                Codes.NoNounToDrawFrom,
                $"Theme \"{themeName}\" holds no noun, so it cannot produce a slug.",
                context => context.Add(ThemeName, themeName))
            .WithPublicMessage("That theme holds no noun.");

    /// <summary>
    /// A length budget left room for no noun at all, so the theme can produce nothing under it
    /// (DEC0018). Known before the first draw rather than discovered by one, and never waived:
    /// a run that can produce nothing is not a small run.
    /// </summary>
    /// <param name="themeName">The theme nothing fits in.</param>
    /// <param name="maxLength">The ceiling that left no room.</param>
    public static DomainError NothingFitsTheLimit(string themeName, int maxLength) =>
        DomainError.Create(
                Codes.NothingFitsTheLimit,
                $"No slug of theme \"{themeName}\" fits in {maxLength:N0} characters.",
                context => context.Add(ThemeName, themeName).Add(Minimum, maxLength))
            .WithPublicMessage("No slug of that theme fits the length asked for.");

    /// <summary>
    /// A word cap left the theme no noun to draw on at all: every one of them is written in
    /// more words than the run allows (DEC0023).
    /// </summary>
    /// <param name="themeName">The theme the run asked for.</param>
    /// <param name="maxSegmentWords">The cap the run set.</param>
    public static DomainError NoValueIsShortEnough(string themeName, int maxSegmentWords) =>
        DomainError.Create(
                Codes.NoValueIsShortEnough,
                $"No noun of theme \"{themeName}\" is written in {Plural(maxSegmentWords, "word")} or fewer.",
                context => context.Add(ThemeName, themeName).Add(Minimum, maxSegmentWords))
            .WithPublicMessage("No noun of that theme is short enough in words for the limit asked for.");

    /// <summary>The theme holds fewer distinct nouns than the floor.</summary>
    /// <param name="count">How many nouns the theme declares.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError TooFewNouns(int count, int minimum) =>
        DomainError.Create(
                Codes.TooFewNouns,
                $"{Plural(count, "noun")}, but a theme needs at least {minimum:N0}.",
                context => context.Add(Counted, count).Add(Minimum, minimum))
            .WithPublicMessage("The theme holds too few nouns.");

    /// <summary>
    /// A noun reaches too few participles, in a theme whose segment mode draws them. The mode
    /// is carried rather than implied: it is what chose the floor, and an author asking why
    /// the number is 20 here and 100 there has the answer in the sentence.
    /// </summary>
    /// <param name="noun">The noun whose participle pool is too thin.</param>
    /// <param name="poolSize">What it actually reaches.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    /// <param name="mode">The segment mode that set that floor.</param>
    public static DomainError ParticiplePoolTooSmall(string noun, int poolSize, int minimum, SegmentMode mode) =>
        DomainError.Create(
                Codes.ParticiplePoolTooSmall,
                $"\"{noun}\" reaches {Plural(poolSize, "participle")}, but a theme drawing "
                + $"\"{Spelled(mode)}\" needs at least {minimum:N0} per noun.",
                context => context
                    .Add(Noun, noun)
                    .Add(Counted, poolSize)
                    .Add(Minimum, minimum)
                    .Add(Mode, Spelled(mode)))
            .WithPublicMessage("A noun reaches too few participles.");

    /// <summary>
    /// A noun reaches too few words of any kind, in a theme drawing "either". That mode puts one
    /// word in front of the noun and draws it from the two pools at once, so neither pool has a
    /// floor of its own and the sum carries the whole one.
    /// </summary>
    /// <param name="noun">The noun whose two pools are too thin between them.</param>
    /// <param name="adjectives">What it reaches in "adjectives".</param>
    /// <param name="participles">What it reaches in "participles".</param>
    /// <param name="minimum">The floor the two had to clear together.</param>
    public static DomainError CombinedPoolTooSmall(string noun, int adjectives, int participles, int minimum) =>
        DomainError.Create(
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

    /// <summary>Some noun resolves to fewer adjectives than the floor.</summary>
    /// <param name="noun">The noun whose pool is too small - named, because a global count would hide it.</param>
    /// <param name="poolSize">How many adjectives that noun can reach.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError PoolTooSmall(string noun, int poolSize, int minimum) =>
        DomainError.Create(
                Codes.PoolTooSmall,
                $"\"{noun}\" reaches {Plural(poolSize, "adjective")}, but every noun needs at least {minimum:N0}.",
                context => context.Add(Noun, noun).Add(Counted, poolSize).Add(Minimum, minimum))
            .WithPublicMessage("A noun reaches too few adjectives.");

    /// <summary>A whole branch of the theme is poor in combinations, even if each of its nouns clears the pool floor.</summary>
    /// <param name="category">The category that is too poor.</param>
    /// <param name="combinations">What its nouns total between them.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError CategoryTooPoor(string category, long combinations, long minimum) =>
        DomainError.Create(
                Codes.CategoryTooPoor,
                $"Category \"{category}\" totals {combinations:N0} combinations, but every category needs at least {minimum:N0}.",
                context => context.Add(Category, category).Add(Counted, combinations).Add(Minimum, minimum))
            .WithPublicMessage("A category of the theme is too poor in combinations.");

    /// <summary>The defaults ask for participles the theme declares nowhere.</summary>
    /// <param name="requestedMode">The segment mode the defaults asked for.</param>
    public static DomainError ParticiplesRequestedButAbsent(SegmentMode requestedMode) =>
        DomainError.Create(
                Codes.ParticiplesRequestedButAbsent,
                $"defaults.segmentMode asks for \"{Spelled(requestedMode)}\", "
                + "but the theme declares no participle anywhere.",
                context => context.Add(Section, "defaults.segmentMode"))
            .WithPublicMessage("The theme asks for participles it does not declare.");

    /// <summary>How a mode is written in a theme file, which is how a message must name it.</summary>
    private static string Spelled(SegmentMode mode) => Spelling.Of(mode);

    private static string Plural(long value, string singular) =>
        value == 1 ? $"1 {singular}" : $"{value:N0} {singular}s";
}
