using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Base class for transform language analyzer: this parses <see cref="TransformStatement"/>.
/// <para>
/// Specializations can implement <see cref="ILowLevelTokenizer"/> if the transform language
/// requires more than the default low level tokens handled by <see cref="TransformerHost.RootTransformAnalyzer.LowLevelTokenize(ReadOnlySpan{char})"/>.
/// </para>
/// </summary>
public abstract class TransformStatementAnalyzer
{
    readonly TransformLanguage _language;

    /// <summary>
    /// Initializes a new analyzer.
    /// </summary>
    /// <param name="language">The transform language.</param>
    protected TransformStatementAnalyzer( TransformLanguage language )
    {
        _language = language;
    }

    /// <summary>
    /// Gets the <see cref="TransformLanguage"/>.
    /// </summary>
    public TransformLanguage Language => _language;

    /// <summary>
    /// Parses a "begin ... end" block. Statements are parsed by <see cref="ParseStatement(ref TokenizerHead)"/>.
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The list of transform statements.</returns>
    internal List<TransformStatement> ParseStatements( ref TokenizerHead head )
    {
        var statements = new List<TransformStatement>();
        head.MatchToken( "begin", inlineError: true );
        Token? foundEnd = null;
        while( head.EndOfInput == null && !head.TryMatchToken( "end", out foundEnd ) )
        {
            var s = ParseStatement( ref head );
            if( s != null )
            {
                statements.Add( s );
            }
            else
            {
                head.CreateInlineError( $"Failed to parse a transform '{_language.LanguageName}' language statement." );
                break;
            }
        }
        if( foundEnd == null ) head.CreateInlineError( "Expected 'end'." );
        return statements;
    }

    /// <summary>
    /// Must implement transform specific statement parsing.
    /// Parsing errors should be inlined <see cref="TokenError"/>. 
    /// <para>
    /// At this level, this handles transform statements that apply to any language:
    /// the <see cref="InjectIntoStatement"/>.
    /// </para>
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The parsed statement or null.</returns>
    protected virtual TransformStatement? ParseStatement( ref TokenizerHead head )
    {
        if( head.TryMatchToken( "inject", out var inject ) )
        {
            return MatchInjectIntoStatement( ref head, inject );
        }
        return null;
    }

    static InjectIntoStatement? MatchInjectIntoStatement( ref TokenizerHead head, Token inject )
    {
        int startSpan = head.LastTokenIndex;
        var content = MatchRawString( ref head );
        head.MatchToken( "into", inlineError: true );
        var target = MatchInjectionPoint( ref head );
        head.TryMatchToken( ";", out _ );
        return content != null && target != null
                ? new InjectIntoStatement( startSpan, head.LastTokenIndex, content, target )
                : null;

        static InjectionPoint? MatchInjectionPoint( ref TokenizerHead head )
        {
            if( head.LowLevelTokenType == TokenType.LessThan )
            {
                var sHead = head.Head;
                int nameLen = TriviaInjectionPointMatcher.GetInjectionPointLength( sHead );
                if( nameLen > 0 && nameLen < sHead.Length && sHead[nameLen] == '>' )
                {
                    head.PreAcceptToken( nameLen + 1, out var text, out var leading, out var trailing );
                    return head.Accept( new InjectionPoint( text, leading, trailing ) );
                }
            }
            head.CreateInlineError( "Expected <InjectionPoint>." );
            return null;
        }
    }

    public static RawString? MatchRawString( ref TokenizerHead head )
    {
        if( head.LowLevelTokenType == TokenType.DoubleQuote )
        {
            var start = head.Head.TrimStart( '"' );
            var quoteCount = head.Head.Length - start.Length;
            Throw.DebugAssert( quoteCount > 0 );
            // Empty string.
            if( quoteCount == 2 )
            {
                head.PreAcceptToken( 2, out var text, out var leading, out var trailing );
                return head.Accept( new RawString( text, default, leading, trailing ) );
            }
            if( quoteCount == 1 )
            {
                return SingleLine( ref head, start );
            }
            return PossiblyMultiLine( ref head, start, quoteCount );
        }
        head.CreateError( "Expected string.", true );
        return null;

        static RawString? SingleLine( ref TokenizerHead head, ReadOnlySpan<char> start )
        {
            int idxE = start.IndexOf( '"' );
            if( idxE < 0 )
            {
                head.CreateInlineError( "Unterminated string." );
                return null;
            }
            start = start.Slice( 0, idxE );
            if( start.Contains( '\n' ) )
            {
                head.CreateInlineError( "Single-line string must not contain end of line." );
                return null;
            }
            head.PreAcceptToken( 2 + start.Length, out var text, out var leading, out var trailing );
            return head.Accept( new RawString( text, text.Slice( 1, start.Length ), leading, trailing ) );
        }

        static RawString? PossiblyMultiLine( ref TokenizerHead head, ReadOnlySpan<char> start, int quoteCount )
        {
            int idxE = start.IndexOf( head.Head.Slice( 0, quoteCount ) );
            if( idxE < 0 )
            {
                head.CreateInlineError( "Unterminated string." );
                return null;
            }
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
                if( ++offset >= quoteCount )
                {
                    head.CreateInlineError( "Invalid raw string terminator: too many closing \"." );
                    return null;
                }
            }
            idxE += offset;
            start = start.Slice( 0, idxE );
            head.PreAcceptToken( 2 * quoteCount + start.Length, out var text, out var leading, out var trailing );
            return head.Accept( new RawString( text, text.Slice( quoteCount, start.Length ), leading, trailing ) );
        }

        static RawString? MultiLine( ref TokenizerHead head, ReadOnlySpan<char> multiLine, int quoteCount, int idxFirstEndOfLine )
        {
            int contentLength = multiLine.Length;
            ReadOnlySpan<char> mustBeEmpty;
            if( idxFirstEndOfLine > 0 )
            {
                mustBeEmpty = multiLine.Slice( 0, idxFirstEndOfLine );
                if( mustBeEmpty.ContainsAnyExcept( " \r\t" ) )
                {
                    head.CreateInlineError( $"Invalid multi-line raw string: there must be no character after the opening {head.Head.Slice( 0, quoteCount )} characters." );
                    return null;
                }
                multiLine = multiLine.Slice( idxFirstEndOfLine + 1 );
            }
            int idxLastEndOfLine = multiLine.LastIndexOf( '\n' );
            if( idxLastEndOfLine < 0 )
            {
                head.CreateInlineError( $"Invalid multi-line raw string: at least one line must appear between the {head.Head.Slice( 0, quoteCount )}." );
                return null;
            }
            mustBeEmpty = multiLine.Slice( idxLastEndOfLine + 1 );
            if( mustBeEmpty.ContainsAnyExcept( " \t" ) )
            {
                head.CreateInlineError( $"Invalid multi-line raw string: there must be no character on the line before the closing {head.Head.Slice( 0, quoteCount )} characters." );
                return null;
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
                        head.CreateInlineError( $"Invalid multi-line raw string: there must be no character before column {prefixLength} in '{line}'." );
                        return null;
                    }
                    Throw.DebugAssert( multiLine.Overlaps( line ) );
                    multiLine.Overlaps( line, out var pos );
                    builder.Add( mLine.Slice( pos + prefixLength, line.Length - prefixLength ) );
                }
                else
                {
                    if( line.ContainsAnyExcept( " \t" ) )
                    {
                        head.CreateInlineError( $"Invalid multi-line raw string: there must be no character before column {prefixLength}." );
                        return null;
                    }
                    hasEmptyLine = true;
                }
            }
            head.PreAcceptToken( 2 * quoteCount + contentLength, out var text, out var leading, out var trailing );
            return head.Accept( new RawString( text, text.Slice( quoteCount, contentLength - quoteCount ), builder.DrainToImmutable(), leading, trailing ) );
        }
    }
}
