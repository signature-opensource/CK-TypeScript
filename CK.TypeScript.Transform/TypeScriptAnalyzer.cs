using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.TypeScript.Transform;

sealed partial class TypeScriptAnalyzer : Analyzer
{
    TokenNode? _lastToken;
    // Keeps the brace depth of interpolated starts.
    readonly Stack<int> _interpolated;
    int _braceDepth;

    public TypeScriptAnalyzer()
    {
        _interpolated = new Stack<int>();
    }

    public override void Reset( ReadOnlyMemory<char> text )
    {
        _lastToken = null;
        _braceDepth = 0;
        _interpolated.Clear();
        base.Reset( text );
    }

    public override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeLineComment();
        c.AcceptCLikeStarComment();
    }

    TokenNode Scan( ref ParserHead head )
    {
        switch( head.LowLevelTokenType )
        {
            case NodeType.None:
                _lastToken = head.CreateError( "Unrecognized token." );
                break;
            case NodeType.GenericInterpolatedStringStart:
                _lastToken = head.CreateLowLevelToken();
                _interpolated.Push( ++_braceDepth );
                break;
            case NodeType.OpenBrace:
                _lastToken = head.CreateLowLevelToken();
                ++_braceDepth;
                break;
            case NodeType.CloseBrace:
                if( _interpolated.TryPeek( out var depth ) && depth == _braceDepth )
                {
                    var t = ReadInterpolatedSegment( head.Head, false );
                    if( t.NodeType == NodeType.GenericInterpolatedStringEnd )
                    {
                        --_braceDepth;
                    }
                    _interpolated.Pop();
                    _lastToken = head.CreateToken( t.NodeType, t.Length );
                }
                else
                {
                    _lastToken = --_braceDepth < 0
                                    ? head.CreateError( "Unbalanced {{brace}." )
                                    : head.CreateLowLevelToken();
                }
                break;
            case NodeType.Slash or NodeType.SlashEquals:
                if( _lastToken != null )
                {
                    var type = _lastToken.NodeType;
                    if( type is not NodeType.GenericIdentifier
                            and not NodeType.GenericNumber
                            and not NodeType.GenericString
                            and not NodeType.GenericRegularExpression
                            and not NodeType.PlusPlus
                            and not NodeType.MinusMinus
                            and not NodeType.CloseParen
                            and not NodeType.CloseBrace
                            and not NodeType.CloseBracket )
                    {
                        var t = TryParseRegex( new LowLevelToken( head.LowLevelTokenType, head.LowLevelTokenText.Length ), head.Head );
                        _lastToken = head.CreateToken( t.NodeType, t.Length );
                    }
                    else
                    {
                        _lastToken = head.CreateLowLevelToken();
                    }
                }
                else
                {
                    _lastToken = head.CreateLowLevelToken();
                }
                break;
            default:
                _lastToken = head.CreateLowLevelToken();
                break;
        }
        return _lastToken;
    }

    protected override IAbstractNode? Parse( ref ParserHead head )
    {
        var b = ImmutableArray.CreateBuilder<AbstractNode>();
        for( ; ; )
        {
            var t = Scan( ref head );
            if( t is TokenErrorNode ) return t;
            if( t is EndOfInputToken )
            {
                return b.Count > 0
                        ? new RawNodeList( NodeType.SyntaxNode, b.DrainToImmutable() )
                        : t;
            }
            b.Add( t );
        }
    }
}

