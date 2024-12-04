using CK.Core;
using CK.Transform.Core;
using CK.Transform.ErrorTolerant;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CK.Transform.TransformLanguage;


public abstract class BaseTransformAnalyzer : Analyzer
{
    readonly TransformLanguage _language;

    public TransformLanguage Language => _language;

    protected BaseTransformAnalyzer( TransformLanguage language )
    {
        _language = language;
    }

    public override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptRecursiveStartComment();
        c.AcceptLineComment();
    }

    public override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        var c = head[0];
        if( char.IsAsciiLetter( c ) )
        {
            int iS = 0;
            while( ++iS < head.Length && char.IsAsciiLetter( head[iS] ) ) ;
            return new LowLevelToken( NodeType.GenericIdentifier, iS );
        }
        if( c == '"' )
        {
            return new LowLevelToken( NodeType.DoubleQuote, 1 );
        }
        if( c == '<' )
        {
            return new LowLevelToken( NodeType.OpenAngleBracket, 1 );
        }
        return default;
    }

    public override IAbstractNode Parse( ref AnalyzerHead head )
    {
        if( head.AcceptLowLevelToken( "inject", out var inject ) )
        {
            return MatchInjectIntoStatement( ref head, inject );
        }
        return TokenErrorNode.Unhandled;
    }

    static IAbstractNode MatchInjectIntoStatement( ref AnalyzerHead head, TokenNode inject )
    {
        var n = MatchRawString( ref head );
        if( n is not RawString content ) return n;

        var into = head.MatchToken( "into" );
        if( into is TokenErrorNode ) return into;

        n = MatchInjectionPoint( ref head );
        if( n is not InjectionPoint target ) return n;

        var terminator = head.MatchToken( ";" );
        if( terminator is TokenErrorNode ) return terminator;

        return new InjectIntoStatement( inject, content, into, target, terminator );


        static AbstractNode MatchInjectionPoint( ref AnalyzerHead head )
        {
            if( head.LowLevelToken.NodeType == NodeType.OpenAngleBracket )
            {
                var sHead = head.Head;
                int iS = 0;
                while( ++iS < sHead.Length && char.IsAsciiLetterOrDigit( sHead[iS] ) ) ;
                if( iS < sHead.Length && sHead[iS] == '>' )
                {
                    head.AcceptToken( iS, out var text, out var leading, out var trailing );
                    return new InjectionPoint( text, leading, trailing );
                }
            }
            return head.CreateError( "Expected <InjectionPoint>." );
        }
    }

    public static AbstractNode MatchRawString( ref AnalyzerHead head )
    {
        if( head.LowLevelToken.NodeType == NodeType.DoubleQuote )
        {
            var start = head.Head.TrimStart( '"' );
            var quoteCount = head.Head.Length - start.Length;
            Throw.DebugAssert( quoteCount > 0 );
            // Empty string.
            if( quoteCount == 2 )
            {
                head.AcceptToken( 2, out var text, out var leading, out var trailing );
                return new RawString( text, default, leading, trailing );
            }
            if( quoteCount == 1 )
            {
                return SingleLine( ref head, start );
            }
            return PossiblyMultiLine( ref head, start, quoteCount );
        }
        return head.CreateError( "Expected string." );

        static AbstractNode SingleLine( ref AnalyzerHead head, ReadOnlySpan<char> start )
        {
            int idxE = start.IndexOf( '"' );
            if( idxE < 0 ) return head.CreateError( "Unterminated string." );
            start = start.Slice( 0, idxE );
            if( start.Contains( '\n' ) ) return head.CreateError( "Single-line string must not contain end of line." );
            head.AcceptToken( 2 + start.Length, out var text, out var leading, out var trailing );
            return new RawString( text, text.Slice( 1, start.Length - 1 ), leading, trailing );
        }

        static AbstractNode PossiblyMultiLine( ref AnalyzerHead head, ReadOnlySpan<char> start, int quoteCount )
        {
            int idxE = start.IndexOf( start.Slice( 0, quoteCount ) );
            if( idxE < 0 ) return head.CreateError( "Unterminated string." );
            var lineOrMultiLine = start.Slice( 0, idxE );
            int idxFirstEndOfLine = lineOrMultiLine.IndexOf( "\n" );
            if( idxFirstEndOfLine >= 0 )
            {
                return MultiLine( ref head, lineOrMultiLine, quoteCount, idxFirstEndOfLine );
            }
            // Single line case.
            int idxEndQuotes = idxE + quoteCount;
            // Kindly offset the end to handle """raw ""string""""" as |raw ""string""|.
            int offset = 0;
            while( idxEndQuotes < start.Length && start[idxEndQuotes] == '"' )
            {
                idxEndQuotes++;
                if( ++offset >= quoteCount ) return head.CreateError( "Invalid raw string terminator: too many closing \"." );
            }
            idxE += offset;
            start = start.Slice( 0, idxE );
            head.AcceptToken( 2*quoteCount + start.Length, out var text, out var leading, out var trailing );
            return new RawString( text, text.Slice( quoteCount, start.Length - quoteCount ), leading, trailing );
        }

        static AbstractNode MultiLine( ref AnalyzerHead head, ReadOnlySpan<char> multiLine, int quoteCount, int idxFirstEndOfLine )
        {
            ReadOnlySpan<char> mustBeEmpty;
            if( idxFirstEndOfLine > 0 )
            {
                mustBeEmpty = multiLine.Slice( 0, idxFirstEndOfLine );
                if( mustBeEmpty.ContainsAnyExcept( " \r\t" ) )
                {
                    return head.CreateError( $"Invalid multi-line raw string: there must be no character after the opening {head.Head.Slice( 0, quoteCount )} characters." );
                }
            }
            int idxLastEndOfLine = multiLine.LastIndexOf( '\n' );
            Throw.DebugAssert( idxLastEndOfLine > 0 );
            if( idxLastEndOfLine == idxFirstEndOfLine )
            {
                return head.CreateError( $"Invalid multi-line raw string: at least one line must appear between the {head.Head.Slice( 0, quoteCount )}." );
            }
            mustBeEmpty = multiLine.Slice( idxLastEndOfLine );
            if( mustBeEmpty.ContainsAnyExcept( " \t" ) )
            {
                return head.CreateError( $"Invalid multi-line raw string: there must be no character on the line before the closing {head.Head.Slice( 0, quoteCount )} characters." );
            }
            multiLine = multiLine.Slice( idxFirstEndOfLine, idxLastEndOfLine - idxFirstEndOfLine );
            var mLine = head.Head.Slice( quoteCount + idxFirstEndOfLine, multiLine.Length );
            int prefixLength = mustBeEmpty.Length;
            int i;
            var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
            
            while( (i = multiLine.IndexOf( "\n" )) >= 0 )
            {

                multiLine = multiLine.Slice( i + 1 );
            }
            throw new NotImplementedException();
        }
    }

}
