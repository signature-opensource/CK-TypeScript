using CK.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace CK.TypeScript.LiveEngine;

public sealed class LiveState
{
    /// <summary>
    /// Gets the name of the watcher.
    /// </summary>
    public const string WatcherAppName = "ck-ts-watch";

    /// <summary>
    /// Gets the name of the watcher folder state from the target project path.
    /// </summary>
    public const string FolderWatcher = "ck-gen-transform/." + WatcherAppName;


    internal const string FileName = "LiveState.dat";

    readonly NormalizedPath _targetProjectPath;
    readonly NormalizedPath _ckGenFolder;
    readonly NormalizedPath _watchRoot;
    readonly NormalizedPath _loadFolder;
    readonly NormalizedPath _rootStateFile;
    readonly ImmutableArray<LocalPackage> _localPackages;
    readonly HashSet<NormalizedCultureInfo> _activeCultures;
    readonly FileSystemResourceContainer _ckGenTransform;


    internal LiveState( NormalizedPath targetProjectPath,
                        NormalizedPath watchRoot,
                        HashSet<NormalizedCultureInfo> activeCultures,
                        NormalizedPath loadFolder,
                        NormalizedPath rootStateFile,
                        ImmutableArray<LocalPackage> localPackages )
    {
        _targetProjectPath = targetProjectPath;
        _ckGenFolder = targetProjectPath.AppendPart( "ck-gen" );
        _watchRoot = watchRoot;
        _activeCultures = activeCultures;
        _loadFolder = loadFolder;
        _rootStateFile = rootStateFile;
        _localPackages = localPackages.IsDefault ? ImmutableArray<LocalPackage>.Empty : localPackages;
        _ckGenTransform = new FileSystemResourceContainer( targetProjectPath.AppendPart( "ck-gen-transform" ), "ck-gen-transform" );
    }

    /// <summary>
    /// Gets the "ck-gen-transform/.<see cref="WatcherAppName"/>" path.
    /// </summary>
    public NormalizedPath LoadFolder => _loadFolder;

    /// <summary>
    /// Gets the folder path that contains all the <see cref="LocalPackages"/> resources folders.
    /// </summary>
    public NormalizedPath WatchRoot => _watchRoot;

    /// <summary>
    /// Gets the full path of the file that contains the state file.
    /// </summary>
    public NormalizedPath RootStateFile => _rootStateFile;

    public NormalizedPath CKGenFolder => _ckGenFolder;

    public IResourceContainer CKGenTransform => _ckGenTransform;

    internal NormalizedPath TargetProjectPath => _targetProjectPath;

    internal HashSet<NormalizedCultureInfo> ActiveCultures => _activeCultures;

    public ImmutableArray<LocalPackage> LocalPackages => _localPackages;

    public static LiveState? Load( IActivityMonitor monitor, NormalizedPath targetProjectPath )
    {
        var loadFolder = targetProjectPath.Combine( FolderWatcher );
        var rootStateFile = loadFolder.AppendPart( FileName );
        if( !File.Exists( rootStateFile ) ) return null;

        var result = StateSerializer.ReadFile( monitor,
                                               rootStateFile,
                                               ( monitor, r ) => StateSerializer.ReadLiveState( monitor, r, loadFolder, rootStateFile ) );
        if( result != null )
        {

        }
        return result;
    }

}

