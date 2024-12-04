using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Transform.TransformLanguage;


public sealed partial class TransformerHost
{
    sealed class TAnalyzer : BaseTransformAnalyzer
    {
        readonly TransformerHost _host;

        public TAnalyzer( TransformerHost host )
            : base( host._transformLanguage )
        {
            _host = host;
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
            return default;
        }

        internal IAbstractNode ParseFunction( ref AnalyzerHead head, IAnalyzerBehavior? newBehavior = null )
        {
            if( head.AcceptLowLevelToken( "create", out var create ) )
            {
                CachedLanguage? cLang = _host.Find( head.LowLevelTokenText );
                if( cLang == null )
                {
                    return head.CreateError( $"Expected language name. Available languages are: '{_host.Languages.Select( l => l.LanguageName ).Concatenate("', '")}'." );
                }
                var language = head.CreateLowLevelToken();
                var transformer = head.MatchToken( "transformer" );
                if( transformer is IErrorNode ) return transformer;
                TokenNode? functionName = null;
                bool hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
                if( !hasOn && !head.LowLevelTokenText.Equals( "as", StringComparison.Ordinal ) )
                {
                    functionName = head.CreateLowLevelToken();
                    hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
                }
                TokenNode? on = null;
                AbstractNode? target = null;
                if( hasOn )
                {
                    on = head.CreateLowLevelToken();
                    if( head.LowLevelTokenType == NodeType.DoubleQuote )
                    {
                        target = MatchRawString( ref head );
                        if( target is not RawString ) return target;
                    }
                    else if( head.LowLevelTokenType == NodeType.GenericIdentifier )
                    {
                        target = head.CreateLowLevelToken();
                    }
                    else
                    {
                        return head.CreateError( "Expecting a transformer name after 'on' that can be a string or an identifier." );
                    }
                }
                var asT = head.MatchToken( "as" );
                if( asT is IErrorNode ) return asT;
                var beginT = head.MatchToken( "begin" );
                if( beginT is IErrorNode ) return beginT;
                var statements = new List<ITransformStatement>();
                TokenNode? endT;
                while( !head.AcceptLowLevelToken( "end", out endT ) )
                {
                    var s = cLang.TransformAnalyzer.Parse( ref head );
                    if( s is IErrorNode ) return s;
                    if( s is not ITransformStatement statement )
                    {
                        return Throw.InvalidOperationException<IAbstractNode>( $"Language '{cLang.Language.LanguageName}' parsed a '{s.GetType().ToCSharpName()}' that is not a ITransformStatement." );
                    }
                    statements.Add( statement );
                }
                return new TransfomerFunction( create, language, transformer, functionName, on, target, asT, beginT, new NodeList<ITransformStatement>( statements ), endT );
            }
            return head.CreateError( "Expecting 'create <language> transformer [name] [on <target>] as begin ... end" );
        }

    }
}
