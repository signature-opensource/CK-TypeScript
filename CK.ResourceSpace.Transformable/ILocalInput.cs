using System.Threading;
using System;
using System.IO;
using System.Collections.Generic;
using CK.EmbeddedResources;

namespace CK.Core;

/// <summary>
/// Local imput is a <see cref="LocalFunctionSource"/> or a <see cref="LocalItem"/>.
/// </summary>
interface ILocalInput : IResourceInput
{
    /// <summary>
    /// Gets the full local path.
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Gets or sets the previous input in this <see cref="IResourceInput.Resources"/>.
    /// </summary>
    ILocalInput? Prev { get; set; }

    /// <summary>
    /// Gets or sets the next input in this <see cref="IResourceInput.Resources"/>.
    /// </summary>
    ILocalInput? Next { get; set; }

    /// <summary>
    /// Returns true if ApplyChanges must be called.
    /// False if nothing else must be done: on error, this input has been removed from the environment.
    /// For <see cref="LocalItem"/>, the <see cref="TransformableItem.TargetPath"/> file has been
    /// removed from the target folder.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="removedTargets">Collector for removed <see cref="TransformableItem.TargetPath"/>.</param>
    /// <returns>True if ApplyChanges must be called, false otherwise.</returns>
    bool InitializeApplyChanges( IActivityMonitor monitor, TransformEnvironment environment, ref HashSet<NormalizedPath>? removedTargets );


    void ApplyChanges( IActivityMonitor monitor, TransformEnvironment environment, HashSet<LocalItem> toBeInstalled );

    internal static bool TryReadText( IActivityMonitor monitor, ILocalInput input, out string? newText )
    {
        var origin = input.Origin;
        newText = SafeReadText( monitor, origin );
        if( string.IsNullOrEmpty( newText ) )
        {
            monitor.Info( $"Removing {origin}." );
            return false;
        }
        if( newText == input.Text )
        {
            monitor.Debug( $"No change for {origin}. Skipped." );
            newText = null;
        }
        return true;
    }

    internal static string? SafeReadText( IActivityMonitor monitor, ResourceLocator r )
    {
        string? newText = null;
        int retryCount = 0;
        var fullPath = r.LocalFilePath;
        retry:
        if( File.Exists( fullPath ) )
        {
            try
            {
                newText = File.ReadAllText( fullPath );
            }
            catch( Exception ex )
            {
                if( ++retryCount < 3 )
                {
                    monitor.Warn( $"While reading {r}.", ex );
                    Thread.Sleep( retryCount * 100 );
                    goto retry;
                }
                monitor.Error( $"Unable to read {r}'.", ex );
            }
        }

        return newText;
    }
}

