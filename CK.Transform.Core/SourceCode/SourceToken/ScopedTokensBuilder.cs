using CK.Core;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class ScopedTokensBuilder
{
    readonly IActivityMonitor _monitor;
    readonly TransformerHost.Language _language;
    readonly IEnumerable<IEnumerable<IEnumerable<SourceToken>>> _input;
    readonly ActivityMonitorExtension.ErrorTracker _errorTracker;
    bool _hasError;

    /// <summary>
    /// Gets an empty scoped tokens result.
    /// Typically used when an error is signaled.
    /// </summary>
    public static readonly IEnumerable<IEnumerable<IEnumerable<SourceToken>>> EmptyResult = [[[]]];

    internal ScopedTokensBuilder( IActivityMonitor monitor,
                                  IEnumerable<IEnumerable<IEnumerable<SourceToken>>> input,
                                  TransformerHost.Language language )
    {
        _monitor = monitor;
        _language = language;
        _input = input;
        _language = language;
        _errorTracker = monitor.OnError( OnError );
    }

    void OnError() => _hasError = true;

    /// <summary>
    /// Gets the monitor that must be used to signal errors.
    /// </summary>
    public IActivityMonitor Monitor => _monitor;

    /// <summary>
    /// Gets whether this builder is on error.
    /// </summary>
    public bool HasError => _hasError;

    /// <summary>
    /// Gets the current tokens to consider.
    /// </summary>
    public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens => _input;

    /// <summary>
    /// Gets the current language.
    /// </summary>
    public TransformerHost.Language Language => _language;

    internal void Dispose() => _errorTracker.Dispose();
}
