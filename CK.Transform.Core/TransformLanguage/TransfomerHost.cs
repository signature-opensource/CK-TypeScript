using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Transform.TransformLanguage;


public sealed class TransfomerHost
{
    readonly List<CachedLanguage> _languages;

    sealed class CachedLanguage
    {
        public readonly TransformLanguage Language;
        public readonly BaseTransformAnalyzer TransformAnalyzer;
        public readonly Analyzer TargetAnalyzer;

        public CachedLanguage( TransformLanguage language )
        {
            Language = language;
            TransformAnalyzer = language.CreateTransformAnalyzer();
            TargetAnalyzer = language.CreateTargetAnalyzer();
        }
    }

    public TransfomerHost( params IEnumerable<TransformLanguage> languages )
    {
        _languages = new List<CachedLanguage>();
        foreach( var language in languages ) EnsureLanguage( language );
    }

    public IEnumerable<TransformLanguage> Languages => _languages.Select( l => l.Language );

    public bool RemoveLanguage( TransformLanguage language )
    {
        var idx = _languages.FindIndex( l => l.Language.LanguageName == language.LanguageName );
        if( idx >= 0 )
        {
            _languages.RemoveAt( idx );
            return true;
        }
        return false;
    }

    public void EnsureLanguage( TransformLanguage language )
    {
        var l = _languages.FirstOrDefault( l => l.Language.LanguageName == language.LanguageName );
        if( l == null )
        {
            _languages.Add( new CachedLanguage( language ) );
        }
    }

    CachedLanguage? Find( ReadOnlySpan<char> name )
    {
        foreach( var l in _languages )
        {
            if( name.Equals( l.Language.LanguageName, StringComparison.OrdinalIgnoreCase ) )
                return l;
        }
        return null;
    }

    sealed class FunctionAnalyzer : Analyzer
    {
        readonly TransfomerHost _host;

        public FunctionAnalyzer( TransfomerHost host ) => _host = host;


        protected internal override void ParseTrivia( ref TriviaHead c )
        {
            c.AcceptRecursiveStartComment();
            c.AcceptLineComment();
        }

        protected internal override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
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

        protected internal override IAbstractNode Parse( ref AnalyzerHead head )
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
                if( transformer is TokenErrorNode ) return transformer;
                TokenNode? functionName = null;
                bool hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
                if( !hasOn )
                {
                    functionName = head.CreateLowLevelToken();
                    hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
                }
                TokenNode? on = null;
                AbstractNode? target = null;
                if( hasOn )
                {
                    on = head.CreateLowLevelToken();
                    if( head.LowLevelToken.NodeType == NodeType.DoubleQuote )
                    {
                        target = BaseTransformAnalyzer.MatchRawString( ref head );
                        if( target is not RawString ) return target;
                    }
                    else if( head.LowLevelToken.NodeType == NodeType.GenericIdentifier )
                    {
                        target = head.CreateLowLevelToken();
                    }
                    else
                    {
                        return head.CreateError( "Expecting a transformer name after 'on' that can be a string or an identifier." );
                    }
                }
                var asT = head.MatchToken( "as" );
                if( asT is TokenErrorNode ) return asT;
                var beginT = head.MatchToken( "begin" );
                if( beginT is TokenErrorNode ) return beginT;
                cLang.TransformAnalyzer.Reset( RemainingText );
                List<ITransformStatement> statements = new List<ITransformStatement>();
                while( !head.AcceptLowLevelToken( "end", out var endT ) )
                {
                    var s = cLang.TransformAnalyzer.ParseOne();
                    if( s is TokenErrorNode ) return s;
                    if( s is not ITransformStatement statement )
                    {
                        return Throw.InvalidOperationException<IAbstractNode>( $"Language '{cLang.Language.LanguageName}' parsed a '{s.GetType().ToCSharpName()}' that is not a ITransformStatement." );
                    }
                    statements.Add( statement );
                }

            }
            return head.CreateError( "Expecting 'create <language> transformer [name] [on <target>] as begin ... end" );
        }

    }
}
