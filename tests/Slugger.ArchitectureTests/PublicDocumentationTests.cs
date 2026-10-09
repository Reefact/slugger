#region Usings declarations

using System.Xml.Linq;

using Mono.Cecil;

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
    ///     Whether a documentation id names something a consumer cannot reach: a type that is not
    ///     visible, a member of one, or a member whose every match by name and arity is internal or
    ///     private. An id it cannot match is not counted against the file.
    /// </summary>
    /// <param name="id">A documentation id, such as <c>M:Slugger.Domain.Theme.#ctor(System.String)</c>.</param>
    /// <param name="types">Every type of the assembly, by its documentation name.</param>
    private static bool IsUnreachable(string id, Dictionary<string, TypeDefinition> types) {
        char   kind = id[0];
        string rest = id[2..];
        if (kind == 'T') { return types.TryGetValue(rest, out TypeDefinition? documented) && !IsVisible(documented); }

        int    open = rest.IndexOf('(', StringComparison.Ordinal);
        string head = open < 0 ? rest : rest[..open];
        int    dot  = head.LastIndexOf('.');
        if (!types.TryGetValue(head[..dot], out TypeDefinition? type)) { return false; }
        if (!IsVisible(type)) { return true; }

        string name    = head[(dot + 1)..].Replace('#', '.').Split("``")[0];
        int    arity   = open < 0 ? 0 : CountParameters(rest[open..]);
        bool[] matches = [.. Visibilities(type, kind, name, arity)];

        return matches.Length > 0 && !matches.Contains(true);
    }

    /// <summary>Whether each member of that kind, name and arity is visible to a consumer.</summary>
    private static IEnumerable<bool> Visibilities(TypeDefinition type, char kind, string name, int arity) {
        if (kind == 'M') { return type.Methods.Where(method => method.Name == name && method.Parameters.Count == arity).Select(IsVisible); }
        if (kind == 'P') { return type.Properties.Where(property => property.Name == name && property.Parameters.Count == arity).Select(IsVisible); }
        if (kind == 'F') { return type.Fields.Where(field => field.Name == name).Select(field => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly); }
        if (kind == 'E') { return type.Events.Where(handler => handler.Name == name).Select(handler => IsVisible(handler.AddMethod)); }

        return [];
    }

    /// <summary>How many parameters a parenthesised list holds, generic arguments and array bounds aside.</summary>
    private static int CountParameters(string parameters) {
        int depth = 0;
        int count = 1;
        foreach (char character in parameters[1..]) {
            if (character is '(' or '{' or '[') { depth++; }
            if (character == ',' && depth == 0) { count++; }
            if (character is ')' or '}' or ']') {
                if (depth == 0) { break; }

                depth--;
            }
        }

        return count;
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

    private static bool IsVisible(PropertyDefinition property) {
        return IsVisible(property.GetMethod) || IsVisible(property.SetMethod);
    }

    #endregion

    [Fact]
    public void Documents_nothing_a_consumer_cannot_reach() {
        // Setup
        using AssemblyDefinition engine = AssemblyDefinition.ReadAssembly(EngineAssembly);
        Dictionary<string, TypeDefinition> types = EveryType(engine.MainModule.Types).ToDictionary(type => type.FullName.Replace('/', '.'));

        // Exercise
        string[] unreachable = [.. DocumentedIds().Where(id => IsUnreachable(id, types))];

        // Verify
        Assert.Empty(unreachable);
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
