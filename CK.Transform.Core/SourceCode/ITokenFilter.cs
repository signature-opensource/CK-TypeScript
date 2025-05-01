using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class ScopedTokens : IEnumerable<IEnumerable<IEnumerable<SourceToken>>>
{
    readonly IEnumerable<IEnumerable<IEnumerable<SourceToken>>> _inner;
    readonly IActivityMonitor _monitor;
    readonly ActivityMonitorExtension.ErrorTracker _errorTracker;
    bool _hasError;

    internal ScopedTokens( IActivityMonitor monitor, IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        _monitor = monitor;
        _inner = inner;
        _errorTracker = monitor.OnError( OnError );
    }

    void OnError() => _hasError = true;

    internal bool Release()
    {
        _errorTracker.Dispose();
        return _hasError;
    }

    /// <summary>
    /// Gets whether this enumerable is on error.
    /// </summary>
    public bool HasError => _hasError;

    /// <inheritdoc />
    public IEnumerator<IEnumerable<IEnumerable<SourceToken>>> GetEnumerator() => _inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
}

public interface IScopedTokenProvider
{

}

/// <summary>
/// Provides filtering token capabilities.
/// </summary>
public interface ITokenFilter
{
    /// <summary>
    /// Computes scoped tokens from <see cref="ScopedTokensBuilder.Tokens"/>.
    /// </summary>
    /// <param name="builder">The builder to use.</param>
    /// <returns>The result. Must be ignored when <see cref="ScopedTokensBuilder.HasError"/> is true.</returns>
    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> GetScopedTokens( ScopedTokensBuilder builder );
}
