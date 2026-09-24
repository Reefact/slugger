#region Usings declarations

using System.Reflection;

using Value;

#endregion

namespace Slugger.ArchitectureTests;

/// <summary>
///     What an entity has to keep, applied to the types that claim to be one and to no others.
///     The claim is <c>[Entity]</c>, for the same reason <c>[ValueObject]</c> is a claim: the two
///     patterns answer the question of what makes two objects the same in opposite ways, and a
///     type has to say which one it means.
/// </summary>
public sealed class EntityRulesTests {

    #region Static members

    /// <summary>Every type of the engine that claims to be an entity.</summary>
    public static TheoryData<Type> Entities {
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
                             .Where(type => type.GetCustomAttribute<EntityAttribute>() is not null)
                             .OrderBy(type => type.FullName, StringComparer.Ordinal);
    }

    private static bool Declares(Type entity, string method) {
        return entity.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) is not null;
    }

    #endregion

    /// <summary>
    ///     The guard the theories below need: a theory over an empty set passes without asserting
    ///     anything, so a marker nobody applied would make every rule green.
    /// </summary>
    [Fact]
    public void The_engine_declares_at_least_one_entity() {
        // Verify
        Assert.NotEmpty(Marked());
    }

    /// <summary>
    ///     An entity is who it is, not what it holds. Deriving from <c>ValueType&lt;T&gt;</c> would
    ///     answer that question by comparing the contents, which is the value object's answer and
    ///     the one an entity exists to refuse.
    /// </summary>
    [Theory]
    [MemberData(nameof(Entities))]
    public void Is_not_compared_by_what_it_holds(Type entity) {
        // Exercise
        Type byValue = typeof(ValueType<>).MakeGenericType(entity);

        // Verify
        Assert.False(
            byValue.IsAssignableFrom(entity),
            $"{entity.Name} claims to be an entity but derives from ValueType<{entity.Name}>, which compares it by its values.");
    }

    /// <summary>
    ///     Identity has to be written down, because the default is a reference: without this, the
    ///     same entity read twice is two, which is the one thing an entity is for.
    /// </summary>
    [Theory]
    [MemberData(nameof(Entities))]
    public void Says_what_makes_two_of_them_the_same_one(Type entity) {
        // Verify
        Assert.True(
            Declares(entity, nameof(Equals)) && Declares(entity, nameof(GetHashCode)),
            $"{entity.Name} claims to be an entity but leaves Equals or GetHashCode to the reference it is held by.");
    }

    /// <summary>
    ///     An entity is not built from outside: it is read, refused where it breaks a rule, and
    ///     only then made. A public constructor is a second door, and one that skips the refusal.
    /// </summary>
    [Theory]
    [MemberData(nameof(Entities))]
    public void Is_built_by_whoever_read_it_and_by_nobody_else(Type entity) {
        // Exercise
        ConstructorInfo[] published = entity.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

        // Verify
        Assert.True(
            published.Length == 0,
            $"{entity.Name} claims to be an entity but publishes {published.Length} constructor(s), so one can be made without being read.");
    }

}
