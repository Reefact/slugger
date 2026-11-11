#region Usings declarations

using System.Reflection;

using Slugger.Domain;
using Slugger.Errors;

#endregion

namespace Slugger.UnitTests;

/// <summary>
///     What a consumer meets when a theme is refused (DEC0035): an error and an exception named after the
///     theme it holds, codes it can branch on, and nothing it could build an error with.
/// </summary>
public sealed class RefusedThemeTests {

    #region Static members

    private const string Unusable = """{ "adjectives": {}, "nouns": "moon" }""";

    #endregion

    [Fact]
    public void A_refused_load_reports_a_theme_error_whose_every_reason_is_one_too() {
        // Exercise
        Outcome<SluggerTheme> outcome = SluggerTheme.LoadFromJson(Unusable, Dummies.AnyThemeNameOtherThanTheBuiltInOnes());

        // Verify
        SluggerThemeError refusal = Assert.IsType<SluggerThemeError>(outcome.Error);
        Assert.Equal(SluggerThemeError.Codes.Rejected, refusal.Code);
        Assert.All(refusal.InnerErrors, reason => Assert.IsType<SluggerThemeError>(reason));
    }

    [Fact]
    public void A_refused_load_raises_the_theme_exception() {
        // Exercise
        SluggerThemeException raised = Assert.Throws<SluggerThemeException>(() => SluggerTheme.LoadFromJsonOrThrow(Unusable));

        // Verify
        Assert.Equal(SluggerThemeError.Codes.Rejected, raised.Error.Code);
    }

    [Fact]
    public void Only_the_library_builds_one() {
        // Exercise
        MethodInfo[] factories = [
            .. typeof(SluggerThemeError).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        ];
        ConstructorInfo[] constructors = typeof(SluggerThemeError).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Verify
        Assert.Empty(factories);
        Assert.Empty(constructors);
    }

    /// <summary>
    ///     Literal on purpose: these three belong to the command's theme directory, which no library
    ///     method reaches, so a code a consumer can never receive is not promised to them.
    /// </summary>
    [Fact]
    public void Publishes_no_code_only_the_command_can_raise() {
        // Exercise
        string[] published = [
            .. typeof(SluggerThemeError.Codes).GetFields(BindingFlags.Public | BindingFlags.Static)
                                              .Select(field => field.Name)
        ];

        // Verify
        Assert.DoesNotContain("AlreadyRegistered", published);
        Assert.DoesNotContain("NotSelectable", published);
        Assert.DoesNotContain("NotAFile", published);
    }

}
