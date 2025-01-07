using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.TransformLanguage;

public sealed class TransformerFunction : SourceSpan
{
    public TransformerFunction( int createTokenIndex,
                                int endTokenIndex,
                                TransformLanguage language,
                                List<TransformStatement>? statements = null,
                                string? name = null,
                                string? target = null )
        : base( createTokenIndex, endTokenIndex )
    {
        Language = language;
        Name = name;
        Target = target;
        Statements = statements ?? new List<TransformStatement>();
    }

    /// <summary>
    /// Gets or sets this transformer function name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the transform language.
    /// </summary>
    public TransformLanguage Language { get; set; }

    /// <summary>
    /// Gets or sets the target address.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets the transform statements.
    /// </summary>
    public List<TransformStatement> Statements { get; }

    /// <summary>
    /// Applies the <see cref="Statements"/> to the <paramref name="code"/>.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="code">The code to transform.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Apply( IActivityMonitor monitor, SourceCodeEditor code )
    {
        bool success = true;
        using( monitor.OnError( () => success = false) )
        {
            foreach( var statement in Statements )
            {
                statement.Apply( monitor, code );
            }
        }
        return success;
    }

}
