namespace Slugger.Domain.Tests;

/// <summary>
/// The dependency rule is enforced by the compiler: Slugger.Domain.csproj has no
/// ProjectReference at all. This test is what keeps that true after someone edits the
/// csproj to make one import "just work".
/// </summary>
public class DependencyRuleTests
{
    [Fact]
    public void The_domain_references_no_other_slugger_assembly()
    {
        var sluggerReferences = typeof(Theme).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("Slugger", StringComparison.Ordinal))
            .ToArray();

        sluggerReferences.ShouldBeEmpty();
    }

    [Fact]
    public void The_domain_references_no_nuget_package()
    {
        var externalReferences = typeof(Theme).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && !name.StartsWith("System", StringComparison.Ordinal))
            .ToArray();

        externalReferences.ShouldBeEmpty();
    }
}
