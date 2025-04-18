using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CK.Core;

/// <summary>
/// A TransformerFunctionSource is a <see cref="TransformableSource"/> that defines
/// a set of <see cref="TransformerFunction"/>.
/// </summary>
sealed class TFunctionSource : TransformableSource
{
    readonly List<TFunction> _functions;

    TransformerHost.Language? _languageHint;
    string? _sourceName;

    public TFunctionSource( IResPackageResources resources, ResourceLocator origin, string text )
        : base( resources, origin, text )
    {
        _functions = new List<TFunction>();
    }

    [MemberNotNullWhen( true, nameof( _sourceName ) )]
    public bool IsInitialized => _sourceName != null;

    /// <summary>
    /// Gets the name of this source without ".t" suffix and without <see cref="LanguageHint"/>
    /// (suffix like ".ts.t" or ".sql.t" is removed.
    /// </summary>
    public string SourceName
    {
        get
        {
            Throw.DebugAssert( IsInitialized );
            return _sourceName;
        }
    }

    /// <summary>
    /// Gets the optional language specified as a suffix in the name (like ".sql.t").
    /// </summary>
    public TransformerHost.Language? LanguageHint => _languageHint;

    internal bool Initialize( IActivityMonitor monitor, TransformEnvironment environment )
    {
        Throw.DebugAssert( _functions.Count == 0 );
        Throw.DebugAssert( Origin.ResourceName.EndsWith( ".t" ) );

        HandleSourceName( environment.TransformerHost );

        var functions = environment.TransformerHost.TryParseFunctions( monitor, Text );
        if( functions == null ) return false;
        bool success = true;
        foreach( var f in functions )
        {
            if( _languageHint != null && f.Language != _languageHint )
            {
                monitor.Error( $"{Origin} contains a {f.Language.LanguageName} transfomer. It should only contain transformers for {_languageHint.TransformLanguage.LanguageName}." );
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
                    var tF = new TFunction( this, f, target );
                    _functions.Add( tF );
                }
            }
        }
        return success;
    }

    [MemberNotNull( nameof( _sourceName ) )]
    void HandleSourceName( TransformerHost transformerHost )
    {
        var rName = Origin.ResourceName;
        int sourceNameLength = rName.Length - 2;
        _languageHint = transformerHost.FindFromFilename( rName.Slice( 0, sourceNameLength ), out var fileExtension );
        sourceNameLength -= fileExtension.Length;
        _sourceName = rName.Slice( 0, sourceNameLength ).ToString();
    }

    protected override void Die( IActivityMonitor monitor )
    {
        foreach( var f in _functions )
        {
            f.Die( monitor );
        }
    }

    protected override bool Revive( IActivityMonitor monitor, TransformerHost transformerHost )
    {
        throw new NotImplementedException();
    }
}
