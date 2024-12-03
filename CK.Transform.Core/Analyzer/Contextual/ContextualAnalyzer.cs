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
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>).</returns>
    protected internal override sealed IAbstractNode Parse( ref AnalyzerHead head )
    {
        return Parse( ref head, _rootContext );
    }

    /// <summary>
    /// Same as the contextless <see cref="Analyzer.ParseOne()"/> but with a <paramref name="context"/>.
    /// </summary>
    /// <param name="head">The <see cref="AnalyzerHead"/>.</param>
    /// <param name="context">The parse context.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>).</returns>
    protected abstract IAbstractNode Parse( ref AnalyzerHead head, T context );
}


