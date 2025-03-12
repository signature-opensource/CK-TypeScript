using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.EmbeddedResources;

/// <summary>
/// Captures a folder like structure of <see cref="ResourceAsset"/>: a read only dictionary of <see cref="NormalizedPath"/>
/// to its <see cref="ResourceAsset"/>.
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
    /// 
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="baseContainer">The container whose assets must be combined with these assets.</param>
    /// <param name="defaultTargetPath">The default target path: a relative path in the final target root folder.</param>
    /// <param name="folder">The folder to load.</param>
    /// <param name="isPartialSet">
    /// When true, a regular override that overrides nothing is kept as it may apply to the final set.
    /// When false, a regular override that overrides nothing is discarded and a warning is emitted.
    /// <para>
    /// This defaults to true because this is primarily used to load combined locales from set of <see cref="IResourceContainer"/>
    /// for the locales set of a package that is not a final one (such locales sets are then combined with the sets
    /// of the package dependencies to build the final one).
    /// </para>
    /// </param>
    /// <returns>True on success, false on error.</returns>
    public bool LoadAndApplyBase( IActivityMonitor monitor,
                                  IResourceContainer baseContainer,
                                  NormalizedPath defaultTargetPath,
                                  string folder = "assets",
                                  bool isPartialSet = true )
    {
        Throw.CheckArgument( baseContainer != null && baseContainer.IsValid );
        if( !baseContainer.LoadAssets( monitor, defaultTargetPath, out var baseAssets, folder ) )
        {
            return false;
        }
        // If there's no base set, we have nothing to do.
        if( baseAssets == null )
        {
            return true;
        }
        return Combine( monitor, baseAssets, isPartialSet );
    }


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
