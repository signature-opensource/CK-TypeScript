using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core;

class FunctionsSource : IResourceInput
{
    readonly IResPackageResources _resources;
    readonly string _fullResourceName;
    string _text;
    readonly List<TFunction> _functions;
    int _languageHintIndex;
    string? _sourceName;

    public FunctionsSource( IResPackageResources resources, string fullResourceName, string text )
    {
        _resources = resources;
        _fullResourceName = fullResourceName;
        _text = text;
        _functions = new List<TFunction>();
        _languageHintIndex = -1;
    }

    public IResPackageResources Resources => _resources;

    public ResourceLocator Origin => new ResourceLocator( _resources.Resources, _fullResourceName );

    public string Text => _text;

    protected void SetText( string text ) => _text = text;

    [MemberNotNullWhen( true, nameof( _sourceName ) )]
    public bool IsInitialized => _sourceName != null;

    /// <summary>
    /// Gets the name of this source without ".t" suffix and without the optional language hint
    /// (a suffix like ".ts.t" or ".sql.t" is removed).
    /// </summary>
    public string SourceName
    {
        get
        {
            Throw.DebugAssert( IsInitialized );
            return _sourceName;
        }
    }

    internal bool Initialize( IActivityMonitor monitor, TransformEnvironment environment )
    {
        Throw.DebugAssert( _functions.Count == 0 );
        _sourceName = HandleSourceName( environment.TransformerHost, Origin, ref _languageHintIndex );
        var functions = Parse( monitor, environment );
        if( functions == null ) return false;
        bool success = true;
        foreach( var (f, target, name) in functions )
        {
            if( environment.TransformFunctions.TryGetValue( name, out var homonym ) )
            {
                monitor.Error( $"""
                    Transformer '{name}' in {Origin}:
                    {f.Text}
                    Is already defined by {homonym.Source.Origin}:
                    {homonym.Function.Text}
                    """ );
                success = false;
            }
            if( !target.TryFindInsertionPoint( monitor, this, f, out var fBefore ) )
            {
                success = false;
            }
            if( success )
            {
                var tF = new TFunction( this, f, target, name );
                _functions.Add( tF );
                environment.TransformFunctions.Add( name, tF );
            }
        }
        return success;

        static string HandleSourceName( TransformerHost transformerHost, in ResourceLocator origin, ref int languageHintIndex )
        {
            Throw.DebugAssert( origin.ResourceName.EndsWith( ".t" ) );
            var rName = origin.ResourceName;
            int sourceNameLength = rName.Length - 2;
            var languageHint = transformerHost.FindFromFilename( rName.Slice( 0, sourceNameLength ), out var fileExtension );
            if( languageHint != null )
            {
                languageHintIndex = languageHint.Index;
                sourceNameLength -= fileExtension.Length;
            }
            return rName.Slice( 0, sourceNameLength ).ToString();
        }


    }

    List<(TransformerFunction, ITransformable, string)>? Parse( IActivityMonitor monitor, TransformEnvironment environment )
    {
        var functions = environment.TransformerHost.TryParseFunctions( monitor, Text );
        if( functions == null ) return null;
        bool success = true;
        var result = new List<(TransformerFunction, ITransformable, string)>( functions.Count );
        foreach( var f in functions )
        {
            if( _languageHintIndex != -1 && f.Language.Index != _languageHintIndex )
            {
                monitor.Error( $"{Origin} contains a {f.Language.LanguageName} transfomer. It should only contain transformers for {environment.TransformerHost.Languages[_languageHintIndex].LanguageName}." );
                success = false;
            }
            else
            {
                var target = environment.FindTarget( monitor, this, f );
                if( target == null )
                {
                    success = false;
                }
                else
                {
                    var functionName = TFunction.ComputeName( this, f, target );
                    result.Add( (f, target, functionName) );
                }
            }
        }
        return success ? result : null;
    }

}
