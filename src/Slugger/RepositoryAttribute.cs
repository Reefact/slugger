namespace Slugger;

/// <summary>
///     Repository (Domain-Driven Design) - the door the domain reaches an entity through, which
///     looks like a collection and hides whatever actually holds them.
/// </summary>
/// <remarks>
///     A stand-in, on the same terms as <see cref="EntityAttribute" /> and
///     <see cref="ValueObjectAttribute" />: the attribute of that name in
///     <c>DesignPatternCatalog.DomainDrivenDesign</c>, carried here until the package is published.
///     It marks the interface, never the implementation - what the domain names is the door, and
///     the thing behind it belongs to <c>Infrastructure</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
internal sealed class RepositoryAttribute : Attribute { }
