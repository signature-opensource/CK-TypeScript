using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;

namespace CK.TypeScript.Transform;

/// <summary>
/// TypeScript language anlayzer.
/// </summary>
public sealed partial class TypeScriptAnalyzer : Tokenizer, ITargetAnalyzer
{
    // Keeps the brace depth of interpolated starts.
    readonly Stack<int> _classes;

    /// <summary>
    /// Gets "TypeScript".
    /// </summary>
    public string LanguageName => TypeScriptLanguage._languageName;

    /// <summary>
    /// Initialize a new TypeScriptAnalyzer.
    /// </summary>
    public TypeScriptAnalyzer()
    {
        _interpolated = new Stack<int>();
        _classes = new Stack<int>();
    }

    /// <inheritdoc/>
    protected override void Reset( ReadOnlyMemory<char> text )
    {
        _braceDepth = 0;
        _interpolated.Clear();
        _classes.Clear();
        base.Reset( text );
    }

    /// <summary>
    /// Calls <see cref="TriviaHeadExtensions.AcceptCLikeLineComment(ref TriviaHead)"/>
    /// and <see cref="TriviaHeadExtensions.AcceptCLikeStarComment(ref TriviaHead)"/>.
    /// </summary>
    /// <param name="c">The trivia head.</param>
    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeLineComment();
        c.AcceptCLikeStarComment();
    }

    /// <inheritdoc/>
    public AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        Reset( text );
        return Parse();
    }

    /// <inheritdoc/>
    protected override void Tokenize( ref TokenizerHead head )
    {
        for(; ; )
        {
            var t = GetNextToken( ref head );
            if( t.TokenType is TokenType.EndOfInput )
            {
                return;
            }
            // Handles import statement (but not import(...) functions) only here (top-level constructs).
            if( t.TextEquals( "import" ) && head.LowLevelTokenType is not TokenType.OpenParen )
            {
                ImportStatement.Match( ref head, t );
            }
            if( t is not TokenError )
            {
                HandleKnownSpan( ref head, t );
            }
        }
    }

    internal SourceSpan? HandleKnownSpan( ref TokenizerHead head, Token t )
    {
        Throw.DebugAssert( t is not TokenError );
        if( t.TextEquals( "class" ) )
        {
            return ClassDefinition.Match( this, ref head, t );
        }
        if( t.TokenType is TokenType.OpenBrace )
        {
            return BraceSpan.Match( this, ref head, t );
        }
        return null;
    }

    internal bool SkipTo( ref TokenizerHead head, TokenType type )
    {
        for( ; ; )
        {
            var t = GetNextToken( ref head );
            if( t.TokenType is TokenType.EndOfInput )
            {
                head.AppendError( $"Missing '{type}' token.", 0 );
                return false;
            }
            if( t.TokenType == type )
            {
                return true;
            }
        }
    }

}
