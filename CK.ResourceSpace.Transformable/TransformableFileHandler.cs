using CK.BinarySerialization;
using CK.Transform.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Handles files in languages supported by a <see cref="TransformerHost"/>.
/// </summary>
public sealed partial class TransformableFileHandler : ResourceSpaceFileHandler
{
    readonly TransformerHost _transformerHost;
    TransformEnvironment? _environment;

    /// <summary>
    /// Initializes a new <see cref="TransformableFileHandler"/> for all the file extensions from
    /// <see cref="TransformerHost.LockLanguages()"/>'s <see cref="TransformLanguage.FileExtensions"/>.
    /// </summary>
    /// <param name="installer">The installer that <see cref="Install(IActivityMonitor)"/> will use.</param>
    /// <param name="transformerHost">The transfomer host that will be used by this file handler.</param>
    public TransformableFileHandler( IResourceSpaceItemInstaller? installer, TransformerHost transformerHost )
        : base( installer,
                transformerHost.LockLanguages().SelectMany( l => l.TransformLanguage.FileExtensions ).Distinct().ToImmutableArray() )
    {
        _transformerHost = transformerHost;
    }

    /// <summary>
    /// Initializes this handler by analyzing all the configured language files from all packages' resources.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="spaceData">The resource space data.</param>
    /// <param name="folderFilter">The filter to use.</param>
    /// <returns>True on success, false on error.</returns>
    protected override bool Initialize( IActivityMonitor monitor, ResSpaceData spaceData, FolderExclusion folderFilter )
    {
        bool success = true;
        var environment = new TransformEnvironment( spaceData, _transformerHost );
        foreach( var resources in spaceData.AllPackageResources )
        {
            foreach( var r in resources.Resources.AllResources )
            {
                if( folderFilter.IsExcluded( r ) ) continue;
                var language = _transformerHost.FindFromFilename( r.Name, out _ );
                if( language == null ) continue;
                success &= environment.Register( monitor, resources, language, r ) != null ;
            }
        }
        if( success )
        {
            _environment = environment;
        }
        return success;
    }

    /// <inheritdoc />
    protected override bool Install( IActivityMonitor monitor )
    {
        if( Installer is null )
        {
            monitor.Warn( $"No installer associated to '{ToString()}'. Skipped." );
            return true;
        }
        Throw.CheckState( _environment != null );
        bool success = true;
        foreach( var i in _environment.Items.Values )
        {
            var text = i.GetFinalText( monitor, _environment.TransformerHost );
            if( text == null )
            {
                success = false;
            }
            else
            {
                Installer.Write( i.TargetPath, text );
            }
        }
        return success;
    }
}
