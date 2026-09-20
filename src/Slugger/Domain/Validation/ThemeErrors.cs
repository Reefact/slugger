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
    /// A noun reaches too few participles, in a theme that declares some. Its own floor rather
    /// than the adjective one: a participle is drawn into the slug like an adjective, but a
    /// theme may hold none at all, so the rule only applies where the theme has opted in.
    /// </summary>
    /// <param name="noun">The noun whose participle pool is too thin.</param>
    /// <param name="poolSize">What it actually reaches.</param>
    /// <param name="minimum">The floor it had to clear.</param>
    public static DomainError ParticiplePoolTooSmall(string noun, int poolSize, int minimum) =>
        DomainError.Create(
                Codes.ParticiplePoolTooSmall,
                $"\"{noun}\" reaches {Plural(poolSize, "participle")}, but every noun needs at least {minimum:N0} "
                + "in a theme that declares participles.",
                context => context.Add(Noun, noun).Add(Counted, poolSize).Add(Minimum, minimum))
            .WithPublicMessage("A noun reaches too few participles.");

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
                $"defaults.segmentMode asks for \"{requestedMode.ToString().ToLowerInvariant()}\", "
                + "but the theme declares no participle anywhere.",
                context => context.Add(Section, "defaults.segmentMode"))
            .WithPublicMessage("The theme asks for participles it does not declare.");

    private static string Plural(long value, string singular) =>
        value == 1 ? $"1 {singular}" : $"{value:N0} {singular}s";
}
