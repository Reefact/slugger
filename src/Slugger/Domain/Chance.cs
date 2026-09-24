#region Usings declarations

using System.Diagnostics;

using FirstClassErrors;

using Value;

#endregion

namespace Slugger.Domain;

/// <summary>
///     How often something happens, out of a hundred: never at nought, always at a hundred. Whole
///     rather than fractional because that is how the domain states one - <c>--token-chance</c>
///     counts hundredths and nothing finer.
/// </summary>
[ValueObject]
[DebuggerDisplay("{ToString()}")]
public sealed class Chance : ValueType<Chance> {

    #region Static members

    /// <summary>It does not happen, which decides without asking anything.</summary>
    public static Chance Never { get; } = new(0);

    /// <summary>It always happens, which decides without asking anything too.</summary>
    public static Chance Always { get; } = new(100);

    /// <summary>The chance that number names, or the reason it names none.</summary>
    /// <param name="value">A whole number from 0 to 100.</param>
    public static Outcome<Chance> From(int value) {
        if (value is < 0 or > 100) { return Outcome<Chance>.Failure(ChanceError.OutsideTheRange(value)); }

        return Outcome<Chance>.Success(new Chance(value));
    }

    /// <summary>The same, for a caller that has no report to fill.</summary>
    /// <param name="value">A whole number from 0 to 100.</param>
    /// <exception cref="ChanceException">The number is outside 0 to 100; the exception carries the reason.</exception>
    public static Chance FromOrThrow(int value) {
        Outcome<Chance> outcome = From(value);
        if (outcome.Error is ChanceError refused) { throw refused.ToException(); }

        return outcome.GetResultOrThrow();
    }

    #endregion

    #region Fields

    private readonly int _value;

    #endregion

    #region Constructors & Destructor

    private Chance(int value) {
        _value = value;
    }

    #endregion

    /// <summary>It never happens, so nothing it decides needs to be asked.</summary>
    public bool IsNever => _value == 0;

    /// <summary>It always happens, so nothing it decides needs to be asked either.</summary>
    public bool IsAlways => _value == 100;

    /// <summary>
    ///     Whether a roll lands inside the chance. A roll of nought to ninety-nine covers a hundred
    ///     outcomes, so a chance of one takes the roll that is nought and nothing else.
    /// </summary>
    /// <param name="roll">A draw from 0 to 99.</param>
    public bool Covers(int roll) {
        return roll < _value;
    }

    /// <summary>Whether a roll falls outside the chance, so a caller reads no negation.</summary>
    /// <param name="roll">A draw from 0 to 99.</param>
    public bool DoesNotCover(int roll) {
        return !Covers(roll);
    }

    /// <summary>The chance, for a human reading a watch window.</summary>
    public override string ToString() {
        return $"{_value}%";
    }

    /// <summary>Two chances of the same frequency are the same chance.</summary>
    protected override IEnumerable<object> GetAllAttributesToBeUsedForEquality() {
        yield return _value;
    }

}
