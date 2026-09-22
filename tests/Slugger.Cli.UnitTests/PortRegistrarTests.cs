#region Usings declarations

using Slugger.Cli.CommandLine;

using Spectre.Console.Cli;

#endregion

namespace Slugger.Cli.UnitTests;

/// <summary>
///     The container slugger does not have. Spectre asks for one, and this hands over the ports the
///     composition root already built - so what is worth pinning is the contract it implements on
///     Spectre's behalf rather than the ports themselves.
/// </summary>
public sealed class PortRegistrarTests {

    [Fact]
    public void Hands_back_the_instance_it_was_given() {
        // Setup
        FakeConsole   console = new();
        PortRegistrar ports   = new PortRegistrar().With<IConsole>(console);

        // Verify
        Assert.Same(console, ports.Build().Resolve(typeof(IConsole)));
    }

    /// <summary>
    ///     Lazy as the name promises. Running the factory at registration would make every command
    ///     line pay for a collaborator it may never reach - <c>--help</c> included, which reaches
    ///     none of them.
    /// </summary>
    [Fact]
    public void Leaves_a_deferred_factory_alone_until_the_type_is_asked_for() {
        // Setup
        int           made  = 0;
        PortRegistrar ports = new();

        // Exercise
        ports.RegisterLazy(typeof(IConsole), () => {
            made++;

            return new FakeConsole();
        });

        // Verify - built, and still nothing made.
        ITypeResolver resolver = ports.Build();
        Assert.Equal(0, made);

        resolver.Resolve(typeof(IConsole));
        Assert.Equal(1, made);
    }

    /// <summary>One object rather than one per ask, which is what a registration means.</summary>
    [Fact]
    public void Makes_a_deferred_type_once_however_often_it_is_asked_for() {
        // Setup
        PortRegistrar ports = new();
        ports.RegisterLazy(typeof(IConsole), () => new FakeConsole());

        // Exercise
        ITypeResolver resolver = ports.Build();

        // Verify
        Assert.Same(resolver.Resolve(typeof(IConsole)), resolver.Resolve(typeof(IConsole)));
    }

    /// <summary>
    ///     Spectre asks for its optional collaborators as a sequence, and refuses a null where it
    ///     expected one. Nothing registered means none, which is an empty sequence.
    /// </summary>
    [Fact]
    public void Answers_a_sequence_nothing_was_registered_for_with_an_empty_one() {
        // Exercise
        object? resolved = new PortRegistrar().Build().Resolve(typeof(IEnumerable<IConsole>));

        // Verify
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<IConsole>>(resolved));
    }

}