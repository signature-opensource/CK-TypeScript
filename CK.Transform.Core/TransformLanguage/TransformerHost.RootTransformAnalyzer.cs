using CK.Core;
using CK.Transform.Core;
using System;
using System.Linq;
using System.Threading;

namespace CK.Transform.TransformLanguage;


public sealed partial class TransformerHost
{
    /// <summary>
    /// Transform language analyzer itself. This handles the top-level 'create &lt;language&gt; transformer [name] [on &lt;target&gt;] [as] begin ... end'.
    /// Statements analysis is delegated to <see cref="Language.TransformStatementAnalyzer"/>.
    /// </summary>
    sealed class RootTransformAnalyzer : Tokenizer, IAnalyzer<TransfomerFunction>
    {
        readonly TransformerHost _host;
        // We only parse one statement at a time here: the side channel
        // doesn't need to be a collector.
        TransfomerFunction? _result;

        public RootTransformAnalyzer( TransformerHost host )
        {
            _host = host;
        }

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
                var headStatements = head.CreateSubHead( out var safetyToken, cLang.TransformStatementAnalyzer as ILowLevelTokenizer );
                var statements = cLang.TransformStatementAnalyzer.ParseStatements( ref headStatements );
                head.SkipTo( safetyToken, in headStatements );
                _result = new TransfomerFunction( cLang.TransformLanguage, statements, functionName?.ToString(), target?.ToString() );
            }
            return null;
        }

        public IAnalyzerResult<TransfomerFunction> Parse( ReadOnlyMemory<char> text )
        {
            bool success = Tokenize( out var tokens, out var error );
            return success
                    ? AnalyzerResult.Create( tokens, _result )
                    : AnalyzerResult.CreateFailed<TransfomerFunction>( tokens, error );
        }
    }

}
