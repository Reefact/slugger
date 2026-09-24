namespace Slugger;

/// <summary>
///     ValueObject (Domain-Driven Design) - an object of the domain described only by its values. It
///     carries no identity, it is immutable, and it exists because it says something about the
///     domain rather than because comparing it by value is convenient.
/// </summary>
/// <remarks>
///     <para>
///         A stand-in for the attribute of the same name in
///         <c>DesignPatternCatalog.DomainDrivenDesign</c>, which is not on nuget.org yet - measured,
///         the feed answers NotFound for every version. It is to be replaced by the package rather
///         than grown: no member, no behaviour, nothing to migrate but a using directive.
///     </para>
///     <para>
///         What it is for is <c>ValueObjectRulesTests</c>: the rules a value object must keep are
///         applied to what carries this attribute and to nothing else, so a record or an entity is
///         never measured against them by accident.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
internal sealed class ValueObjectAttribute : Attribute { }
