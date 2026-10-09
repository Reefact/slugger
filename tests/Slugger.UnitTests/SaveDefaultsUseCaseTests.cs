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

    /// <summary>
    ///     A relative directory is read from wherever the command runs, and the run that reads the
    ///     saved one starts somewhere else - so it is saved as the directory it named from here.
    /// </summary>
    [Fact]
    public void Saves_a_relative_theme_directory_as_the_absolute_path_it_names() {
        // Setup
        string              relative = Path.Combine(Dummies.AnyWord(), Dummies.AnyWord());
        FakeConfigStore     config   = new();
        SaveDefaultsUseCase useCase  = new(config);

        // Exercise
        useCase.Execute(new SluggerOptions { ThemeDirectory = relative });

        // Verify
        Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), relative), config.Stored!.ThemeDirectory);
    }

}