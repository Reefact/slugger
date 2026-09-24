#region Usings declarations

using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Slugger.ArchitectureTests;

/// <summary>
///     Where a value object is allowed to give up what it holds. Everywhere inside the domain a
///     type answers questions - <c>chance.Covers(roll)</c>, <c>length.Reaches(written)</c> - and
///     <c>Dehydrate</c> is the one door out, for whoever is leaving.
/// </summary>
/// <remarks>
///     Read from the compiled assembly rather than by reflection, because what is being checked is
///     a call and not a signature: reflection sees that a method exists, Cecil sees who calls it.
/// </remarks>
public sealed class DehydrationTests {

    #region Static members

    private const string Dehydrate = nameof(Dehydrate);
    private const string DomainNamespace = "Slugger.Domain";

    private static IEnumerable<MethodDefinition> DomainMethods() {
        using AssemblyDefinition engine = AssemblyDefinition.ReadAssembly(typeof(Themes).Assembly.Location);

        foreach (TypeDefinition type in engine.MainModule.Types.Where(InTheDomain)) {
            foreach (MethodDefinition method in type.Methods.Where(method => method.HasBody)) {
                yield return method;
            }
        }
    }

    private static bool InTheDomain(TypeDefinition type) {
        return type.Namespace.StartsWith(DomainNamespace, StringComparison.Ordinal);
    }

    private static bool CallsDehydration(Instruction instruction) {
        if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt) { return false; }

        return instruction.Operand is MethodReference called && called.Name == Dehydrate;
    }

    #endregion

    /// <summary>
    ///     A composite dehydrates by dehydrating its parts, which is the one call the rule allows.
    ///     Anything else in the domain reaching for a primitive has stopped asking the type and
    ///     started reading it.
    /// </summary>
    [Fact]
    public void Nothing_in_the_domain_dehydrates_except_a_dehydration() {
        // Exercise
        string[] leaks = [
            .. DomainMethods()
              .Where(method => method.Name != Dehydrate)
              .Where(method => method.Body.Instructions.Any(CallsDehydration))
              .Select(method => $"{method.DeclaringType.Name}.{method.Name}")
              .Distinct(StringComparer.Ordinal)
              .Order(StringComparer.Ordinal)
        ];

        // Verify
        Assert.Empty(leaks);
    }

    /// <summary>
    ///     The guard the rule above needs: a marker nobody applied would make it green for the
    ///     wrong reason, and so would a domain nobody found.
    /// </summary>
    [Fact]
    public void The_domain_declares_dehydrations_to_measure() {
        // Verify
        Assert.Contains(DomainMethods(), method => method.Name == Dehydrate);
    }

}
