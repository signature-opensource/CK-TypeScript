using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Captures a folder like structure of <see cref="ResourceAsset"/>.
/// </summary>
public sealed class ResourceAssetSet
{
    readonly Dictionary<NormalizedPath,ResourceAsset> _assets;

    /// <summary>
    /// Initializes a new set.
    /// </summary>
    /// <param name="assets">The assets.</param>
    public ResourceAssetSet( Dictionary<NormalizedPath, ResourceAsset> assets )
    {
        _assets = assets;
    }

    /// <summary>
    /// Gets the assets.
    /// </summary>
    public IReadOnlyDictionary<NormalizedPath, ResourceAsset> Assets => _assets;

    /// <summary>
    /// Updates this assets the content of <paramref name="above"/>.
    /// If this set contains overrides, they apply and if it tries to redefine an existing
    /// resource, this is an error.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="above">The new resources to combine.</param>
    /// <param name="isPartialSet">
    /// When true, a regular override that overrides nothing is kept as it may apply to the eventual set.
    /// When false, a regular override that overrides nothing is discarded and a warning is emitted.
    /// </param>
    /// <returns>True on success, false on error.</returns>
    internal bool Combine( IActivityMonitor monitor, ResourceAssetSet above, bool isPartialSet )
    {
        bool success = true;
        foreach( var (path, asset) in above.Assets )
        {
            if( _assets.TryGetValue( path, out var ourAsset ) )
            {
                if( asset.Override is ResourceOverrideKind.None )
                {
                    monitor.Error( $"""
                                    Asset '{path}' in {asset.Origin.Container} overides the existing asset from {ourAsset.Origin.Container}.
                                    An explicit override declaration "O": [... "{path}" ...] is required.
                                    """ );
                    success = false;
                }
                else
                {
                    // Whether it is a "O", "?O" or "!O" we don't care here as we override.
                    _assets[path] = asset;
                }
            }
            else
            {
                // No existing asset.
                if( isPartialSet
                    || asset.Override is ResourceOverrideKind.None or ResourceOverrideKind.Always )
                {
                    _assets.Add( path, asset );
                }
                else
                {
                    Throw.DebugAssert( asset.Override is ResourceOverrideKind.Regular or ResourceOverrideKind.Optional );
                    if( asset.Override is ResourceOverrideKind.Regular )
                    {
                        monitor.Warn( $"Invalid override \"O\" for {asset.Origin}: the target asset doesn't exist, there's nothing to override." );
                    }
                    // ResourceOverrideKind.Optional doesn't add the resource and doesn't warn
                    // since it doesn't already exist. 
                }
            }
        }
        return success;
    }
}
