using CK.Core;
using CK.EmbeddedResources;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    readonly ImmutableArray<RegularPackage> _regularPackages;
    readonly CKBinaryReader.ObjectPool<IResourceContainer> _resourceContainerPool;
    readonly FileSystemResourceContainer _ckGenTransform;

    readonly LiveTSLocales _tsLocales;
    readonly LiveAssets _assets;

    internal LiveState( LiveStatePathContext pathContext,
                        string watchRoot,
                        HashSet<NormalizedCultureInfo> activeCultures,
                        ImmutableArray<LocalPackage> localPackages,
                        ImmutableArray<RegularPackage> regularPackages,
                        CKBinaryReader.ObjectPool<IResourceContainer> resourceContainerPool )
    {
        _pathContext = pathContext;
        _watchRoot = watchRoot;
        _activeCultures = activeCultures;
        _regularPackages = regularPackages;
        _resourceContainerPool = resourceContainerPool;
        _localPackages = localPackages.IsDefault ? ImmutableArray<LocalPackage>.Empty : localPackages;
        _ckGenTransform = new FileSystemResourceContainer( pathContext.CKGenTransformPath, "ck-gen-transform" );
        _tsLocales = new LiveTSLocales( this );
        _assets = new LiveAssets( this );
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

    /// <summary>
    /// Gets the local packages that are basically FileSystemResourceContainer on the local folder.
    /// </summary>
    public ImmutableArray<LocalPackage> LocalPackages => _localPackages;

    /// <summary>
    /// Gets the regular packages.
    /// </summary>
    public ImmutableArray<RegularPackage> RegularPackages => _regularPackages;

    /// <summary>
    /// Called by the file watcher.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="package">The package that owns the changed resource or null when it is the ck-gen-transform/ folder.</param>
    /// <param name="subPath">The path in the resource folder.</param>
    public void OnChange( IActivityMonitor monitor, LocalPackage? package, string subPath )
    {
        // If anything change in a ts-locales/ subfolder wherever it is, we recompute the
        // locales: the LocalesState is loaded once and only when needed.
        if( subPath.StartsWith( "ts-locales" ) && (subPath.Length == 10 || subPath[10] == Path.DirectorySeparatorChar) )
        {
            Throw.DebugAssert( "ts-locales".Length == 10 );
            // Load on demand.
            if( !_tsLocales.IsLoaded ) _tsLocales.Load( monitor,_resourceContainerPool );
            if( _tsLocales.IsValid )
            {
                _tsLocales.OnChange( monitor, package, subPath );
            }
        }
        else if( subPath.StartsWith( "assets" ) && (subPath.Length == 6 || subPath[6] == Path.DirectorySeparatorChar) )
        {
            Throw.DebugAssert( "assets".Length == 6 );
            _assets.OnChange( monitor, package, subPath );
        }
    }

    /// <summary>
    /// Applies any changes.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    public void ApplyChanges( IActivityMonitor monitor )
    {
        if( _tsLocales.IsDirty ) _tsLocales.ApplyChanges( monitor );
        if( _assets.IsDirty ) _assets.ApplyChanges( monitor );
    }

    public static async Task<LiveState> WaitForStateAsync( IActivityMonitor monitor, LiveStatePathContext pathContext, CancellationToken cancellation )
    {
        var liveState = Load( monitor, pathContext );
        while( liveState == null )
        {
            await Task.Delay( 500, cancellation );
            liveState = Load( monitor, pathContext );
        }
        return liveState;
    }

    static LiveState? Load( IActivityMonitor monitor, LiveStatePathContext pathContext )
    {
        if( !File.Exists( pathContext.PrimaryStateFile ) ) return null;
        var result = StateSerializer.ReadFile( monitor,
                                               pathContext.PrimaryStateFile,
                                               ( monitor, r ) => StateSerializer.ReadLiveState( monitor, r, pathContext ) );
        return result;
    }

    internal bool LoadExtensions( IActivityMonitor monitor, CKBinaryReader r )
    {
        return _assets.Load( monitor, r, _resourceContainerPool );
    }
}

