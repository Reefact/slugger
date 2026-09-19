using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Slugger.Domain;

namespace Slugger.Core.UnitTests;

/// <summary>
/// Four analyzer packages are referenced by every project, and none of them may cost a
/// consumer anything. This covers the half an assembly can show - that nothing survives
/// compilation. The other half, staying out of the nuspec's dependency graph, is invisible from
/// here and is guarded by the AnalyzersStayPrivate target in Directory.Build.props.
/// </summary>
public sealed class PackageWeightTests
{
    /// <summary>
    /// SuppressMessageAttribute is [Conditional("CODE_ANALYSIS")], and this project does not
    /// define that symbol. The compiler honours the suppression and then writes nothing: a
    /// catalogue-based suppression costs a consumer neither a dependency nor a byte of metadata.
    /// </summary>
    [Fact]
    public void A_catalogue_suppression_leaves_no_attribute_in_metadata()
    {
        // Setup
        ConstructorInfo? seededConstructor = typeof(DefaultRandomSource).GetConstructor([typeof(int?)]);
        Assert.NotNull(seededConstructor);

        // Exercise
        SuppressMessageAttribute[] emitted = seededConstructor
            .GetCustomAttributes<SuppressMessageAttribute>()
            .ToArray();

        // Verify
        Assert.Empty(emitted);
    }
}
