#region Usings declarations

using Spectre.Console.Cli;

#endregion

namespace Slugger.Cli.CommandLine;

/// <summary>
///     How Spectre.Console.Cli reaches the ports this application already builds by hand. The
///     composition root stays <see cref="Program" />: this only hands over what it built, so a test
///     can build the same application over fakes and drive it through the real command line.
/// </summary>
/// <remarks>
///     Spectre asks for a container. slugger has none and wants none - a handful of ports and one
///     runner do not need a graph - so this is a dictionary of instances plus the type Spectre
///     registers for its own command, built from those ports when it is asked for.
/// </remarks>
internal sealed class PortRegistrar : ITypeRegistrar {

    #region Fields

    private readonly Dictionary<Type, object>       _instances     = [];
    private readonly Dictionary<Type, Type>         _registrations = [];
    private readonly Dictionary<Type, Lazy<object>> _deferred      = [];

    #endregion

    /// <inheritdoc />
    public ITypeResolver Build() {
        return new PortResolver(_instances, _registrations, _deferred);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     The mapping is recorded, never acted on: Spectre registers its command type here, and a
    ///     command taking ports has no parameterless constructor to build it with yet.
    /// </remarks>
    public void Register(Type service, Type implementation) {
        _registrations[service] = implementation;
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation) {
        _instances[service] = implementation;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Kept lazy, as its name promises: the factory runs the first time the type is asked for,
    ///     and not at all when it never is. Running it here would make "lazy" mean "eager", and
    ///     whatever it costs would be paid by every command line, "--help" included.
    /// </remarks>
    public void RegisterLazy(Type service, Func<object> factory) {
        ArgumentNullException.ThrowIfNull(factory);

        _deferred[service] = new Lazy<object>(factory);
    }

    /// <summary>Registers one already-built instance under the type a command will ask for.</summary>
    /// <typeparam name="TService">The type a command's constructor names.</typeparam>
    /// <param name="instance">What to hand it.</param>
    internal PortRegistrar With<TService>(TService instance)
        where TService : class {
        _instances[typeof(TService)] = instance;

        return this;
    }

    #region Nested types

    private sealed class PortResolver(
        Dictionary<Type, object>       instances,
        Dictionary<Type, Type>         registrations,
        Dictionary<Type, Lazy<object>> deferred)
        : ITypeResolver {

        /// <inheritdoc />
        /// <remarks>
        ///     A port is handed back as it was built. Anything else - in practice the command Spectre
        ///     is about to run - is constructed from the ports its constructor names, because Spectre
        ///     otherwise falls back to a parameterless constructor it does not have.
        /// </remarks>
        public object? Resolve(Type? type) {
            if (type is null) { return null; }

            if (instances.TryGetValue(type, out object? instance)) { return instance; }

            // Made once and kept, so two asks hand back one object rather than two.
            if (deferred.TryGetValue(type, out Lazy<object>? made)) { return made.Value; }

            Type wanted = registrations.TryGetValue(type, out Type? implementation) ? implementation : type;

            // Spectre asks for its optional collaborators as a sequence - help providers, and
            // whatever it adds next. Nothing registered means none, which is an empty one rather
            // than a null it would then refuse.
            if (wanted.IsGenericType && wanted.GetGenericTypeDefinition() == typeof(IEnumerable<>)) { return Array.CreateInstance(wanted.GetGenericArguments()[0], 0); }

            return wanted.GetConstructors().FirstOrDefault() is { } constructor
                ? constructor.Invoke([.. constructor.GetParameters().Select(parameter => Resolve(parameter.ParameterType))])
                : null;
        }

    }

    #endregion

}