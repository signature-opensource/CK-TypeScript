using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CK.TypeScript.Engine;

/// <summary>
/// Used by CK.TypeScript.Engine to setup the state folder.
/// </summary>
public sealed class LiveStateBuilder
{
    readonly NormalizedPath _targetProjectPath;
    readonly IReadOnlySet<NormalizedCultureInfo> _activeCultures;
    readonly TSLocalesBuilder _locales;
    readonly List<LocalPackageRef> _localPackages;
    readonly NormalizedPath _stateFolder;
    NormalizedPath _watchRoot;

    public LiveStateBuilder( NormalizedPath targetProjectPath,
                             IReadOnlySet<NormalizedCultureInfo> activeCultures )
    {
        _targetProjectPath = targetProjectPath;
        _stateFolder = targetProjectPath.Combine( LiveState.FolderWatcher );
        _activeCultures = activeCultures;
        _localPackages = new List<LocalPackageRef>();
        _locales = new TSLocalesBuilder();
    }

    public void ClearState( IActivityMonitor monitor )
    {
        var stateFile = _stateFolder.AppendPart( LiveState.FileName );
        if( File.Exists( stateFile ) )
        {
            int retryCount = 0;
            retry:
            try
            {
                File.Delete( stateFile );
            }
            catch( Exception ex )
            {
                if( ++retryCount < 3 )
                {
                    monitor.Warn( $"While clearing state file '{stateFile}'.", ex );
                    Thread.Sleep( retryCount * 100 );
                    goto retry;
                }
                monitor.Warn( $"Unable to detete state file '{stateFile}'.", ex );
            }
        }
    }

    public void AddRegularPackage( IActivityMonitor monitor,
                                   LocaleCultureSet? locales )
    {
        if( locales != null ) _locales.AddRegularPackage( monitor, locales );
    }

    public void AddLocalPackage( IActivityMonitor monitor, NormalizedPath localResPath, string displayName )
    {
        var loc = new LocalPackageRef( localResPath, displayName, _localPackages.Count );
        _localPackages.Add( loc );
        if( _localPackages.Count == 1 )
        {
            _watchRoot = localResPath;
        }
        else
        {
            _watchRoot = CommonParentPath( _watchRoot, localResPath );
        }
        _locales.AddLocalPackage( monitor, loc );

        static NormalizedPath CommonParentPath( NormalizedPath p1, NormalizedPath p2 )
        {
            int len = p1.Parts.Count;
            if( len > p2.Parts.Count )
            {
                (p1,p2) = (p2,p1);
                len = p1.Parts.Count;
            }
            int iCommon = 0;
            while( iCommon < len && p1.Parts[iCommon] == p2.Parts[iCommon] ) ++iCommon;
            return iCommon == 0
                     ? default
                     : len == iCommon
                        ? p1
                        : p1.RemoveParts( iCommon, len - iCommon );
        }

    }

    public bool WriteState( IActivityMonitor monitor )
    {
        bool success = true;
        if( !Directory.Exists( _stateFolder ) )
        {
            monitor.Info( $"Creating '{LiveState.FolderWatcher}' folder with '.gitignore' all." );
            Directory.CreateDirectory( _stateFolder );
            File.WriteAllText( _stateFolder.AppendPart( ".gitignore" ), "*" );
        }
        success &= _locales.WriteTSLocalesState( monitor, _stateFolder );
        // Ends with the LiveState.dat.
        monitor.Info( $"Watch root is '{_watchRoot}'." );
        success &= StateSerializer.WriteFile( monitor,
                                              _stateFolder.AppendPart( LiveState.FileName ),
                                              ( monitor, w ) => StateSerializer.WriteLiveState( w,
                                                                                                _targetProjectPath,
                                                                                                _watchRoot,
                                                                                                _activeCultures,
                                                                                                _localPackages ) );
        return success;
    }

}

