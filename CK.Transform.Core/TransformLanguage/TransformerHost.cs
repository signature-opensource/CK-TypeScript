using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Transform.TransformLanguage;


/// <summary>
/// Hosts multiple <see cref="TransformLanguage"/>.
/// This is NOT thread safe and should never be used concurrently.
/// </summary>
public sealed partial class TransformerHost
{
    readonly List<Language> _languages;
    readonly TLanguage _transformLanguage;
    readonly TAnalyzer _transformAnalyzer;

    /// <summary>
    /// Cached instance of a <see cref="TransformLanguage"/> for this host.
    /// </summary>
    public sealed class Language
    {
        readonly TransformLanguage _language;
        readonly BaseTransformAnalyzer _transformAnalyzer;
        readonly Analyzer _targetAnalyzer;

        /// <summary>
        /// Gets the <see cref="TransformLanguage.LanguageName"/>.
        /// </summary>
        public string LanguageName => _language.LanguageName;

        /// <summary>
        /// Gets the analyzer for the transform language.
        /// </summary>
        public BaseTransformAnalyzer TransformAnalyzer => _transformAnalyzer;

        /// <summary>
        /// Gets the target language annalyzer.
        /// </summary>
        public Analyzer TargetAnalyzer => _targetAnalyzer;

        /// <summary>
        /// Gets the transform language.
        /// </summary>
        public TransformLanguage TransformLanguage => _language;

        internal Language( TransformLanguage language )
        {
            _language = language;
            _transformAnalyzer = language.CreateTransformAnalyzer();
            _targetAnalyzer = language.CreateTargetAnalyzer();
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
        _languages = new List<Language>();
        foreach( var language in languages ) EnsureLanguage( language );
        if( Find( _languages, _transformLanguage.LanguageName ) == null ) 
        {
            _languages.Add( new Language( _transformLanguage ) );
        }
    }

    /// <summary>
    /// Gets the languages that this transfomer handles.
    /// </summary>
    public IReadOnlyList<Language> Languages => _languages;

    /// <summary>
    /// Removes a language.
    /// </summary>
    /// <param name="language">The language to remove.</param>
    /// <returns>Tre if the language has been removed, false if it was not found.</returns>
    public bool RemoveLanguage( TransformLanguage language )
    {
        var idx = _languages.FindIndex( l => l.TransformLanguage != _transformLanguage && l.LanguageName == language.LanguageName );
        if( idx >= 0 )
        {
            _languages.RemoveAt( idx );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a language if it is not already registered.
    /// </summary>
    /// <param name="language">The language to add.</param>
    public void EnsureLanguage( TransformLanguage language )
    {
        var l = _languages.FirstOrDefault( l => l.LanguageName == language.LanguageName );
        if( l == null )
        {
            _languages.Add( new Language( language ) );
        }
    }

    /// <summary>
    /// Finds a registered <see cref="Language"/>.
    /// </summary>
    /// <param name="name">The language name.</param>
    /// <returns>The language or null if not found.</returns>
    public Language? Find( ReadOnlySpan<char> name ) => Find( _languages, name );

    /// <inheritdoc cref="ParseFunction(ReadOnlyMemory{char})"/>
    public TransfomerFunction ParseFunction( string text ) => ParseFunction( text.AsMemory() );

    /// <summary>
    /// Parses a <see cref="TransfomerFunction"/> or throws if it cannot be parsed.
    /// </summary>
    /// <param name="text">the text to parse.</param>
    /// <returns>The function.</returns>
    public TransfomerFunction ParseFunction( ReadOnlyMemory<char> text )
    {
        var head = new ParserHead( text, _transformAnalyzer );
        var n = _transformAnalyzer.ParseExpectedFunction( ref head );
        if( n is not TransfomerFunction f )
        {
            return Throw.ArgumentException<TransfomerFunction>( nameof( text ), n.ToString() );
        }
        return f;
    }

    /// <summary>
    /// Applies a sequence of transformers to an initial <paramref name="text"/>.
    /// </summary>
    /// <param name="monitor">The monitor that will receive logs and errors.</param>
    /// <param name="text">The text to transform.</param>
    /// <param name="transformers">The transformers to apply in order.</param>
    /// <returns>The transformed node on success and null if an error occurred.</returns>
    public AbstractNode? Transform( IActivityMonitor monitor,
                                    string text,
                                    params IEnumerable<TransfomerFunction> transformers )
    {
        return Transform( monitor, text, null, transformers );
    }

    /// <summary>
    /// Applies a sequence of transformers to an initial <paramref name="text"/>.
    /// </summary>
    /// <param name="monitor">The monitor that will receive logs and errors.</param>
    /// <param name="text">The text to transform.</param>
    /// <param name="scope">Optional scope. Applies only to the first transformer.</param>
    /// <param name="transformers">The transformers to apply in order.</param>
    /// <returns>The transformed node on success and null if an error occurred.</returns>
    public AbstractNode? Transform( IActivityMonitor monitor,
                                    string text,
                                    NodeScopeBuilder? scope = null,
                                    params IEnumerable<TransfomerFunction> transformers )
    {
        var transformer = transformers.FirstOrDefault();
        Throw.CheckArgument( transformer is not null );

        Language? language = LocalFind( monitor, _languages, transformer, text );
        if( language == null ) return null;
        AbstractNode? target = ParseTarget( monitor, text, language );
        if( target == null ) return null;

        var h = new Host( language, target );
        if( !h.Apply( monitor, transformer, scope ) )
        {
            return null;
        }
        foreach( var t in transformers.Skip( 1 ) )
        {
            if( !t.LanguageName.Equals( language.LanguageName, StringComparison.OrdinalIgnoreCase ) )
            {
                language = LocalFind( monitor, _languages, transformer, text );
                if( language == null || !h.Reparse( monitor ) )
                {
                    return null;
                }
            }
            if( !h.Apply( monitor, transformer, scope: null ) )
            {
                return null;
            }
        }
        return h.Node;

        static Language? LocalFind( IActivityMonitor monitor, List<Language> languages, TransfomerFunction transformer, string text )
        {
            var l = Find( languages, transformer.LanguageName );
            if( l == null )
            {
                monitor.Error( $"""
                            Unavailable language '{transformer.LanguageName}'. Cannot apply:
                            {transformer}
                            On text:
                            {text}
                            """ );
                return null;
            }
            return l;
        }

    }

    static Language? Find( List<Language> languages, ReadOnlySpan<char> name )
    {
        foreach( var l in languages )
        {
            if( name.Equals( l.LanguageName, StringComparison.OrdinalIgnoreCase ) )
                return l;
        }
        return null;
    }

    static AbstractNode? ParseTarget( IActivityMonitor monitor, string text, Language language )
    {
        language.TargetAnalyzer.Reset( text.AsMemory() );
        var target = language.TargetAnalyzer.ParseAll();
        if( target is IErrorNode e )
        {
            monitor.Error( $"""
                            Unable to parse target text. {e}:
                            {text}
                            """ );
            return null;
        }
        return Unsafe.As<AbstractNode>( target );
    }
}
