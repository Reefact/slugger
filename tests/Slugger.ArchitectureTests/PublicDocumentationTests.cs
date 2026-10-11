#region Usings declarations

using System.Xml.Linq;

using Mono.Cecil;
using Mono.Cecil.Rocks;

#endregion

namespace Slugger.ArchitectureTests;

/// <summary>
///     The documentation file a consumer receives describes what a consumer can reach. The compiler
///     writes every doc comment into it, whatever the member's accessibility, and
///     <c>KeepOnlyPublicDocumentation</c> in Slugger.csproj removes the rest from the copy that the
///     package and every project reference take - this one included.
/// </summary>
/// <remarks>
///     Read with Cecil rather than with the reflection the build task uses, so that the task and its
///     test do not share a mistake.
/// </remarks>
public sealed class PublicDocumentationTests {

    #region Static members

    private static readonly string EngineAssembly = typeof(Themes).Assembly.Location;

    private static IReadOnlyList<string> DocumentedIds() {
        string file = Path.ChangeExtension(EngineAssembly, ".xml");

        return [.. XDocument.Load(file).Descendants("member").Select(member => (string)member.Attribute("name")!)];
    }

    private static IEnumerable<TypeDefinition> EveryType(IEnumerable<TypeDefinition> types) {
        foreach (TypeDefinition type in types) {
            yield return type;

            foreach (TypeDefinition nested in EveryType(type.NestedTypes)) {
                yield return nested;
            }
        }
    }

    /// <summary>
    ///     The documentation id of everything a consumer cannot reach: a type that is not visible,
    ///     every member of one, and every member of a visible type that is internal or private.
    ///     Spelled by Cecil from the full signature, so an internal overload is told from a public
    ///     one of the same name and arity.
    /// </summary>
    private static HashSet<string> UnreachableIds(IEnumerable<TypeDefinition> types) {
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (TypeDefinition type in types) {
            bool hidden = !IsVisible(type);
            if (hidden) {
                ids.Add(DocCommentId.GetDocCommentId(type));
            }

            foreach (IMemberDefinition member in MembersOf(type).Where(member => hidden || !IsVisible(member))) {
                ids.Add(DocCommentId.GetDocCommentId(member));
            }
        }

        return ids;
    }

    /// <summary>The documentation id of every type and member of the assembly, reachable or not.</summary>
    private static HashSet<string> EveryId(IEnumerable<TypeDefinition> types) {
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (TypeDefinition type in types) {
            ids.Add(DocCommentId.GetDocCommentId(type));
            ids.UnionWith(MembersOf(type).Select(DocCommentId.GetDocCommentId));
        }

        return ids;
    }

    private static IEnumerable<IMemberDefinition> MembersOf(TypeDefinition type) {
        return type.Methods.Cast<IMemberDefinition>()
                   .Concat(type.Properties)
                   .Concat(type.Fields)
                   .Concat(type.Events);
    }

    private static bool IsVisible(IMemberDefinition member) {
        return member switch {
            MethodDefinition method     => IsVisible(method),
            PropertyDefinition property => IsVisible(property.GetMethod) || IsVisible(property.SetMethod),
            FieldDefinition field       => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly,
            EventDefinition handler     => IsVisible(handler.AddMethod),
            _                           => false
        };
    }

    private static bool IsVisible(TypeDefinition type) {
        if (!type.IsNested) { return type.IsPublic; }
        if (!type.IsNestedPublic && !type.IsNestedFamily && !type.IsNestedFamilyOrAssembly) { return false; }

        return IsVisible(type.DeclaringType);
    }

    private static bool IsVisible(MethodDefinition? method) {
        if (method is null) { return false; }

        return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
    }

    #endregion

    [Fact]
    public void Documents_nothing_a_consumer_cannot_reach() {
        // Setup
        using AssemblyDefinition engine = AssemblyDefinition.ReadAssembly(EngineAssembly);
        HashSet<string> unreachable = UnreachableIds(EveryType(engine.MainModule.Types));

        // Exercise
        string[] leaked = [.. DocumentedIds().Where(unreachable.Contains)];

        // Verify
        Assert.Empty(leaked);
    }

    /// <summary>
    ///     The test above can only see a leak whose id Cecil spells as the compiler does. An id it
    ///     spelled differently would match nothing and pass unseen, so every id in the file has to
    ///     name something Cecil knows.
    /// </summary>
    [Fact]
    public void Spells_every_documented_id_as_the_compiler_does() {
        // Setup
        using AssemblyDefinition engine = AssemblyDefinition.ReadAssembly(EngineAssembly);
        HashSet<string> known = EveryId(EveryType(engine.MainModule.Types));

        // Exercise
        string[] unknown = [.. DocumentedIds().Where(id => !known.Contains(id))];

        // Verify
        Assert.Empty(unknown);
    }

    /// <summary>
    ///     SlugGenerator.Generate has public overloads and an internal one of the same arity, so
    ///     the filter cannot decide by name and parameter count: it has to read the parameter types.
    /// </summary>
    /// <remarks>Literal on purpose: these two overloads are the case.</remarks>
    [Fact]
    public void Tells_an_internal_overload_from_a_public_one_of_the_same_arity() {
        // Exercise
        IReadOnlyList<string> documented = DocumentedIds();

        // Verify
        Assert.Contains("M:Slugger.Domain.Generation.SlugGenerator.Generate(Slugger.Domain.Resolution.WeightedThemePicker,Slugger.Domain.GenerationOptions,Slugger.Domain.IRandomSource)", documented);
        Assert.DoesNotContain("M:Slugger.Domain.Generation.SlugGenerator.Generate(Slugger.Domain.Resolution.ThemeResolver,Slugger.Domain.GenerationOptions,Slugger.Domain.IRandomSource)", documented);
    }

    /// <summary>
    ///     A filter that removed everything would pass the test above, so this one names a type and
    ///     a method a consumer calls first.
    /// </summary>
    /// <remarks>Literal on purpose: these ids are the public surface being pinned.</remarks>
    [Fact]
    public void Still_documents_the_public_surface() {
        // Exercise
        IReadOnlyList<string> documented = DocumentedIds();

        // Verify
        Assert.Contains("T:Slugger.Themes", documented);
        Assert.Contains("M:Slugger.Domain.Generation.SlugGenerator.Generate(Slugger.Domain.ThemeDocument,Slugger.Domain.GenerationOptions)", documented);
    }

}
