#region Usings declarations

using System.Reflection;
using System.Runtime.CompilerServices;

#endregion

namespace Slugger.ArchitectureTests;

/// <summary>
///     Slugger.Core is one assembly, so the layering is a namespace convention and the compiler
///     no longer refuses to build a violation. This is what replaces that guarantee: dependencies
///     still point inwards only, Domain to Application to Infrastructure.
/// </summary>
/// <remarks>
///     It reads type signatures - base types, interfaces, fields, properties, parameters, return
///     types - which is where a leak that matters shows up: a domain type that has to hold, accept
///     or return something from an outer ring. A method body calling an outer type without naming
///     it in its signature is not caught, and catching that would mean reading IL. The cheap check
///     is worth more than the thorough one nobody keeps.
/// </remarks>
public sealed class NamespaceDependencyTests {

    private const string Domain         = "Slugger.Domain";
    private const string Application    = "Slugger.Application";
    private const string Infrastructure = "Slugger.Infrastructure";

    #region Static members

    public static TheoryData<string, string[]> Layers => new() {
        { Domain, [Application, Infrastructure] },
        { Application, [Infrastructure] }
    };

    private static Type[] TypesInNamespace(string namespaceName) {
        return typeof(Themes).Assembly
                             .GetTypes()
                             .Where(type => type.GetCustomAttribute<CompilerGeneratedAttribute>() is null)
                             .Where(type => IsInNamespace(type, namespaceName))
                             .ToArray();
    }

    private static bool IsInNamespace(Type type, string namespaceName) {
        return type.Namespace is not null
            && (type.Namespace.Equals(namespaceName, StringComparison.Ordinal)
             || type.Namespace.StartsWith($"{namespaceName}.", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> SignatureTypes(Type type) {
        const BindingFlags Declared = BindingFlags.Public
                                    | BindingFlags.NonPublic
                                    | BindingFlags.Instance
                                    | BindingFlags.Static
                                    | BindingFlags.DeclaredOnly;

        if (type.BaseType is not null) {
            foreach (Type unwrapped in Unwrap(type.BaseType)) { yield return unwrapped; }
        }

        foreach (Type contract in type.GetInterfaces()) {
            foreach (Type unwrapped in Unwrap(contract)) { yield return unwrapped; }
        }

        foreach (FieldInfo field in type.GetFields(Declared)) {
            foreach (Type unwrapped in Unwrap(field.FieldType)) { yield return unwrapped; }
        }

        foreach (PropertyInfo property in type.GetProperties(Declared)) {
            foreach (Type unwrapped in Unwrap(property.PropertyType)) { yield return unwrapped; }
        }

        foreach (MethodBase method in type.GetMethods(Declared).Cast<MethodBase>().Concat(type.GetConstructors(Declared))) {
            if (method is MethodInfo { ReturnType: { } returnType }) {
                foreach (Type unwrapped in Unwrap(returnType)) { yield return unwrapped; }
            }

            foreach (ParameterInfo parameter in method.GetParameters()) {
                foreach (Type unwrapped in Unwrap(parameter.ParameterType)) { yield return unwrapped; }
            }
        }
    }

    /// <summary>Peels arrays, by-ref and generic arguments, so IReadOnlyList&lt;NounEntry&gt; also reports NounEntry.</summary>
    private static IEnumerable<Type> Unwrap(Type type) {
        yield return type;

        if (type.HasElementType && type.GetElementType() is { } element) {
            foreach (Type unwrapped in Unwrap(element)) { yield return unwrapped; }
        }

        if (type.IsGenericType) {
            foreach (Type argument in type.GetGenericArguments()) {
                foreach (Type unwrapped in Unwrap(argument)) { yield return unwrapped; }
            }
        }
    }

    #endregion

    [Theory]
    [MemberData(nameof(Layers))]
    public void A_layer_never_names_a_type_from_a_ring_further_out(string layer, string[] forbiddenLayers) {
        // Setup
        Type[] layerTypes = TypesInNamespace(layer);
        Assert.NotEmpty(layerTypes);

        // Exercise
        string[] leaks = layerTypes
                        .SelectMany(type => SignatureTypes(type).Select(referenced => (type, referenced)))
                        .Where(pair => forbiddenLayers.Any(forbidden => IsInNamespace(pair.referenced, forbidden)))
                        .Select(pair => $"{pair.type.FullName} -> {pair.referenced.FullName}")
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();

        // Verify
        Assert.Empty(leaks);
    }

    /// <summary>
    ///     The engine's dependency list is a whitelist, not an absence - the count was never the point.
    ///     What matters is where a dependency sits: one taken by Slugger.Core reaches everybody who
    ///     references it, as a line in the published nuspec, while one taken by Slugger.Cli stops at the
    ///     executable. So the engine's list is worth a deliberate act, and this test is what makes
    ///     adding to it one: a package arriving here without being named is an accident, not a decision.
    /// </summary>
    /// <remarks>
    ///     FirstClassErrors is on the list because Outcome and the error model are worth its weight.
    ///     TextCopy is not, and never will be: a clipboard has no business in a slug engine, which
    ///     ClipboardDependencyTests states from the other side.
    /// </remarks>
    [Fact]
    public void The_engine_depends_on_nothing_but_what_was_deliberately_taken_on() {
        // Setup
        Assembly core    = typeof(Themes).Assembly;
        string[] allowed = ["FirstClassErrors", "Value"];

        // Exercise
        string[] external = core.GetReferencedAssemblies()
                                .Select(reference => reference.Name)
                                .Where(name => name is not null && !name.StartsWith("System", StringComparison.Ordinal))
                                .Select(name => name!)
                                .Except(allowed, StringComparer.Ordinal)
                                .ToArray();

        // Verify
        Assert.Empty(external);
    }

    /// <summary>
    ///     What the package offers, against how it is built. Application and Infrastructure are the
    ///     second: a consumer referencing Slugger should not have to scroll past a JSON serializer and
    ///     five use cases to find the four types actually promised to them, and nothing out there
    ///     is owed their stability. One assembly is what made `internal` possible; this keeps it used.
    /// </summary>
    [Theory]
    [InlineData(Application)]
    [InlineData(Infrastructure)]
    public void A_supporting_layer_exposes_nothing_publicly(string layer) {
        // Setup
        Type[] layerTypes = TypesInNamespace(layer);
        Assert.NotEmpty(layerTypes);

        // Exercise
        string[] exposed = layerTypes
                          .Where(type => type.IsVisible)
                          .Select(type => type.FullName!)
                          .Order(StringComparer.Ordinal)
                          .ToArray();

        // Verify
        Assert.Empty(exposed);
    }

}