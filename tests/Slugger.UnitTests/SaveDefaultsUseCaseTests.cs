#region Usings declarations

using Slugger.Application.Options;
using Slugger.Application.UseCases;

#endregion

namespace Slugger.UnitTests;

public sealed class SaveDefaultsUseCaseTests {

    [Fact]
    public void Persists_what_the_command_line_carried() {
        // Setup
        FakeConfigStore     config  = new();
        SaveDefaultsUseCase useCase = new(config);

        // Exercise
        useCase.Execute(new SluggerOptions { Count = 5, Oneshot = true });

        // Verify
        Assert.Equal(5, config.Stored!.Count);
        Assert.True(config.Stored.Oneshot);
    }

    /// <summary>A second --init adds to the config rather than wiping what it says nothing about.</summary>
    [Fact]
    public void Lays_over_what_was_already_saved() {
        // Setup
        FakeConfigStore     config  = new(new SluggerOptions { Seed = 42, Count = 2 });
        SaveDefaultsUseCase useCase = new(config);

        // Exercise
        useCase.Execute(new SluggerOptions { Count = 7 });

        // Verify
        Assert.Equal(7, config.Stored!.Count);
        Assert.Equal(42, config.Stored.Seed);
    }

}