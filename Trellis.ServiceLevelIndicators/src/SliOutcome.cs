namespace Trellis.ServiceLevelIndicators;

/// <summary>
/// SLI outcome emitted with each operation measurement.
/// </summary>
public enum SliOutcome
{
    /// <summary>
    /// The operation completed successfully and counts toward the success numerator and denominator.
    /// </summary>
    Success,

    /// <summary>
    /// The operation failed and counts toward the success-rate denominator.
    /// </summary>
    Failure,

    /// <summary>
    /// The operation failed because of caller input or another expected client condition.
    /// </summary>
    ClientError,

    /// <summary>
    /// The operation should be excluded from the default success-rate denominator.
    /// </summary>
    Ignored
}
