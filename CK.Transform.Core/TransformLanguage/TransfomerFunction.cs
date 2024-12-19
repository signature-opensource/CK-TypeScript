using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.TransformLanguage;

public sealed class TransfomerFunction
{
    public TransfomerFunction( TransformLanguage language, List<TransformStatement>? statements = null, string? name = null, string? target = null )
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
    /// Gets the initial tokens if this transformer has been parsed.
    /// </summary>
    public ImmutableArray<Token> Tokens { get; internal set; }
}
