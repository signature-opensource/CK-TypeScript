using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Base class for transform language parser.
/// </summary>
public abstract class BaseTransformParser : Tokenizer
{
    readonly TransformerHost _host;
    readonly TransformLanguage _language;
    // We only parse one statement at a time here: the side channel
    // doesn't need to be a collector.
    TransfomerFunction? _result;

    protected BaseTransformParser( TransformerHost host, TransformLanguage language )
    {
        _host = host;
        _language = language;
    }

    /// <summary>
    /// Gets the <see cref="TransformLanguage"/>.
    /// </summary>
    public TransformLanguage Language => _language;

    /// <summary>
    /// Transform language accepts <see cref="TriviaHeadExtensions.AcceptCLikeRecursiveStarComment(ref TriviaHead)"/>
    /// and <see cref="TriviaHeadExtensions.AcceptCLikeLineComment(ref TriviaHead)"/>.
    /// <para>
    /// This cannot be changed.
    /// </para>
    /// </summary>
    /// <param name="c">The trivia head.</param>
    protected override sealed void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeRecursiveStarComment();
        c.AcceptCLikeLineComment();
    }

    /// <summary>
    /// Implements minimal support required by any Transform language.
    /// <para>
    /// When overridden, <see cref="TokenType.GenericIdentifier"/> that at least handles "Ascii letter[Ascii letter or digit]*",
    /// <see cref="TokenType.DoubleQuote"/>, <see cref="TokenType.LessThan"/>, <see cref="TokenType.Dot"/>
    /// and <see cref="TokenType.SemiColon"/> must be handled.
    /// </para>
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The low level token.</returns>
    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
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
        if( c == '.' )
        {
            return new LowLevelToken( TokenType.Dot, 1 );
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
    /// Parses a single top-level statement that is a 'create &lt;language&gt; transformer [name] [on &lt;target&gt;] [as] begin ... end'.
    /// <para>
    /// Actual transform statements are handled by <see cref="ParseStatement(ref TokenizerHead)"/>.
    /// </para>
    /// <para>
    /// This is internal: only the <see cref="TransformerHost"/> calls this on its own transform parser language.
    /// </para>
    /// </summary>
    /// <param name="tokens">The tokens on success. Can be empty. <see cref="ImmutableArray{T}.IsDefault"/> is true on error.</param>
    /// <param name="error">The error if any.</param>
    /// <returns>True on success, false on error.</returns>
    internal TransfomerFunction? TryParse( IActivityMonitor monitor, ReadOnlyMemory<char> text )
    {
        if( !base.Tokenize( out var tokens, out var error))
        {
            if( error == null ) error = tokens.OfType<TokenError>().First();
            monitor.Error( $"""
                        Parsing error {error.ErrorMessage} - @{error.SourcePosition.Line},{error.SourcePosition.Column} while parsing:
                        {text}
                        """ );
            return null;
        }
        Throw.DebugAssert( _result != null );
        _result.Tokens = tokens;
        return _result;
    }

    /// <summary>
    /// Overridden to throw "Only ParseStatement must be implemented by Transform parsers.".
    /// </summary>
    /// <param name="tokens">Unused.</param>
    /// <param name="error">Unused.</param>
    /// <returns>Never.</returns>
    /// <exception cref="InvalidOperationException">Always thrown as this must not be used.</exception>
    protected override bool Tokenize( out ImmutableArray<Token> tokens, out TokenError? error )
    {
        throw new InvalidOperationException( "Only ParseStatement must be implemented by Transform parsers." );
    }

    /// <summary>
    /// Sealed Tokenize function: this handles the top-level 'create &lt;language&gt; transformer [name] [on &lt;target&gt;] [as] begin ... end'
    /// and is used by the <see cref="TransformerHost"/>. Specialized parsers mut override <see cref="ParseStatement(ref TokenizerHead)"/>.
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>Single hard failure is that the target language is not registered. Other errors are inlined.</returns>
    protected sealed override TokenError? Tokenize( ref TokenizerHead head )
    {
        if( head.TryMatchToken( "create", out var create ) )
        {
            var cLang = _host.Find( head.LowLevelTokenText );
            if( cLang == null )
            {
                return head.CreateError( $"Expected language name. Available languages are: '{_host.Languages.Select( l => l.LanguageName ).Concatenate( "', '" )}'." );
            }
            var language = head.CreateLowLevelToken();
            head.MatchToken( "transformer", inlineError: true );

            Token? functionName = null;
            Token? target = null;
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
                    // on
                    head.CreateLowLevelToken();
                    target = head.MatchToken( TokenType.GenericIdentifier, "target identifier" );
                }
                // The optional "as" token is parsed in the context of the transform generic language.
                head.TryMatchToken( "as", out _ );
            }
            // The begin...end is parsed in the context of the actual transform language.
            // The leading trivias will be analyzed by the TransformAnalyzer.
            var headStatements = head.CreateSubHead( out var safetyToken, cLang.TransformTokenizer );
            var statements = cLang.TransformTokenizer.ParseStatements( ref headStatements );
            head.SkipTo( safetyToken, in headStatements );
            _result = new TransfomerFunction( cLang.TransformLanguage, statements, functionName?.ToString(), target?.ToString() );
        }
        return null;
    }

    List<TransformStatement> ParseStatements( ref TokenizerHead head )
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
    /// the <see cref="InjectIntoStatementOld"/>.
    /// </para>
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The handled statements or null.</returns>
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
        var content = MatchRawString( ref head );
        head.MatchToken( "into", inlineError: true );
        var target = MatchInjectionPoint( ref head );
        head.TryMatchToken( ";", out _ );
        return content != null && target != null
                ? new InjectIntoStatement( content, target )
                : null;

        static InjectionPoint? MatchInjectionPoint( ref TokenizerHead head )
        {
            if( head.LowLevelTokenType == TokenType.LessThan )
            {
                var sHead = head.Head;
                int nameLen = TriviaInjectionPointMatcher.GetInsertionPointLength( sHead );
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
                head.CreateInlineError( "Single-line string must not contain end of line.");
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
