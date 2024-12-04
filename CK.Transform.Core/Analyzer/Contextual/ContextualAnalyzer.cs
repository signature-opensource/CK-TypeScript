using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.Core;

/// <summary>
/// Contextual analyzer supports an associated context while parsing.
/// </summary>
/// <typeparam name="T">The context type.</typeparam>
public abstract class ContextualAnalyzer<T> : Analyzer where T : AnalyzerContext<T>, new()
{
    readonly T _rootContext;

    /// <summary>
    /// Initializes a new contextual analyzer.
    /// </summary>
    protected ContextualAnalyzer()
    {
        _rootContext = new T();
    }

    /// <summary>
    /// Gets the root context.
    /// </summary>
    protected T RootContext => _rootContext;

    /// <inheritdoc />
    public override void Reset( ReadOnlyMemory<char> text )
    {
        base.Reset( text );
        _rootContext.OnResetAnalyzer( this );
    }

    /// <summary>
    /// Overridden to call the contextualized <see cref="Parse(ref AnalyzerHead, T)"/> with
    /// the root context.
    /// </summary>
    /// <param name="head">The <see cref="AnalyzerHead"/>.</param>
    /// <param name="newBehavior">Optional behavior that will be set after the parse.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>).</returns>
    public override sealed IAbstractNode Parse( ref AnalyzerHead head, IAnalyzerBehavior? newBehavior = null )
    {
        return Parse( ref head, _rootContext, newBehavior );
    }

    /// <summary>
    /// Same as the contextless <see cref="Analyzer.Parse(ref AnalyzerHead, IAnalyzerBehavior?)"/>
    /// but with a <paramref name="context"/>.
    /// </summary>
    /// <param name="head">The <see cref="AnalyzerHead"/>.</param>
    /// <param name="context">The parse context.</param>
    /// <param name="newBehavior">Optional behavior that will be set after the parse.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>).</returns>
    public abstract IAbstractNode Parse( ref AnalyzerHead head, T context, IAnalyzerBehavior? newBehavior = null );
}


