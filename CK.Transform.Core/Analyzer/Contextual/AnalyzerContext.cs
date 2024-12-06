using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Parser context for the <see cref="ContextualAnalyzer{T}"/>.
/// </summary>
/// <typeparam name="TSelf">This type (Curiously recurring template pattern).</typeparam>
public abstract class AnalyzerContext<TSelf> where TSelf : AnalyzerContext<TSelf>
{
    readonly TSelf? _parent;

    /// <summary>
    /// Initializes a new root context.
    /// </summary>
    protected AnalyzerContext()
    {
    }

    /// <summary>
    /// Initializes a new sunbrdinated context.
    /// </summary>
    protected AnalyzerContext( TSelf parent )
    {
        Throw.CheckArgument( parent is not null );
        _parent = parent;
    }

    /// <summary>
    /// Called on <see cref="ContextualAnalyzer{T}.Reset(ReadOnlyMemory{char})"/>.
    /// </summary>
    /// <param name="analyzer">The analyzer.</param>
    internal protected virtual void OnResetAnalyzer( Analyzer analyzer )
    {
        Throw.CheckState( "Can only be called on the root context.", _parent is null );
    }

    /// <summary>
    /// Gets the parent context. Null for the root.
    /// </summary>
    public TSelf? Parent => _parent;

}
