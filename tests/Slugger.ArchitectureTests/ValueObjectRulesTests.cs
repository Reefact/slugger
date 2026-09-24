#region Usings declarations

using System.Diagnostics;
using System.Reflection;

using FirstClassErrors;

using Value;

#endregion

namespace Slugger.ArchitectureTests;

/// <summary>
///     What a value object has to keep, applied to the types that claim to be one and to no others.
///     The claim is <c>[ValueObject]</c>, so a record carrying identity is never measured against
///     rules it was never meant to keep - which is the whole reason the marker exists rather than a
///     naming convention.
/// </summary>
public sealed class ValueObjectRulesTests {

    #region Static members

    /// <summary>Every type of the engine that claims to be a value object.</summary>
    public static TheoryData<Type> ValueObjects {
        get {
            TheoryData<Type> claimants = [];
            foreach (Type type in Marked()) {
                claimants.Add(type);
            }

            return claimants;
        }
    }

    private static IEnumerable<Type> Marked() {
        return typeof(Themes).Assembly
                             .GetTypes()
                             .Where(type => type.GetCustomAttribute<ValueObjectAttribute>() is not null)
                             .OrderBy(type => type.FullName, StringComparer.Ordinal);
    }

    #endregion

    /// <summary>
    ///     The guard the theories below need: a theory over an empty set passes without asserting
    ///     anything, so a marker nobody applied would make every rule green.
    /// </summary>
    [Fact]
    public void The_engine_declares_at_least_one_value_object() {
        // Verify
        Assert.NotEmpty(Marked());
    }

    /// <summary>
    ///     A value object is its values, so two of them holding the same ones are the same object.
    ///     Deriving from <c>ValueType&lt;T&gt;</c> is what states the contract rather than leaving
    ///     equality to a reference.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValueObjects))]
    public void Compares_by_its_values_rather_than_by_reference(Type valueObject) {
        // Exercise
        Type byValue = typeof(ValueType<>).MakeGenericType(valueObject);

        // Verify
        Assert.True(
            byValue.IsAssignableFrom(valueObject),
            $"{valueObject.Name} claims to be a value object but does not derive from ValueType<{valueObject.Name}>.");
    }

    /// <summary>
    ///     Immutability, read on the fields the type declares itself - the cached hash code of
    ///     <c>EquatableByValue</c> lives on the base and is none of its business.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValueObjects))]
    public void Holds_nothing_that_can_change_after_it_is_built(Type valueObject) {
        // Exercise
        string[] mutable = valueObject
                          .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                          .Where(field => !field.IsInitOnly)
                          .Select(field => field.Name)
                          .ToArray();

        // Verify
        Assert.Empty(mutable);
    }

    /// <summary>The same rule from the other side: nothing it exposes can be written to either.</summary>
    [Theory]
    [MemberData(nameof(ValueObjects))]
    public void Exposes_nothing_that_can_be_written_to(Type valueObject) {
        // Exercise
        string[] settable = valueObject
                           .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                           .Where(property => property.SetMethod is not null)
                           .Select(property => property.Name)
                           .ToArray();

        // Verify
        Assert.Empty(settable);
    }

    /// <summary>
    ///     An error catalogue belongs to one type and is named after it, so that finding the
    ///     refusals a type can produce is reading a name rather than searching. A type with no
    ///     refusal has no catalogue, which is why this reads from the catalogues rather than from
    ///     the types: absence is allowed, a mismatch is not.
    /// </summary>
    [Fact]
    public void An_error_catalogue_is_named_after_the_type_it_speaks_for() {
        // Setup
        Assembly engine = typeof(Themes).Assembly;

        // Exercise
        string[] mismatched = engine
                             .GetTypes()
                             .Select(type => (type, provides: type.GetCustomAttribute<ProvidesErrorsForAttribute>()))
                             .Where(pair => pair.provides is not null)
                             .Where(pair => pair.type.Name != $"{pair.provides!.Source}Error"
                                         || engine.GetTypes().All(other => other.Name != pair.provides.Source))
                             .Select(pair => $"{pair.type.Name} provides errors for '{pair.provides!.Source}'")
                             .ToArray();

        // Verify
        Assert.Empty(mismatched);
    }

    /// <summary>
    ///     A value object spells itself out for a human. Inheriting object's rendering would show
    ///     the type's name, which says nothing in a watch window.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValueObjects))]
    public void Renders_itself_for_a_human_to_read(Type valueObject) {
        // Exercise
        MethodInfo? rendering = valueObject.GetMethod(nameof(ToString), Type.EmptyTypes);

        // Verify
        Assert.Equal(valueObject, rendering?.DeclaringType);
    }

    /// <summary>
    ///     And the debugger is pointed at that rendering, so the watch window and the message a
    ///     human reads cannot drift apart.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValueObjects))]
    public void Points_the_debugger_at_that_rendering(Type valueObject) {
        // Exercise
        DebuggerDisplayAttribute? display = valueObject.GetCustomAttribute<DebuggerDisplayAttribute>();

        // Verify
        Assert.Equal("{ToString()}", display?.Value);
    }

}
