using CK.Core;
using CK.EmbeddedResources;
using CK.TypeScript.LiveEngine;
using System;
using System.Collections.Generic;
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
    readonly IReadOnlySet<NormalizedCultureInfo> _activeCultures;
    readonly TSLocalesBuilder _locales;
    readonly AssetsBuilder _assets;
    readonly List<RegularPackageRef> _regularPackages;
    readonly List<LocalPackageRef> _localPackages;
    string? _watchRoot;

    public LiveStateBuilder( NormalizedPath targetProjectPath,
                             IReadOnlySet<NormalizedCultureInfo> activeCultures )
    {
        _pathContext = new LiveStatePathContext( targetProjectPath );
        _activeCultures = activeCultures;
        _localPackages = new List<LocalPackageRef>();
        _regularPackages = new List<RegularPackageRef>();
        _locales = new TSLocalesBuilder();
        _assets = new AssetsBuilder();
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
                                   AssemblyResourceContainer resources,
                                   NormalizedPath typeScriptFolder,
                                   LocaleCultureSet? locales,
                                   ResourceAssetSet? assets )
    {
        var reg = new RegularPackageRef( resources, typeScriptFolder, _regularPackages.Count );
        _regularPackages.Add( reg );
        if( locales != null ) _locales.AddRegularPackage( monitor, locales );
        if( assets != null ) _assets.AddRegularPackage( monitor, assets );
    }

    public void AddLocalPackage( IActivityMonitor monitor,
                                 FileSystemResourceContainer resources,
                                 NormalizedPath typeScriptFolder )
    {
        var loc = new LocalPackageRef( resources, typeScriptFolder, _localPackages.Count );
        _localPackages.Add( loc );
        if( _watchRoot == null )
        {
            _watchRoot = resources.ResourcePrefix;
        }
        else
        {
            _watchRoot = CommonParentPath( _watchRoot, resources.ResourcePrefix );
        }
        _locales.AddLocalPackage( monitor, loc );
        _assets.AddLocalPackage( monitor, loc );

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

    public void SetFinalAssets( ResourceAssetSet final ) => _assets.SetFinalAssets( final );

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
        // LocalesState is independent.
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
                                                                                                _regularPackages,
                                                                                                _localPackages,
                                                                                                _assets ) );
        return success;
    }

}

