namespace Slugger;

/// <summary>An adjective, and how many nouns can reach it.</summary>
public sealed class WordExposure {

    #region Constructors & Destructor

    internal WordExposure(string word, int nouns) {
        Word  = word;
        Nouns = nouns;
    }

    #endregion

    /// <summary>The adjective, as normalised at load.</summary>
    public string Word { get; }

    /// <summary>How many nouns can draw it.</summary>
    public int Nouns { get; }

}
