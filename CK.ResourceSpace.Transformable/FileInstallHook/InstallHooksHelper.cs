using CK.Transform.Core;
using System;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Simple helper.
/// </summary>
readonly struct InstallHooksHelper
{
    readonly IEnumerable<ITransformableFileInstallHook> _hooks;
    readonly IResourceSpaceItemInstaller _installer;
    readonly TransformerHost _transformerHost;

    public InstallHooksHelper( IEnumerable<ITransformableFileInstallHook> hooks,
                               IResourceSpaceItemInstaller installer,
                               Transform.Core.TransformerHost transformerHost )
    {
        _hooks = hooks;
        _installer = installer;
        _transformerHost = transformerHost;
    }

    public bool Run( IActivityMonitor monitor, IEnumerable<TransformableItem> items )
    {
        bool success = true;
        Start( monitor );
        foreach( var i in items )
        {
            var text = i.GetFinalText( monitor, _transformerHost );
            if( text == null )
            {
                success = false;
            }
            else
            {
                success &= Handle( monitor, i, text );
            }
        }
        Stop( monitor, success );
        return success;
    }

    void Start( IActivityMonitor monitor )
    {
        foreach( var h in _hooks )
        {
            h.StartInstall( monitor );
        }
    }

    bool Handle( IActivityMonitor monitor, ITransformInstallableItem item, string text )
    {
        bool success = true;
        bool handled = false;
        foreach( var h in _hooks )
        {
            success = h.HandleInstall( monitor, item, text, _installer, out handled );
            if( !success || handled )
            {
                break;
            }
        }
        if( success && !handled )
        {
            _installer.Write( item.TargetPath, text );
        }
        return success;
    }

    void Stop( IActivityMonitor monitor, bool success )
    {
        foreach( var h in _hooks )
        {
            h.StopInstall( monitor, success, _installer );
        }
    }

}
