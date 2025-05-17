using System.Collections.Immutable;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

namespace CK.Core;

/// <summary>
/// Simple helper.
/// </summary>
readonly struct InstallHooksHelper
{
    readonly ImmutableArray<ITransformableFileInstallHook> _hooks;
    readonly IResourceSpaceItemInstaller _installer;

    public InstallHooksHelper( ImmutableArray<ITransformableFileInstallHook> hooks, IResourceSpaceItemInstaller installer )
    {
        _hooks = hooks;
        _installer = installer;
    }

    public void Start( IActivityMonitor monitor )
    {
        foreach( var h in _hooks )
        {
            h.StartInstall( monitor );
        }
    }

    public void Handle( IActivityMonitor monitor, TransformInstallableItem item, string text )
    {
        bool handled = false;
        foreach( var h in _hooks )
        {
            if( (handled = h.HandleInstall( monitor, item, text, _installer )) )
            {
                break;
            }
        }
        if( !handled )
        {
            _installer.Write( item.TargetPath, text );
        }
    }

    public void Stop( IActivityMonitor monitor )
    {
        foreach( var h in _hooks )
        {
            h.StopInstall( monitor, _installer );
        }
    }

}
