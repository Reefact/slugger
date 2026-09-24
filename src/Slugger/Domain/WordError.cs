#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using DiagnosticCatalog.Sonar;

using FirstClassErrors;

#endregion

namespace Slugger.Domain;

/// <summary>
///     Every way a value can fail to be one word, declared once - grouped on the error itself
///     rather than beside it, so that the situations, their codes, their documentation and the
///     exception they raise are one thing to find and one thing to keep in step.
/// </summary>
/// <remarks>
///     Deriving from <see cref="Error" /> rather than <c>DomainError</c> is not a choice:
///     <c>DomainError</c> keeps every constructor internal, so nothing outside FirstClassErrors can
///     extend it. <see cref="Error" /> is what allows <see cref="ToException" /> to be overridden,
///     which is what lets a caller catch a <see cref="WordException" /> by its own name.
/// </remarks>
[ProvidesErrorsFor(
    "Word",
    Description = "Reading a value as a word: the smallest unit of the vocabulary, letters and digits only.")]
public sealed class WordError : Error {

    #region Static members

    /// <summary>The value a refusal is about.</summary>
    public static readonly ErrorContextKey<string> Value = ErrorContextKey.Create<string>("Value", "The value that was read as a word.");

    /// <summary>The character that made the value more than one word.</summary>
    public static readonly ErrorContextKey<string> Boundary = ErrorContextKey.Create<string>("Boundary", "The word boundary found inside the value.");

    /// <summary>Nothing was written where a word was expected - empty, or whitespace alone.</summary>
    /// <param name="value">What was read, as written, so that a report can point at the line.</param>
    [DocumentedBy(nameof(DescribeEmpty))]
    public static WordError Empty(string value) {
        return new WordError(
            Codes.Empty,
            "A word cannot be empty.",
            "A word is missing.",
            "A word carries at least one letter or digit.",
            context => context.Add(Value, value));
    }

    /// <summary>
    ///     The value holds a word boundary, so it spells more than one word. Refused rather than
    ///     reduced: handing back one word out of "jack o'neil" would drop two in silence.
    /// </summary>
    /// <param name="value">What was read.</param>
    /// <param name="boundary">The first character that is neither a letter nor a digit.</param>
    [DocumentedBy(nameof(DescribeNotOneWord))]
    public static WordError NotOneWord(string value, char boundary) {
        return new WordError(
            Codes.NotOneWord,
            $"\"{value}\" is not one word: {Name(boundary)} is a word boundary.",
            "A word carries something that is neither a letter nor a digit.",
            "Spell it as several words, or let the theme loader reduce the boundary first.",
            context => context.Add(Value, value).Add(Boundary, boundary.ToString()));
    }

    /// <summary>
    ///     How to write a boundary into a message. A space quoted between apostrophes shows nothing,
    ///     and a tab less than that, so an invisible character is named by its code point instead.
    /// </summary>
    private static string Name(char boundary) {
        if (char.IsWhiteSpace(boundary) || char.IsControl(boundary)) { return $"U+{(int)boundary:X4}"; }

        return $"'{boundary}'";
    }

    private static ErrorDocumentation DescribeEmpty() {
        return DescribeError
              .WithTitle("A word cannot be empty")
              .WithDescription(
                   "A word is the smallest unit of the vocabulary, and it carries at least one letter or "
                 + "digit. An empty value spells no word at all, so there is nothing to draw and nothing to "
                 + "put in a slug.")
              .WithRule("A word holds one or more characters, each a letter or a digit.")
              .WithDiagnostic(
                   "A theme file declares an entry that spells no word at all - whitespace alone, or a "
                 + "value written entirely of punctuation such as \"!!!\", which keeps no letter once "
                 + "DEC0008 has run.",
                   ErrorOrigin.External,
                   "Read the entry as it stands in the theme file rather than as it reaches the domain.")
              .AndDiagnostic(
                   "A caller split a compound value and handed over the empty piece that two adjacent "
                 + "boundaries left behind.",
                   ErrorOrigin.Internal,
                   "Check that the split drops empty pieces rather than passing them on.")
              .WithExamples(() => Empty("   "));
    }

    private static ErrorDocumentation DescribeNotOneWord() {
        return DescribeError
              .WithTitle("A value spells more than one word")
              .WithDescription(
                   "Everything that is neither a letter nor a digit is a word boundary (DEC0008), so a "
                 + "value carrying one spells several words. It is refused rather than reduced, because "
                 + "returning one word out of \"jack o'neil\" would drop two without saying so.")
              .WithRule("A word holds letters and digits only. An accented letter is a letter.")
              .WithDiagnostic(
                   "A theme file spells a compound entry - \"rock crystal\", \"jack o'neil\" - where one "
                 + "word was expected.",
                   ErrorOrigin.External,
                   "Read the boundary the context carries: it names the character at fault.")
              .AndDiagnostic(
                   "A caller built a word from a raw value without letting the theme loader canonicalize "
                 + "it first, so the boundaries are still written as they were.",
                   ErrorOrigin.Internal,
                   "Canonicalize the value, then split it on its spaces before making a word of each piece.")
              .WithExamples(() => NotOneWord("jack o'neil", '\''));
    }

    #endregion

    #region Constructors & Destructor

    private WordError(ErrorCode                   code,
                      string                      diagnosticMessage,
                      string                      shortMessage,
                      string                      detailedMessage,
                      Action<ErrorContextBuilder> configureContext)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    #endregion

    /// <summary>Raised as this concept's own exception, so a caller can catch it by name.</summary>
    public override DiagnosableException ToException() {
        return new WordException(this);
    }

    /// <summary>The codes the factories above produce, so a caller can match on one.</summary>
    [SuppressMessage(
        SonarRule.S3218.Category,
        SonarRule.S3218.Id,
        Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
    public static class Codes {

        #region Static members

        /// <summary>See <see cref="WordError.Empty" />.</summary>
        public static readonly ErrorCode Empty = ErrorCode.Create("WORD_EMPTY");

        /// <summary>See <see cref="WordError.NotOneWord" />.</summary>
        public static readonly ErrorCode NotOneWord = ErrorCode.Create("WORD_NOT_ONE_WORD");

        #endregion

    }

}
