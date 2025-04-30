using CK.Core;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class ScopedTokensBuilder
{
    readonly IActivityMonitor _monitor;
    readonly SourceCodeEditor _editor;
    readonly ActivityMonitorExtension.ErrorTracker _errorTracker;
    bool _hasError;

    static readonly IEnumerable<IEnumerable<IEnumerable<SourceToken>>> _emptyResult = [[[]]];

    internal ScopedTokensBuilder( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        _monitor = monitor;
        _editor = editor;
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
    public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens => _editor.ScopedTokens.Tokens;

    /// <summary>
    /// Gets an empty scoped tokens result.
    /// Typiscally used when an error is signaled in the <see cref="Monitor"/>.
    /// </summary>
    public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> EmptyResult => _emptyResult;

    /// <summary>
    /// Gets the current language.
    /// </summary>
    public TransformerHost.Language Language => _editor.Language;

    internal void Dispose() => _errorTracker.Dispose();
}
