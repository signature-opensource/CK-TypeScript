using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Composite of one or more parsers.
/// <para>
/// When only one parser is provided, this composite is "transparent" and acts as a validator:
/// returned node and memory is checked in depth to throw on any invalid behavior.
/// </para>
/// </summary>
public sealed class CompositeParser : IParser
{
    readonly ImmutableArray<IParser> _parsers;
    readonly string _name;

    public CompositeParser( params ImmutableArray<IParser> parsers )
    {
        Throw.CheckArgument( parsers.Length > 0 );
        _parsers = parsers;
        _name = _parsers.Select( p => p.ToString() ).Concatenate( ", " );
    }

    /// <summary>
    /// Returns a single <see cref="IAbstractNode"/> that can be a <see cref="NodeList{T}"/> of 
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public IAbstractNode Parse( ref ReadOnlyMemory<char> text )
    {
        AbstractNode? singleResult = null;
        List<AbstractNode>? multiResult = null;
        foreach( var p in _parsers )
        {
            var head = text;
            var node = GetSafeNode( p, head );
            if( !node.TokenType.IsError() )
            {
                CheckMemory( p, text, head, node, true );
                if( singleResult == null ) singleResult = node;
                else
                {
                    multiResult ??= new List<AbstractNode>() { singleResult };
                    multiResult.Add( node );
                }
                text = head;
            }
            else
            {
                // On error, text may have been forwarded or not.
                // We keep the current text where it is.
                CheckMemory( p, text, head, node, false );
            }
            // On the end, stop calling any more parsers.
            if( node.TokenType == TokenType.EndOfInput ) break;
        }
        if( multiResult != null ) return new NodeList<AbstractNode>( multiResult );
        if( singleResult != null ) return singleResult;
        return new TokenErrorNode( TokenType.EndOfInput, $"Parser '{_name}' found nothig to parse." );


        static AbstractNode GetSafeNode( IParser p, ReadOnlyMemory<char> head )
        {
            var n = p.Parse( ref head );
            if( n == null )
            {
                return Throw.InvalidOperationException<AbstractNode>( $"""
                                                                      Parser '{p}' returned a null node on:
                                                                      {head}
                                                                      """ );
            }
            if( n is not AbstractNode node )
            {
                return Throw.InvalidOperationException<AbstractNode>( $"""
                                                                      Parser '{p}' returned a node of type '{n.GetType().ToCSharpName()}' that is not an AbstractNode on:
                                                                      {head}
                                                                      """ );
            }
            return node;
        }

        static void CheckMemory( IParser p,
                                 ReadOnlyMemory<char> text,
                                 ReadOnlyMemory<char> head,
                                 AbstractNode node,
                                 bool success )
        {
            var eaten = text.Length - head.Length;
            if( success && eaten == 0 )
            {
                Throw.InvalidOperationException( $"""
                                                     Parser '{p}' returned a successful '{node.GetType().ToCSharpName()}' node but did not forward the text on:
                                                     {head}
                                                     """ );
            }
            //
            // Note: ReadOnlySpan<T> operator == tests if two ReadOnlySpan<T> instances point to the same starting memory location,
            // and have the same Length values. This does not compare the contents of two ReadOnlySpan<T> instances.
            // And this is exactly what we need.
            //
            if( eaten < 0 || text.Slice( eaten ).Span != head.Span )
            {
                Throw.InvalidOperationException( $"""
                                                     Parser '{p}' returned a '{node.GetType().ToCSharpName()}' but updated the text with another memory block.
                                                     Input text:
                                                     {text}
                                                     Updated text:
                                                     {head}
                                                     """ );
            }
        }
    }

    public override string ToString() => _name;
}
