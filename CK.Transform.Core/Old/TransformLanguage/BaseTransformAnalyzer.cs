using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static CK.Transform.TransformLanguage.TransformerHostOld;

namespace CK.Transform.TransformLanguage;


public abstract class BaseTransformAnalyzer : Analyzer
{
    readonly TransformerHostOld _host;
    readonly TransformLanguageOld _language;

    public TransformLanguageOld Language => _language;

    protected BaseTransformAnalyzer( TransformerHostOld host, TransformLanguageOld language )
    {
        _host = host;
        _language = language;
    }

    public override sealed void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeRecursiveStarComment();
        c.AcceptCLikeLineComment();
    }

    /// <summary>
    /// Implements minimal support required by any Transform language:
    /// <see cref="TokenType.GenericIdentifier"/> that at least handles "Ascii letter[Ascii letter or digit]*",
    /// <see cref="TokenType.DoubleQuote"/>, <see cref="TokenType.LessThan"/> and <see cref="TokenType.SemiColon"/>
    /// must be handled.
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The low level token.</returns>
    public override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        var c = head[0];
        if( char.IsAsciiLetter( c ) )
        {
            int iS = 0;
            while( ++iS < head.Length && char.IsAsciiLetterOrDigit( head[iS] ) ) ;
            return new LowLevelToken( TokenType.GenericIdentifier, iS );
        }
        if( c == '"' )
        {
            return new LowLevelToken( TokenType.DoubleQuote, 1 );
        }
        if( c == ';' )
        {
            return new LowLevelToken( TokenType.SemiColon, 1 );
        }
        if( c == '<' )
        {
            return new LowLevelToken( TokenType.LessThan, 1 );
        }
        return default;
    }

    /// <summary>
    /// Overridden to parse the single top-level statement that is a 'create &lt;language&gt; transformer [name] [on &lt;target&gt;] [as] begin ... end'.
    /// <para>
    /// Actual transform statements are handled by <see cref="ParseStatement(ref ParserHead)"/>.
    /// </para>
    /// </summary>
    /// <param name="head">The <see cref="ParserHead"/>.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>) or null if not recognized.</returns>
    protected override sealed IAbstractNode? Parse( ref ParserHead head )
    {
        if( head.TryMatchToken( "create", out var create ) )
        {
            Language? cLang = _host.Find( head.LowLevelTokenText );
            if( cLang == null )
            {
                return head.CreateError( $"Expected language name. Available languages are: '{_host.Languages.Select( l => l.LanguageName ).Concatenate( "', '" )}'." );
            }
            var language = head.CreateLowLevelToken();
            var transformer = head.MatchToken( "transformer" );
            if( transformer is IErrorNode ) return transformer;

            TokenNode? functionName = null;
            TokenNode? on = null;
            AbstractNode? target = null;
            TokenNode? asT = null;
            if( !head.LowLevelTokenText.Equals( "begin", StringComparison.Ordinal ) )
            {
                bool hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
                if( !hasOn && !head.LowLevelTokenText.Equals( "as", StringComparison.Ordinal ) )
                {
                    functionName = head.CreateLowLevelToken();
                    hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
                }
                if( hasOn )
                {
                    on = head.CreateLowLevelToken();
                    if( head.LowLevelTokenType == TokenType.DoubleQuote )
                    {
                        target = MatchRawString( ref head );
                        if( target is not RawStringOld ) return target;
                    }
                    else if( head.LowLevelTokenType == TokenType.GenericIdentifier )
                    {
                        target = head.CreateLowLevelToken();
                    }
                    else
                    {
                        return head.CreateError( "Expecting a target after 'on' that can be a string or an identifier." );
                    }
                }
                // The optional "as" token is parsed in the context of the transform generic language.
                head.TryMatchToken( "as", out asT );
            }
            // The begin...end is parsed in the context of the actual transform language.
            // The leading trivias will be analyzed by the TransformAnalyzer.
            var headStatements = head.CreateSubHead( out var safetyToken, cLang.TransformAnalyzer );
            var endToken = cLang.TransformAnalyzer.ParseStatements( ref headStatements, out TokenNode beginT, out List<ITransformStatement> statements );
            if( endToken is not TokenNode endT ) return endToken;
            head.SkipTo( safetyToken, in headStatements );
            return new TransfomerFunctionOld( create, language, transformer, functionName, on, target, asT, beginT, new NodeList<ITransformStatement>( statements ), endT );
        }
        return null;
    }


    internal IAbstractNode ParseStatements( ref ParserHead head, out TokenNode beginT, out List<ITransformStatement> statements )
    {
        statements = new List<ITransformStatement>();
        beginT = head.MatchToken( "begin" );
        if( beginT is IErrorNode ) return beginT;
        TokenNode? endT;
        while( !head.TryMatchToken( "end", out endT ) )
        {
            var s = ParseStatement( ref head );
            if( s is IErrorNode ) return s;
            if( s == null )
            {
                return head.CreateError( $"Expecting '{_language.LanguageName}' statement." );
            }
            if( s is not ITransformStatement statement )
            {
                return Throw.InvalidOperationException<IAbstractNode>( $"Language '{_language.LanguageName}' parsed a '{s.GetType().ToCSharpName()}' that is not a ITransformStatement." );
            }
            statements.Add( statement );
        }
        return endT;
    }


    /// <summary>
    /// Handles transform statements that apply to any language:
    /// the <see cref="InjectIntoStatementOld"/>.
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The handled statements or null.</returns>
    protected virtual IAbstractNode? ParseStatement( ref ParserHead head )
    {
        if( head.TryMatchToken( "inject", out var inject ) )
        {
            return MatchInjectIntoStatement( ref head, inject );
        }
        return null;
    }

    static IAbstractNode MatchInjectIntoStatement( ref ParserHead head, TokenNode inject )
    {
        var n = MatchRawString( ref head );
        if( n is not RawStringOld content ) return n;

        var into = head.MatchToken( "into" );
        if( into is TokenErrorNode ) return into;

        n = MatchInjectionPoint( ref head );
        if( n is not InjectionPointOld target ) return n;

        var terminator = head.MatchToken( ";" );
        if( terminator is TokenErrorNode ) return terminator;

        return new InjectIntoStatementOld( inject, content, into, target, terminator );


        static AbstractNode MatchInjectionPoint( ref ParserHead head )
        {
            if( head.LowLevelTokenType == TokenType.LessThan )
            {
                var sHead = head.Head;
                int nameLen = TriviaInjectionPointMatcher.GetInsertionPointLength( sHead );
                if( nameLen > 0 && nameLen < sHead.Length && sHead[nameLen] == '>' )
                {
                    head.AcceptToken( nameLen + 1, out var text, out var leading, out var trailing );
                    return new InjectionPointOld( text, leading, trailing );
                }
            }
            return head.CreateError( "Expected <InjectionPoint>." );
        }
    }

    public static AbstractNode MatchRawString( ref ParserHead head )
    {
        if( head.LowLevelTokenType == TokenType.DoubleQuote )
        {
            var start = head.Head.TrimStart( '"' );
            var quoteCount = head.Head.Length - start.Length;
            Throw.DebugAssert( quoteCount > 0 );
            // Empty string.
            if( quoteCount == 2 )
            {
                head.AcceptToken( 2, out var text, out var leading, out var trailing );
                return new RawStringOld( text, default, leading, trailing );
            }
            if( quoteCount == 1 )
            {
                return SingleLine( ref head, start );
            }
            return PossiblyMultiLine( ref head, start, quoteCount );
        }
        return head.CreateError( "Expected string." );

        static AbstractNode SingleLine( ref ParserHead head, ReadOnlySpan<char> start )
        {
            int idxE = start.IndexOf( '"' );
            if( idxE < 0 ) return head.CreateError( "Unterminated string." );
            start = start.Slice( 0, idxE );
            if( start.Contains( '\n' ) ) return head.CreateError( "Single-line string must not contain end of line." );
            head.AcceptToken( 2 + start.Length, out var text, out var leading, out var trailing );
            return new RawStringOld( text, text.Slice( 1, start.Length ), leading, trailing );
        }

        static AbstractNode PossiblyMultiLine( ref ParserHead head, ReadOnlySpan<char> start, int quoteCount )
        {
            int idxE = start.IndexOf( head.Head.Slice( 0, quoteCount ) );
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
            return new RawStringOld( text, text.Slice( quoteCount, start.Length ), leading, trailing );
        }

        static AbstractNode MultiLine( ref ParserHead head, ReadOnlySpan<char> multiLine, int quoteCount, int idxFirstEndOfLine )
        {
            int contentLength = multiLine.Length;
            ReadOnlySpan<char> mustBeEmpty;
            if( idxFirstEndOfLine > 0 )
            {
                mustBeEmpty = multiLine.Slice( 0, idxFirstEndOfLine );
                if( mustBeEmpty.ContainsAnyExcept( " \r\t" ) )
                {
                    return head.CreateError( $"Invalid multi-line raw string: there must be no character after the opening {head.Head.Slice( 0, quoteCount )} characters." );
                }
                multiLine = multiLine.Slice( idxFirstEndOfLine + 1 );
            }
            int idxLastEndOfLine = multiLine.LastIndexOf( '\n' );
            if( idxLastEndOfLine < 0 )
            {
                return head.CreateError( $"Invalid multi-line raw string: at least one line must appear between the {head.Head.Slice( 0, quoteCount )}." );
            }
            mustBeEmpty = multiLine.Slice( idxLastEndOfLine + 1 );
            if( mustBeEmpty.ContainsAnyExcept( " \t" ) )
            {
                return head.CreateError( $"Invalid multi-line raw string: there must be no character on the line before the closing {head.Head.Slice( 0, quoteCount )} characters." );
            }
            int prefixLength = mustBeEmpty.Length;
            multiLine = multiLine.Slice( 0, multiLine.Length - prefixLength );
            var mLine = head.Text.Slice( head.Text.Length - head.Head.Length + quoteCount + idxFirstEndOfLine + 1, multiLine.Length );
            var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();

            // EnumerateLines normalizes the EOL. One cannot update a position
            // without inspecting the actual EOL, so we use Overlaps to obtain
            // the offset in the ReadOnlyMemory.
            bool hasEmptyLine = false;
            foreach( var line in multiLine.EnumerateLines() )
            {
                if( hasEmptyLine )
                {
                    builder.Add( default );
                    hasEmptyLine = false;
                }
                if( line.Length > prefixLength )
                {
                    if( line.Slice( 0, prefixLength ).ContainsAnyExcept( " \t" ) )
                    {
                        return head.CreateError( $"Invalid multi-line raw string: there must be no character before column {prefixLength} in '{line}'." );
                    }
                    Throw.DebugAssert( multiLine.Overlaps( line ) );
                    multiLine.Overlaps( line, out var pos );
                    builder.Add( mLine.Slice( pos + prefixLength, line.Length - prefixLength ) );
                }
                else
                {
                    if( line.ContainsAnyExcept( " \t" ) )
                    {
                        return head.CreateError( $"Invalid multi-line raw string: there must be no character before column {prefixLength}." );
                    }
                    hasEmptyLine = true;
                }
            }
            head.AcceptToken( 2 * quoteCount + contentLength, out var text, out var leading, out var trailing );
            return new RawStringOld( text, text.Slice( quoteCount, contentLength - quoteCount ), builder.DrainToImmutable(), leading, trailing );
        }
    }

}
