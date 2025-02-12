using CK.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;

namespace CK.TypeScript.LiveEngine;

public sealed class LiveState
{
    /// <summary>
    /// Gets the name of the state file.
    /// </summary>
    public const string StateFileName = "LiveState.dat";

    readonly LiveStatePathContext _pathContext;
    readonly string _watchRoot;
    readonly ImmutableArray<LocalPackage> _localPackages;
    readonly HashSet<NormalizedCultureInfo> _activeCultures;
    readonly FileSystemResourceContainer _ckGenTransform;

    readonly LiveTSLocales _tsLocales;

    internal LiveState( LiveStatePathContext pathContext,
                        string watchRoot,
                        HashSet<NormalizedCultureInfo> activeCultures,
                        ImmutableArray<LocalPackage> localPackages )
    {
        _pathContext = pathContext;
        _watchRoot = watchRoot;
        _activeCultures = activeCultures;
        _localPackages = localPackages.IsDefault ? ImmutableArray<LocalPackage>.Empty : localPackages;
        _ckGenTransform = new FileSystemResourceContainer( pathContext.CKGenTransformPath, "ck-gen-transform" );
        _tsLocales = new LiveTSLocales( this );
    }

    /// <summary>
    /// Gets the paths.
    /// </summary>
    public LiveStatePathContext Paths => _pathContext;

    /// <summary>
    /// Gets the common folder path that contains all the <see cref="LocalPackages"/> resources folders.
    /// </summary>
    public string WatchRootPath => _watchRoot;

    public IResourceContainer CKGenTransform => _ckGenTransform;

    internal HashSet<NormalizedCultureInfo> ActiveCultures => _activeCultures;

    public ImmutableArray<LocalPackage> LocalPackages => _localPackages;

    public void OnChange( IActivityMonitor monitor, LocalPackage? package, string subPath )
    {
        Throw.DebugAssert( "ts-locales".Length == 10 );
        if( subPath.StartsWith( "ts-locales" ) && (subPath.Length == 10 || subPath[10] == Path.DirectorySeparatorChar) )
        {
            _tsLocales.Apply( monitor );
        }
    }

    public static LiveState? Load( IActivityMonitor monitor, LiveStatePathContext pathContext )
    {
        if( !File.Exists( pathContext.PrimaryStateFile ) ) return null;
        var result = StateSerializer.ReadFile( monitor,
                                               pathContext.PrimaryStateFile,
                                               ( monitor, r ) => StateSerializer.ReadLiveState( monitor, r, pathContext ) );
        if( result != null )
        {
            bool success = true;
            success &= result._tsLocales.Load( monitor );
            if( !success ) result = null;
        }
        return result;
    }

}

