#region Usings declarations

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

#endregion

namespace Slugger.ArchitectureTests;

/// <summary>
///     The same rule <see cref="NamespaceDependencyTests" /> states, read one layer deeper.
/// </summary>
/// <remarks>
///     That test walks signatures: what a type names in its own declaration. This one reads the
///     compiled assembly through Mono.Cecil, so it also sees what a method body reaches for - a
///     Domain method calling into Infrastructure without naming it anywhere is invisible to a
///     signature walk and caught here. The two are kept side by side rather than merged: the
///     signature one says what the layer publishes, this one says what it touches.
/// </remarks>
public sealed class LayeringTests {

    #region Static members

    private static readonly Architecture Engine = new ArchLoader().LoadAssembly(typeof(Themes).Assembly).Build();

    private static void Holds(TypesShouldConjunction rule) {
        string[] violations = rule.Evaluate(Engine)
                                  .Where(result => !result.Passed)
                                  .Select(result => result.Description)
                                  .ToArray();

        Assert.Empty(violations);
    }

    #endregion

    /// <summary>Domain is the innermost ring: it reaches for nothing further out, not even inside a method.</summary>
    [Fact]
    public void Domain_touches_nothing_of_Application_or_Infrastructure() {
        // Verify
        Holds(
            Types().That().ResideInNamespaceMatching(@"Slugger\.Domain(\..*)?")
                   .Should().NotDependOnAny(
                        Types().That().ResideInNamespaceMatching(@"Slugger\.(Application|Infrastructure)(\..*)?")));
    }

    /// <summary>Application sits between the two, so Infrastructure is out of its reach as well.</summary>
    [Fact]
    public void Application_touches_nothing_of_Infrastructure() {
        // Verify
        Holds(
            Types().That().ResideInNamespaceMatching(@"Slugger\.Application(\..*)?")
                   .Should().NotDependOnAny(
                        Types().That().ResideInNamespaceMatching(@"Slugger\.Infrastructure(\..*)?")));
    }

}
