using CK.Core;
using CK.Transform.TransformLanguage;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// Non generic <see cref="IAnalyzerResult{T}"/>.
/// </summary>
public interface IAnalyzerResult
{
    /// <summary>
    /// Gets whether <see cref="Error"/> is null and no <see cref="TokenError"/> exist in the <see cref="Tokens"/>.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets a potentially complex result if this analyzer is more than a <see cref="Tokenizer"/>.
    /// This is typically the root node of an Abstract Syntax Tree.
    /// </summary>
    object? Result { get; }

    /// <summary>
    /// Gets a "hard failure" error that stopped the analysis.
    /// </summary>
    TokenError? Error { get; }

    /// <summary>
    /// Gets the tokens. Empty if <see cref="Error"/> is not null.
    /// </summary>
    ImmutableArray<Token> Tokens { get; }
}

/// <summary>
/// Node abstraction.
/// </summary>
public interface ITokenNode
{
    /// <summary>
    /// Gets the number of tokens
    /// </summary>
    int Width { get; }

    ITokenNode? Visit( TokenNodeVisitor visitor );
}

public class RootNode : ITokenNode
{
    readonly ImmutableArray<Token> _tokens;
    readonly ImmutableArray<ITokenNode> _nodes;

    internal RootNode( ImmutableArray<Token> tokens, ImmutableArray<ITokenNode> nodes )
    {
        Throw.DebugAssert( nodes.IsDefault || nodes.Sum( n => n.Width ) == tokens.Length );
        _tokens = tokens;
        _nodes = nodes;
    }

    public int Width => _tokens.Length;

    public ITokenNode? Visit( TokenNodeVisitor visitor )
    {
        if( _nodes.IsDefault )
        {
            ImmutableArray<ITokenNode>.Builder? b = null;
            int idx = 0;
            foreach( var t in _tokens )
            {
                var t2 = t.Visit( visitor );
                if( t2 != t )
                {
                    if( b == null )
                    {
                        b = ImmutableArray.CreateBuilder<ITokenNode>( _tokens.Length );
                        b.AddRange()
                    }
                }
                idx++;
            }
        }
    }
}

public abstract class TokenNodeVisitor
{
    public Token? VisitToken( Token t ) => t;
}
