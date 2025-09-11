using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.IO;

sealed class CKGenAppFilter : IFileEventFilter
{
    readonly string _liveStateFilePath;
    readonly LiveState _liveState;

    public static object PrimaryStateFileChanged => typeof(CKGenAppFilter);

    public CKGenAppFilter( string liveStateFilePath, LiveState liveState )
    {
        _liveStateFilePath = liveStateFilePath;
        _liveState = liveState;
    }

    public object? GetChange( string path )
    {
        var ckGenApp = _liveState.CKGenAppPath;
        if( path.Length > ckGenApp.Length
            && path[ckGenApp.Length-1] == Path.DirectorySeparatorChar )
        {
            // We are in ck-gen-app/ folder.
            // We may be the ck-gen-app/.ck-watch/LiveState.dat file. When this one
            // changes, it is because a setup deleted it.
            // We stop the current run and wait for a new one.
            if( path == _liveStateFilePath )
            {
                return PrimaryStateFileChanged;
            }
            // Not the "ck-gen-app/.ck-watch/LiveState.dat" file but if we are in
            // the "ck-gen-app/.ck-watch/", we ignore it: may be handlers have updated
            // cached files and it's not our business.
            if( path.StartsWith( ckGenApp, StringComparison.Ordinal )
                && !path.StartsWith( _liveState.SpaceData.LiveStatePath ) )
            {
                Throw.DebugAssert( _liveState.SpaceData.AppPackage.Resources.LocalPath == ckGenApp );
                return new PathChangedEvent( _liveState.SpaceData.AppPackage.Resources, path );
            }
        }
        return null;
    }
}
