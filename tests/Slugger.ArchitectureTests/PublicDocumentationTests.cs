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

    private static readonly string EngineAssembly = typeof(SluggerTheme).Assembly.Location;

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
    ///     SlugGenerator has public members and internal ones taking the same first parameter, so the
    ///     filter cannot decide by type alone: it has to read each member's own accessibility. No public
    ///     type has a public and an internal overload of the same name and arity any more (DEC0036), so
    ///     this is the closest case the surface holds.
    /// </summary>
    /// <remarks>Literal on purpose: these two members are the case.</remarks>
    [Fact]
    public void Tells_an_internal_member_from_a_public_one_on_the_same_type() {
        // Exercise
        IReadOnlyList<string> documented = DocumentedIds();

        // Verify
        Assert.Contains("M:Slugger.SlugGenerator.For(Slugger.SluggerTheme,System.Func{Slugger.GenerationOptions,Slugger.GenerationOptions},System.Boolean)", documented);
        Assert.DoesNotContain("M:Slugger.SlugGenerator.Prepare(Slugger.SluggerTheme,Slugger.GenerationOptions,System.Boolean)", documented);
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
        Assert.Contains("T:Slugger.SluggerTheme", documented);
        Assert.Contains("M:Slugger.SluggerTheme.LoadEmbeddedOrThrow(System.String,System.Boolean)", documented);
        Assert.Contains("M:Slugger.SlugGenerator.Next", documented);
    }

    /// <summary>
    ///     A refused theme raises <c>SluggerThemeException</c>, a sibling of <c>DomainException</c> rather
    ///     than a kind of it (DEC0035): a consumer who catches what the documentation names would miss
    ///     every refusal if the two disagreed, so no shipped exception tag names the old one.
    /// </summary>
    [Fact]
    public void Promises_the_theme_exception_wherever_a_theme_is_refused() {
        // Setup
        string file = Path.ChangeExtension(EngineAssembly, ".xml");

        // Exercise
        string[] promised = [
            .. XDocument.Load(file)
                        .Descendants("exception")
                        .Select(exception => (string)exception.Attribute("cref")!)
                        .Distinct(StringComparer.Ordinal)
        ];

        // Verify
        Assert.DoesNotContain("T:Slugger.Errors.DomainException", promised);
        Assert.Contains("T:Slugger.SluggerThemeException", promised);
    }

}
