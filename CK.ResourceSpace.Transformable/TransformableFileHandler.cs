using CK.Transform.Core;
using System.Collections.Immutable;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Core;

/// <summary>
/// Handles files in languages supported by a <see cref="TransformerHost"/>.
/// </summary>
public sealed partial class TransformableFileHandler : ResourceSpaceFileHandler
{
    readonly TransformerHost _transformerHost;
    readonly ImmutableArray<TransformableFileInstallHook> _installHooks;
    TransformEnvironment? _environment;

    /// <summary>
    /// Initializes a new <see cref="TransformableFileHandler"/> for all the file extensions from
    /// <see cref="TransformerHost.LockLanguages()"/>'s <see cref="TransformLanguage.FileExtensions"/>
    /// and any number of <see cref="ITransformableFileInstallHook"/>.
    /// </summary>
    /// <param name="installer">The installer that <see cref="Install(IActivityMonitor)"/> will use.</param>
    /// <param name="transformerHost">The transfomer host that will be used by this file handler.</param>
    /// <param name="installHooks">Optional install hooks.</param>
    public TransformableFileHandler( IResourceSpaceItemInstaller? installer,
                                     TransformerHost transformerHost,
                                     params ImmutableArray<TransformableFileInstallHook> installHooks )
        : base( installer,
                transformerHost.LockLanguages().SelectMany( l => l.TransformLanguage.FileExtensions ).Distinct().ToImmutableArray() )
    {
        _transformerHost = transformerHost;
        _installHooks = installHooks;
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
                var language = environment.FindFromFileName( r.Name, out _ );
                if( language == null ) continue;
                success &= environment.Register( monitor, resources, language, r ) != null ;
            }
        }
        if( success )
        {
            _environment = environment;
            foreach( var item in _installHooks )
            {
                item.Initialize( spaceData, _transformerHost );
            }
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

        var installer = new InstallHooksHelper( _installHooks, Installer, _transformerHost );
        return installer.Run( monitor, _environment.Items.Values, toBeRemoved: null );
    }
}
