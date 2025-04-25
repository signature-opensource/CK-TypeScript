using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace CK.Core;

partial class FunctionSource : IResourceInput
{
    readonly IResPackageResources _resources;
    protected readonly string _fullResourceName;
    string _text;
    readonly List<TFunction> _functions;
    int _languageHintIndex;
    string? _sourceName;

    public FunctionSource( IResPackageResources resources, string fullResourceName, string text )
    {
        _resources = resources;
        _fullResourceName = fullResourceName;
        _text = text;
        _functions = new List<TFunction>();
        _languageHintIndex = -1;
    }

    [MemberNotNullWhen( true, nameof( _sourceName ) )]
    public bool IsInitialized => _sourceName != null;

    public IResPackageResources Resources => _resources;

    public ResourceLocator Origin => new ResourceLocator( _resources.Resources, _fullResourceName );

    public string Text => _text;

    protected void SetText( string text ) => _text = text;

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

    protected List<TFunction> Functions => _functions;

    internal bool Initialize( IActivityMonitor monitor, TransformEnvironment environment )
    {
        Throw.DebugAssert( _functions.Count == 0 );
        _sourceName = HandleSourceName( environment.TransformerHost, Origin, ref _languageHintIndex );
        var functions = Parse( monitor, environment, strict: true );
        if( functions == null ) return false;
        foreach( var (f, target, name, fBefore) in functions )
        {
            var tF = new TFunction( this, f, target, name );
            environment.TransformFunctions.Add( name, tF );
            target.Add( tF, fBefore );
            _functions.Add( tF );
        }
        return true;

        static string HandleSourceName( TransformerHost transformerHost, ResourceLocator origin, ref int languageHintIndex )
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

    protected readonly record struct PreFunction( TransformerFunction F, ITransformable Target, string Name, TFunction? Before );

    protected List<PreFunction>? Parse( IActivityMonitor monitor,
                                        TransformEnvironment environment,
                                        bool strict )
    {
        var functions = environment.TransformerHost.TryParseFunctions( monitor, _text );
        if( functions == null ) return null;
        bool success = true;
        var result = new List<PreFunction>( functions.Count );
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
                    if( environment.TransformFunctions.TryGetValue( functionName, out var homonym ) )
                    {
                        monitor.Error( $"""
                                    Transformer '{functionName}' in {Origin}:
                                    {f.Text}
                                    Is already defined by {homonym.Source.Origin}:
                                    {homonym.Function.Text}
                                    """ );
                        success = false;
                    }
                    else if( !target.TryFindInsertionPoint( monitor, this, f, out var fBefore ) )
                    {
                        success = false;
                    }
                    else
                    { 
                        result.Add( new PreFunction( f, target, functionName, fBefore ) );
                    }
                }
            }
        }
        if( strict )
        {
            return success ? result : null;
        }
        return result.Count > 0 ? result : null;
    }

}
