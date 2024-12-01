using CK.Core;
using CK.Transform.Core;
using CK.Transform.ErrorTolerant;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CK.Transform.TransformLanguage;

public class TransformAnalyzerContext : AnalyzerContext<TransformAnalyzerContext>
{
    public TransformAnalyzerContext()
    {
    }

    protected TransformAnalyzerContext( TransformAnalyzerContext parent ) : base( parent )
    {
    }
}

public sealed class TransformAnalyzer : ContextualAnalyzer<TransformAnalyzerContext>
{
    public TransformAnalyzer()
    {
    }

    protected override IAbstractNode Parse( ref AnalyzerHead head, TransformAnalyzerContext context )
    {
        if( head.TryAcceptToken( TokenType.GenericIdentifier, "inject", out var inject ) )
        {
            var c = new InjectContext( context, inject );
            MatchRawString( ref head, c, out var content );
            MatchToken( ref head, c, "into", out var into );
            MatchInjectionPoint( ref head, c, out var target );
            MatchStatementTerminator( ref head, context, out var terminator );
            return InjectIntoNode.Create( inject, content, into, target, terminator );
        }
        return TokenErrorNode.Unhandled;
    }

    private void MatchToken( ref AnalyzerHead head, TransformAnalyzerContext context, string text, [NotNull]out AbstractNode? into )
    {
        if( !head.TryAcceptToken( TokenType.GenericKeyword, text, out into ) )
        {
            into = context.TextNotFound( ref head, text );
        }
    }

    protected internal override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptStartComment();
        c.AcceptLineComment();
    }
}

class InjectContext : TransformAnalyzerContext
{
    public InjectContext( TransformAnalyzerContext parent, TokenNode inject ) : base( parent )
    {
    }


}
