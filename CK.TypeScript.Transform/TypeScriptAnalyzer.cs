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
    }

    /// <inheritdoc/>
    protected override void Reset( ReadOnlyMemory<char> text )
    {
        _braceDepth = 0;
        _interpolated.Clear();
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
    protected override void Tokenize( ref TokenizerHead head )
    {
        for(; ; )
        {
            var t = Scan( ref head );
            // Currently stops on the first error.
            if( t is TokenError e )
            {
                return;
            }
            // Handles import statement (but not import(...) functions).
            if( t.Text.Span.Equals( "import", StringComparison.Ordinal )
                && head.LowLevelTokenType is not TokenType.OpenParen )
            {
                ImportStatement.Match( ref head, t );
            }
        }
    }

    /// <inheritdoc/>
    public AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        Reset( text );
        return Parse();
    }

    ITokenFilter? ITargetAnalyzer.CreateSpanMatcher( IActivityMonitor monitor, ReadOnlySpan<char> spanType, ReadOnlyMemory<char> pattern )
    {
        throw new NotImplementedException();
    }
}
