using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace CK.Core;

public sealed class TransformableFileHandler : ResourceSpaceFileHandler
{
    readonly TransformerHost _host;
    List<TransformableItem>? _items;

    /// <summary>
    /// Initializes a new <see cref="TransformableFileHandler"/> for all the file extensions from
    /// <see cref="TransformerHost.LockLanguages()"/>'s <see cref="TransformLanguage.FileExtensions"/>.
    /// </summary>
    /// <param name="transformerHost">The transfomer host that will be used by this file handler.</param>
    public TransformableFileHandler( TransformerHost transformerHost )
        : base( transformerHost.LockLanguages().SelectMany( l => l.TransformLanguage.FileExtensions ).Distinct().ToImmutableArray() )
    {
        _host = transformerHost;
    }

    protected override bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData, FolderExclusion folderFilter )
    {
        bool success = true;
        _items = new List<TransformableItem>();
        foreach( var resources in spaceData.AllPackageResources )
        {
            foreach( var r in resources.Resources.AllResources )
            {
                if( folderFilter.IsExcluded( r ) ) continue;
                var language = _host.FindFromFilename( r.Name );
                if( language != null )
                {
                    if( language.TransformLanguage.IsTransformerLanguage )
                    {

                    }
                    else
                    {
                        var target = resources.Package.DefaultTargetPath.Combine( r.ResourceName.ToString() );
                        _items.Add( new TransformableItem( resources, r, language, target ) );
                    }
                }
            }
        }
        return success;
    }

    /// <summary>
    /// Saves the resources into the <paramref name="target"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="target">The target.</param>
    /// <returns>True on succes, false one error (errors have been logged).</returns>
    protected override bool Install( IActivityMonitor monitor, ResourceSpaceFileInstaller target )
    {
        Throw.CheckState( _items != null );
        bool success = true;
        foreach( var i in _items )
        {
            var text = i.GetText( monitor );
            if( text == null )
            {
                success = false;
            }
            else
            {
                target.Write( i.Target, text );
            }
        }
        return success;
    }

}
