using CK.Core;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.TypeScript.LiveEngine;

public sealed class LiveState
{
    /// <summary>
    /// Gets the name of the watcher.
    /// </summary>
    public const string WatcherAppName = "ck-watch";

    /// <summary>
    /// Gets the name of the watcher folder state from the target project path.
    /// </summary>
    public const string FolderWatcher = "ck-gen-transform/." + WatcherAppName;


    internal const string FileName = "LiveState.dat";

    readonly NormalizedPath _targetProjectPath;
    readonly NormalizedPath _watchRoot;
    readonly NormalizedPath _loadFolder;
    readonly ImmutableArray<LocalPackage> _localPackages;
    readonly HashSet<NormalizedCultureInfo> _activeCultures;


    internal LiveState( NormalizedPath targetProjectPath,
                        NormalizedPath watchRoot,
                        HashSet<NormalizedCultureInfo> activeCultures,
                        NormalizedPath loadFolder,
                        ImmutableArray<LocalPackage> localPackages )
    {
        _targetProjectPath = targetProjectPath;
        _watchRoot = watchRoot;
        _activeCultures = activeCultures;
        _loadFolder = loadFolder;
        _localPackages = localPackages.IsDefault ? ImmutableArray<LocalPackage>.Empty : localPackages;
    }

    public NormalizedPath LoadFolder => _loadFolder;

    internal HashSet<NormalizedCultureInfo> ActiveCultures => _activeCultures;

    internal ImmutableArray<LocalPackage> LocalPackages => _localPackages;

    public static LiveState? Load( IActivityMonitor monitor, NormalizedPath targetProjectPath )
    {
        var loadFolder = targetProjectPath.Combine( FolderWatcher );
        var result = StateSerializer.ReadFile( monitor,
                                               loadFolder.AppendPart( FileName ),
                                               ( monitor, r ) => StateSerializer.ReadLiveState( monitor, r, loadFolder ) );
        if( result != null )
        {

        }
        return result;
    }

}

