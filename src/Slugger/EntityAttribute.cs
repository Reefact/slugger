namespace Slugger;

/// <summary>
///     Entity (Domain-Driven Design) - an object of the domain defined by who it is rather than by
///     what it holds. Two entities with the same values are two entities; the same one read twice
///     is the same one, whatever changed in between.
/// </summary>
/// <remarks>
///     <para>
///         A stand-in for the attribute of the same name in
///         <c>DesignPatternCatalog.DomainDrivenDesign</c>, which is not on nuget.org yet, and to be
///         replaced by the package rather than grown: no member, no behaviour, nothing to migrate
///         but a using directive. <see cref="ValueObjectAttribute" /> says the same of its own.
///     </para>
///     <para>
///         What it is for is the same as there: a rule measured against what carries the marker and
///         against nothing else, so a record that happens to look like one is never held to it.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
internal sealed class EntityAttribute : Attribute { }
