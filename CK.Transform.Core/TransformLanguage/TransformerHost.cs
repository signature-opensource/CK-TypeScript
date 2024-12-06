using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Transform.TransformLanguage;


/// <summary>
/// Hosts multiple <see cref="TransformLanguage"/>.
/// This is NOT thread safe and should never be used concurrently.
/// </summary>
public sealed partial class TransformerHost
{
    readonly List<CachedLanguage> _languages;
    readonly TLanguage _transformLanguage;
    readonly TAnalyzer _transformAnalyzer;

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

    /// <summary>
    /// Initializes a host with at least the <see cref="TLanguage"/>.
    /// </summary>
    /// <param name="languages">Languages to register.</param>
    public TransformerHost( params TransformLanguage[] languages )
    {
        _transformLanguage = new TLanguage( this );
        _transformAnalyzer = new TAnalyzer( this );
        _languages = new List<CachedLanguage>();
        foreach( var language in languages ) EnsureLanguage( language );
        if( Find( _transformLanguage.LanguageName ) == null ) 
        {
            _languages.Add( new CachedLanguage( _transformLanguage ) );
        }
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

    public TransfomerFunction Parse( string text ) => Parse( text.AsMemory() );

    public TransfomerFunction Parse( ReadOnlyMemory<char> text )
    {
        var head = new ParserHead( text, _transformAnalyzer );
        var n = _transformAnalyzer.ParseFunction( ref head );
        if( n is not TransfomerFunction f )
        {
            return Throw.ArgumentException<TransfomerFunction>( nameof( text ), n.ToString() );
        }
        return f;
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


}
