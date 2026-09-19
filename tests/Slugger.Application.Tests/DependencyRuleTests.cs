using Slugger.Application.Abstractions;

namespace Slugger.Application.Tests;

public class DependencyRuleTests
{
    [Fact]
    public void The_application_layer_knows_the_domain_and_no_other_slugger_assembly()
    {
        var sluggerReferences = typeof(IThemeCatalog).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("Slugger", StringComparison.Ordinal))
            .ToArray();

        sluggerReferences.ShouldBe(["Slugger.Domain"]);
    }
}
