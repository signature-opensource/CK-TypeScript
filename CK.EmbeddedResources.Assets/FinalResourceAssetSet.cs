using CK.Core;
using System.Collections.Generic;

namespace CK.EmbeddedResources;

/// <summary>
/// Final asset set: this combines multiple asset sets into one <see cref="Final"/> set.
/// </summary>
public sealed class FinalResourceAssetSet
{
    readonly ResourceAssetSet _final;
    readonly bool _isPartialSet;

    /// <summary>
    /// Initializes a new empty final asset set.
    /// </summary>
    /// <param name="isPartialSet">
    /// True for the real final set of assets, false for an intermediate set of assets that keeps the override definitions.
    /// </param>
    public FinalResourceAssetSet( bool isPartialSet )
    {
        _final = new ResourceAssetSet( new Dictionary<NormalizedPath, ResourceAsset>() );
        _isPartialSet = isPartialSet;
    }

    /// <summary>
    /// Gets the final set.
    /// </summary>
    public ResourceAssetSet Final => _final;

    /// <summary>
    /// Updates this culture set with the content of <paramref name="newSet"/>.
    /// If the new set contains overrides, they apply and if it tries to redefine an already defined
    /// resource, this is an error.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="newSet">The new set of resources to combine.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Add( IActivityMonitor monitor, ResourceAssetSet newSet ) => _final.Combine( monitor, newSet, _isPartialSet );


}
