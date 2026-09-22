#region Usings declarations

using Slugger.Application.Abstractions;

#endregion

namespace Slugger.UnitTests;

/// <summary>A clipboard that remembers the last thing copied to it.</summary>
internal sealed class FakeClipboard : IClipboard {

    internal string? LastCopied { get; private set; }

    internal int Copies { get; private set; }

    public void Copy(string text) {
        LastCopied = text;
        Copies++;
    }

}