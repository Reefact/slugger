using Spectre.Console.Cli;

namespace Slugger.Cli.CommandLine;

/// <summary>
/// How Spectre.Console.Cli reaches the ports this application already builds by hand. The
/// composition root stays <see cref="Program"/>: this only hands over what it built, so a test
/// can build the same application over fakes and drive it through the real command line.
/// </summary>
/// <remarks>
/// Spectre asks for a container. slugger has none and wants none - a handful of ports and one
/// runner do not need a graph - so this is a dictionary of instances plus the type Spectre
/// registers for its own command, built from those ports when it is asked for.
/// </remarks>
internal sealed class PortRegistrar : ITypeRegistrar
{
    private readonly Dictionary<Type, object> _instances = [];
    private readonly Dictionary<Type, Type> _registrations = [];

    /// <summary>Registers one already-built instance under the type a command will ask for.</summary>
    /// <typeparam name="TService">The type a command's constructor names.</typeparam>
    /// <param name="instance">What to hand it.</param>
    internal PortRegistrar With<TService>(TService instance)
        where TService : class
    {
        _instances[typeof(TService)] = instance;

        return this;
    }

    /// <inheritdoc />
    public ITypeResolver Build() => new PortResolver(_instances, _registrations);

    /// <inheritdoc />
    /// <remarks>
    /// The mapping is recorded, never acted on: Spectre registers its command type here, and a
    /// command taking ports has no parameterless constructor to build it with yet.
    /// </remarks>
    public void Register(Type service, Type implementation) => _registrations[service] = implementation;

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation) => _instances[service] = implementation;

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _instances[service] = factory();
    }

    private sealed class PortResolver(Dictionary<Type, object> instances, Dictionary<Type, Type> registrations)
        : ITypeResolver
    {
        /// <inheritdoc />
        /// <remarks>
        /// A port is handed back as it was built. Anything else - in practice the command Spectre
        /// is about to run - is constructed from the ports its constructor names, because Spectre
        /// otherwise falls back to a parameterless constructor it does not have.
        /// </remarks>
        public object? Resolve(Type? type)
        {
            if (type is null)
            {
                return null;
            }

            if (instances.TryGetValue(type, out object? instance))
            {
                return instance;
            }

            Type wanted = registrations.TryGetValue(type, out Type? implementation) ? implementation : type;

            // Spectre asks for its optional collaborators as a sequence - help providers, and
            // whatever it adds next. Nothing registered means none, which is an empty one rather
            // than a null it would then refuse.
            if (wanted.IsGenericType && wanted.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return Array.CreateInstance(wanted.GetGenericArguments()[0], 0);
            }

            return wanted.GetConstructors().FirstOrDefault() is { } constructor
                ? constructor.Invoke([.. constructor.GetParameters().Select(parameter => Resolve(parameter.ParameterType))])
                : null;
        }
    }
}
