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
    /// Overridden to call <see cref="Parse(ref ParserHead, T)"/> with
    /// the root context.
    /// </summary>
    /// <param name="head">The <see cref="ParserHead"/>.</param>
    /// <returns>A node (can be an error node) or null if not recognized.</returns>
    protected override sealed IAbstractNode? Parse( ref ParserHead head ) => Parse( ref head, _rootContext );

    /// <summary>
    /// Parser with <paramref name="context"/>.
    /// </summary>
    /// <param name="head">The <see cref="ParserHead"/>.</param>
    /// <param name="context">The parse context.</param>
    /// <returns>A node (can be an error node) or null if not recognized.</returns>
    protected abstract IAbstractNode? Parse( ref ParserHead head, T context );
}


