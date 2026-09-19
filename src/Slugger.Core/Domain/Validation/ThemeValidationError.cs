namespace Slugger.Domain.Validation;

/// <summary>
/// One reason a theme was refused, as the facts that make it up rather than as a sentence.
/// </summary>
/// <remarks>
/// <para>
/// The spec asks each refusal to name specifics - the missing category and the known ones, the
/// noun and the size of its pool, the category and its total - and asks --register to produce
/// the same message as a runtime load rather than a second wording. A pre-formatted string in
/// here is what makes those two drift: the sentence gets written wherever the error is raised,
/// and there are then as many wordings as there are call sites. Carrying the facts instead
/// leaves exactly one place that turns them into prose.
/// </para>
/// <para>
/// The hierarchy is closed: the constructor is private protected, so every case is one of the
/// nested records below and a renderer can switch over them exhaustively.
/// </para>
/// </remarks>
public abstract record ThemeValidationError
{
    private protected ThemeValidationError(ThemeValidationErrorCode code) => Code = code;

    /// <summary>A stable identifier for the situation, independent of how it is worded.</summary>
    public ThemeValidationErrorCode Code { get; }

    /// <summary>The file is not JSON. Terminal - no later rule can run on something that did not parse.</summary>
    /// <param name="Detail">What the parser objected to.</param>
    /// <param name="LineNumber">Where, when the parser knows.</param>
    /// <param name="Position">How far into that line, when the parser knows.</param>
    public sealed record MalformedJson(string Detail, long? LineNumber, long? Position)
        : ThemeValidationError(ThemeValidationErrorCode.MalformedJson);

    /// <summary>A required section is missing, or is not the shape the schema calls for.</summary>
    /// <param name="Section">The section at fault, as it is spelled in the file.</param>
    /// <param name="Expected">The shape it had to have.</param>
    public sealed record MalformedSection(string Section, string Expected)
        : ThemeValidationError(ThemeValidationErrorCode.MalformedSection);

    /// <summary>An entry of "nouns" is not an object carrying a non-empty "value".</summary>
    /// <param name="Index">Its position in the array, since it has no name to be called by.</param>
    /// <param name="Detail">What is wrong with it.</param>
    public sealed record MalformedNoun(int Index, string Detail)
        : ThemeValidationError(ThemeValidationErrorCode.MalformedNoun);

    /// <summary>A noun references a category that neither "adjectives" nor "participles" declares.</summary>
    /// <param name="Noun">The noun carrying the unknown category.</param>
    /// <param name="Category">The category that does not exist.</param>
    /// <param name="KnownCategories">Every category the theme declares, so the message can list the alternatives.</param>
    public sealed record UnknownCategory(
        string Noun,
        string Category,
        IReadOnlyList<string> KnownCategories)
        : ThemeValidationError(ThemeValidationErrorCode.UnknownCategory);

    /// <summary>The theme holds fewer distinct nouns than the floor.</summary>
    /// <param name="Count">How many nouns the theme declares.</param>
    /// <param name="Minimum">The floor it had to clear.</param>
    public sealed record TooFewNouns(int Count, int Minimum)
        : ThemeValidationError(ThemeValidationErrorCode.TooFewNouns);

    /// <summary>Some noun resolves to fewer adjectives than the floor.</summary>
    /// <param name="Noun">The noun whose pool is too small - named, because a global count would hide it.</param>
    /// <param name="PoolSize">How many adjectives that noun can reach.</param>
    /// <param name="Minimum">The floor it had to clear.</param>
    public sealed record PoolTooSmall(string Noun, int PoolSize, int Minimum)
        : ThemeValidationError(ThemeValidationErrorCode.PoolTooSmall);

    /// <summary>A whole branch of the theme is poor in combinations, even if each of its nouns clears the pool floor.</summary>
    /// <param name="Category">The category that is too poor.</param>
    /// <param name="Combinations">What its nouns total between them.</param>
    /// <param name="Minimum">The floor it had to clear.</param>
    public sealed record CategoryTooPoor(string Category, long Combinations, long Minimum)
        : ThemeValidationError(ThemeValidationErrorCode.CategoryTooPoor);

    /// <summary>
    /// The defaults ask for participles the theme declares nowhere. An incoherent file, not a
    /// small one, so no override clears it - unlike the size rules.
    /// </summary>
    /// <param name="RequestedMode">The segment mode the defaults asked for.</param>
    public sealed record ParticiplesRequestedButAbsent(SegmentMode RequestedMode)
        : ThemeValidationError(ThemeValidationErrorCode.ParticiplesRequestedButAbsent);
}
