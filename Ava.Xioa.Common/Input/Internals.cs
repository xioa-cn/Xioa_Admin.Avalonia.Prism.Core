namespace Ava.Xioa.Common.Input;

internal interface ICancellationAwareCommand
{
    /// <summary>
    /// Gets whether or not the current command supports cancellation.
    /// </summary>
    bool IsCancellationSupported { get; }
}