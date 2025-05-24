using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
        _sourceName = HandleSourceName( environment, Origin, ref _languageHintIndex );
        var preFunctions = Parse( monitor, environment, strict: true );
        if( preFunctions == null ) return false;
        foreach( var p in preFunctions )
        {
            AddNewFunction( environment, p );
        }
        return true;

        static string HandleSourceName( TransformEnvironment environment, ResourceLocator origin, ref int languageHintIndex )
        {
            Throw.DebugAssert( origin.ResourceName.EndsWith( ".t" ) );
            var rName = origin.ResourceName;
            int sourceNameLength = rName.Length - 2;
            var languageHint = environment.FindFromFileName( rName.Slice( 0, sourceNameLength ), out var fileExtension );
            if( languageHint != null )
            {
                languageHintIndex = languageHint.Index;
                sourceNameLength -= fileExtension.Length;
            }
            return rName.Slice( 0, sourceNameLength ).ToString();
        }
    }

    protected TFunction AddNewFunction( TransformEnvironment environment, PreFunction p )
    {
        var f = new TFunction( this, p.F, p.Target, p.Name );
        environment.TransformFunctions.Add( p.Name, f );
        p.Target.Add( f, p.Before );
        _functions.Add( f );
        return f;
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
                // If we don't find the target, it is not necessarily an error:
                // if we are in live and the function's target is a "../" external item, the
                // transfomer must not be run.
                success = environment.FindTarget( monitor, this, f, out ITransformable? target );
                if( target != null )
                {
                    var functionName = TFunction.ComputeName( this, f, target );
                    var already = result.FirstOrDefault( f => f.Name == functionName );
                    if( already.F != null )
                    {
                        monitor.Error( $"""
                                    Duplicate Transformer '{functionName}' definition in {Origin}:
                                    {f.Text}
                                    Is already defined by:
                                    {already.F.Text}
                                    """ );
                        success = false;
                    }
                    else
                    {
                        already = result.FirstOrDefault( f => f.Target == target );
                        if( already.F != null )
                        {
                            monitor.Error( $"""
                                        Duplicate Transformer definition in {Origin}:
                                        {f.Text}

                                        And: 
                                        {already.F.Text}

                                        Both target '{target.TransfomableTargetName}'.
                                        """ );
                            success = false;
                        }
                        else if( environment.TransformFunctions.TryGetValue( functionName, out var homonym ) )
                        {
                            monitor.Error( $"""
                                    Transformer '{functionName}' in {Origin}:
                                    {f.Text}
                                    Is already defined in {homonym.Source.Origin}:
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
        }
        if( strict )
        {
            return success ? result : null;
        }
        // We may return an empty list here.
        return result;
    }

}
