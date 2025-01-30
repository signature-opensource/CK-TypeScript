using System;
using System.Collections.Generic;

namespace CK.Transform.Core;


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
    internal TransformStatementBlock ParseStatements( ref TokenizerHead head )
    {
        var statements = new List<TransformStatement>();
        head.MatchToken( "begin" );
        int begBlock = head.LastTokenIndex;
        Token? foundEnd = null;
        while( head.EndOfInput == null && !head.TryAcceptToken( "end", out foundEnd ) )
        {
            var s = ParseStatement( ref head );
            if( s != null )
            {
                statements.Add( s );
            }
            else
            {
                head.AppendError( $"Failed to parse a transform '{_language.LanguageName}' language statement." );
                break;
            }
        }
        if( foundEnd == null ) head.AppendError( "Expected 'end'." );
        return new TransformStatementBlock( begBlock, head.LastTokenIndex + 1, statements );
    }

    /// <summary>
    /// Must implement transform specific statement parsing.
    /// Parsing errors should be inlined <see cref="TokenError"/>. 
    /// <para>
    /// At this level, this handles transform statements that apply to any language:
    /// <c>begin</c>...<c>end</c> "transaction" (<see cref="TransformStatementBlock"/>)
    /// and the <see cref="InjectIntoStatement"/>.
    /// </para>
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The parsed statement or null.</returns>
    protected virtual TransformStatement? ParseStatement( ref TokenizerHead head )
    {
        if( head.TryAcceptToken( "inject", out var inject ) )
        {
            return MatchInjectIntoStatement( ref head, inject );
        }
        if( head.LowLevelTokenText.Equals( "begin", StringComparison.Ordinal ) )
        {
            return ParseStatements( ref head );
        }
        return null;
    }

    static InjectIntoStatement? MatchInjectIntoStatement( ref TokenizerHead head, Token inject )
    {
        int startStatement = head.LastTokenIndex;
        var content = RawString.TryMatch( ref head );
        head.MatchToken( "into" );
        var target = MatchInjectionPoint( ref head );
        head.TryAcceptToken( ";", out _ );
        return content != null && target != null
                ? new InjectIntoStatement( startStatement, head.LastTokenIndex + 1, content, target )
                : null;

        static InjectionPoint? MatchInjectionPoint( ref TokenizerHead head )
        {
            if( head.LowLevelTokenType == TokenType.LessThan )
            {
                var sHead = head.Head;
                int nameLen = InjectIntoStatement.GetInjectionPointLength( sHead );
                if( nameLen > 0 && nameLen < sHead.Length && sHead[nameLen] == '>' )
                {
                    head.PreAcceptToken( nameLen + 1, out var text, out var leading, out var trailing );
                    return head.Accept( new InjectionPoint( text, leading, trailing ) );
                }
            }
            head.AppendError( "Expected <InjectionPoint>." );
            return null;
        }
    }
}
