using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace CK.TypeScript.Engine;

/// <summary>
/// Used by CK.TypeScript.Engine to setup the state folder.
/// </summary>
public sealed class LiveStateBuilder
{
    readonly LiveStatePathContext _pathContext;
    readonly NormalizedPath _targetProjectPath;
    readonly IReadOnlySet<NormalizedCultureInfo> _activeCultures;
    readonly TSLocalesBuilder _locales;
    readonly List<LocalPackageRef> _localPackages;
    string? _watchRoot;

    public LiveStateBuilder( NormalizedPath targetProjectPath,
                             IReadOnlySet<NormalizedCultureInfo> activeCultures )
    {
        _pathContext = new LiveStatePathContext( targetProjectPath );
        _targetProjectPath = targetProjectPath;
        _activeCultures = activeCultures;
        _localPackages = new List<LocalPackageRef>();
        _locales = new TSLocalesBuilder();
    }

    public void ClearState( IActivityMonitor monitor )
    {
        var stateFile = _pathContext.PrimaryStateFile;
        if( File.Exists( stateFile ) )
        {
            int retryCount = 0;
            retry:
            try
            {
                File.Delete( stateFile );
                monitor.Trace( $"Deleted state file '{stateFile}'." );
            }
            catch( Exception ex )
            {
                if( ++retryCount < 3 )
                {
                    monitor.Warn( $"While clearing state file '{stateFile}'.", ex );
                    Thread.Sleep( retryCount * 100 );
                    goto retry;
                }
                monitor.Warn( $"Unable to delete state file '{stateFile}'.", ex );
            }
        }
    }

    public void AddRegularPackage( IActivityMonitor monitor,
                                   LocaleCultureSet? locales )
    {
        if( locales != null ) _locales.AddRegularPackage( monitor, locales );
    }

    public void AddLocalPackage( IActivityMonitor monitor, string localResPath, string displayName )
    {
        Throw.CheckArgument( Path.EndsInDirectorySeparator( localResPath ) );
        Throw.CheckNotNullOrEmptyArgument( displayName );
        var loc = new LocalPackageRef( localResPath, displayName, _localPackages.Count );
        _localPackages.Add( loc );
        if( _watchRoot == null )
        {
            _watchRoot = localResPath;
        }
        else
        {
            _watchRoot = CommonParentPath( _watchRoot, localResPath );
        }
        _locales.AddLocalPackage( monitor, loc );

        static string CommonParentPath( string path1, string path2 )
        {
            string[] p1 = path1.Split( Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries );
            string[] p2 = path2.Split( Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries );
            int len = p1.Length;
            if( len > p2.Length )
            {
                (p1,p2) = (p2,p1);
                len = p1.Length;
            }
            int iCommon = 0;
            while( iCommon < len && p1[iCommon] == p2[iCommon] ) ++iCommon;
            return iCommon == 0
                     ? string.Empty
                     : iCommon == len 
                        ? path1
                        : string.Join( Path.DirectorySeparatorChar, p1.Take( iCommon ) ) + Path.DirectorySeparatorChar;
        }
    }

    public bool WriteState( IActivityMonitor monitor )
    {
        using var _ = monitor.OpenInfo( $"Saving ck-watch live state. Watch root is '{_watchRoot}'." );
        bool success = true;
        if( !Directory.Exists( _pathContext.StateFolderPath ) )
        {
            monitor.Info( $"Creating '{_pathContext.StateFolderPath}' with '.gitignore' all." );
            Directory.CreateDirectory( _pathContext.StateFolderPath );
            File.WriteAllText( _pathContext.StateFolderPath + ".gitignore", "*" );
        }
        success &= _locales.WriteTSLocalesState( monitor, _pathContext.StateFolderPath );
        // Ends with the LiveState.dat.
        if( _watchRoot == null )
        {
            // If we have no packages, watchRoot is null: we'll only
            // watch the ck-gen-transform folder.
            Throw.DebugAssert( _localPackages.Count == 0 );
            _watchRoot = _pathContext.CKGenTransformPath;
        }
        success &= StateSerializer.WriteFile( monitor,
                                              _pathContext.PrimaryStateFile,
                                              ( monitor, w ) => StateSerializer.WriteLiveState( w,
                                                                                                _pathContext,
                                                                                                _watchRoot,
                                                                                                _activeCultures,
                                                                                                _localPackages ) );
        return success;
    }

}

