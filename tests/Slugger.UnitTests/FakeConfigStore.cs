#region Usings declarations

using Slugger.Application.Abstractions;
using Slugger.Application.Options;

#endregion

namespace Slugger.UnitTests;

/// <summary>A config store that keeps what it was given in memory.</summary>
internal sealed class FakeConfigStore(SluggerOptions? initial = null) : IConfigStore {

    internal SluggerOptions? Stored { get; private set; } = initial;

    public SluggerOptions? Load() {
        return Stored;
    }

    public void Save(SluggerOptions options) {
        Stored = options;
    }

}