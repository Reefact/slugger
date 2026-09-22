namespace Slugger.UnitTests;

/// <summary>
///     The shapes of value this suite treats as arbitrary. A theme word is lowercase letters and
///     nothing else, which is what normalization has already guaranteed by the time anything
///     downstream sees one - so the constraint states a domain invariant rather than what a test
///     happens to assert.
/// </summary>
internal static class Dummies {

    private const string LowercaseLetters = "abcdefghijklmnopqrstuvwxyz";

    #region Static members

    internal static string AnyWord() {
        return Any.String()
                  .WithChars(LowercaseLetters)
                  .WithLengthBetween(3, 10)
                  .Generate();
    }

    internal static string AnyThemeNameOtherThanTheBuiltInOnes() {
        return Any.String()
                  .WithChars(LowercaseLetters)
                  .WithLengthBetween(5, 12)
                  .Except("slugger", "heroku", "docker")
                  .Generate();
    }

    #endregion

}