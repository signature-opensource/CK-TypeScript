using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Transform.Core;


public sealed partial class TransformerHost
{
    /// <summary>
    /// Transform language analyzer itself. This handles the top-level 'create &lt;language&gt; transformer [name] [on &lt;target&gt;] [as] begin ... end'.
    /// Statements analysis is delegated to the <see cref="Language.TransformStatementAnalyzer"/>.
    /// </summary>
    sealed class RootTransformAnalyzer : Tokenizer, ITopLevelAnalyzer<TransformerFunction>, ITargetAnalyzer
    {
        readonly TransformerHost _host;

        public RootTransformAnalyzer( TransformerHost host )
        {
            _host = host;
        }

        /// <summary>
        /// Gets the "Transform" language name.
        /// </summary>
        public string LanguageName => _transformLanguageName;

        /// <summary>
        /// Transform languages accept <see cref="TriviaHeadExtensions.AcceptCLikeRecursiveStarComment(ref TriviaHead)"/>
        /// and <see cref="TriviaHeadExtensions.AcceptCLikeLineComment(ref TriviaHead)"/>.
        /// <para>
        /// This cannot be changed: only the <see cref="ILowLevelTokenizer"/> can be optionnaly supported by a <see cref="TransformStatementAnalyzer"/>.
        /// </para>
        /// </summary>
        /// <param name="c">The trivia head.</param>
        protected override void ParseTrivia( ref TriviaHead c )
        {
            c.AcceptCLikeRecursiveStarComment();
            c.AcceptCLikeLineComment();
        }

        /// <summary>
        /// Calls the public <see cref="TransformLanguage.MinimalTransformerLowLevelTokenize(ReadOnlySpan{char})"/> that
        /// implements minimal support required by any Transform language.
        /// </summary>
        /// <param name="head">The head.</param>
        /// <returns>The low level token.</returns>
        protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head ) => TransformLanguage.MinimalTransformerLowLevelTokenize( head );

        /// <summary>
        /// Handles the top-level 'create &lt;language&gt; transformer [name] [on &lt;target&gt;] [as] begin ... end'
        /// and is used by the <see cref="TransformerHost"/>.
        /// <para>
        /// This doesn't forward the head and doesn't add errors if the text doesn't start with a <c>create</c> token.
        /// </para>
        /// </summary>
        /// <param name="head">The head.</param>
        protected override void Tokenize( ref TokenizerHead head )
        {
            int begText = head.RemainingTextIndex;
            if( !head.TryAcceptToken( "create", out _ ) )
            {
                return;
            }
            int startFunction = head.LastTokenIndex;
            var cLang = _host.FindLanguage( head.LowLevelTokenText, withFileExtensions: false );
            if( cLang == null )
            {
                head.AppendError( $"Expected language name. Available languages are: '{_host.Languages.Select( l => l.LanguageName ).Concatenate( "', '" )}'.", 0 );
                return;
            }
            var language = head.AcceptLowLevelToken();
            head.MatchToken( "transformer" );

            Token? functionName = null;
            string? target = null;
            if( !head.LowLevelTokenText.Equals( "begin", StringComparison.Ordinal ) )
            {
                bool hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
                if( !hasOn && !head.LowLevelTokenText.Equals( "as", StringComparison.Ordinal ) )
                {
                    functionName = head.AcceptLowLevelToken();
                    hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
                }
                if( hasOn )
                {
                    // Eats "on".
                    head.AcceptLowLevelToken();
                    // Either an identifier or a single-line string.
                    if( head.LowLevelTokenType == TokenType.GenericIdentifier )
                    {
                        target = head.AcceptLowLevelToken().ToString();
                    }
                    else if( head.LowLevelTokenType == TokenType.DoubleQuote )
                    {
                        target = RawString.Match( ref head, maxLineCount: 1 )?.Lines[0];
                    }
                    else
                    {
                        if( head.LowLevelTokenText.Equals( "as", StringComparison.Ordinal )
                            || head.LowLevelTokenText.Equals( "begin", StringComparison.Ordinal ) )
                        {
                            head.AppendMissingToken( "target (identifier or one-line string)" );
                        }
                        else
                        {
                            head.AppendUnexpectedToken();
                        }
                    }
                }
                // The optional "as" token is parsed in the context of the root transform language.
                head.TryAcceptToken( "as", out _ );
            }
            // The begin...end is parsed in the context of the actual transform language.
            var headStatements = head.CreateSubHead( out var safetyToken, cLang.TransformStatementAnalyzer as ILowLevelTokenizer );
            var statements = TransformStatementBlock.Parse( cLang, ref headStatements );
            head.SkipTo( safetyToken, ref headStatements );
            var functionText = head.Text.Slice( begText, head.RemainingTextIndex );
            head.AddSpan( new TransformerFunction( functionText,
                                                   startFunction,
                                                   head.LastTokenIndex + 1,
                                                   cLang,
                                                   statements,
                                                   functionName?.ToString(),
                                                   target ) );
        }

        public AnalyzerResult Parse( ReadOnlyMemory<char> text )
        {
            Reset( text );
            return Parse();
        }

        ITokenFilter? ITargetAnalyzer.CreateSpanMatcher( IActivityMonitor monitor, ReadOnlySpan<char> spanType, ReadOnlyMemory<char> pattern )
        {
            Reset( pattern );
            TokenizerHead head = CreateHead();
            while( head.EndOfInput == null )
            {
                if( head.LowLevelTokenType == TokenType.LessThan )
                {
                    if( InjectionPoint.Match( ref head ) == null )
                    {
                        Throw.DebugAssert( head.FirstParseError != null );
                        break;
                    }
                }
                else if( head.LowLevelTokenType == TokenType.DoubleQuote )
                {
                    if( RawString.Match( ref head ) == null )
                    {
                        Throw.DebugAssert( head.FirstParseError != null );
                        break;
                    }
                }
                else
                {
                    head.AcceptLowLevelToken();
                }
            }
            head.ExtractResult( out var code, out var inlineErrorCount );
            Throw.DebugAssert( "No spans here.", !code.Spans.Any() );
            if( inlineErrorCount != 0 )
            {
                monitor.Error( $"""
                    Error '{head.FirstParseError}' while parsing pattern:
                    {pattern}
                    """ );
                return null;
            }
            Type? sType = null;
            if( spanType.Length > 0 )
            {
                sType = spanType switch
                {
                    "statement" => typeof( TransformStatement ),
                    "in" => typeof( InScope ),
                    "replace" => typeof( ReplaceStatement ),
                    _ => null
                };
                if( sType == null )
                {
                    monitor.Error( $"""
                    Invalid span type '{spanType}'. Allowed are "statement", "in", "replace".
                    """ );
                    return null;
                }
                throw new NotImplementedException();
            }
            return new TokenSpanFilter( code.Tokens.ToImmutableArray() );
        }
    }

}
