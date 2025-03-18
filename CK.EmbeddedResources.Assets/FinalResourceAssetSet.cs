using CK.Core;
using System.Collections.Generic;

namespace CK.EmbeddedResources;

/// <summary>
/// A final asset set combines any number of <see cref="ResourceAssetDefinitionSet"/>.
/// There is 3 ways to obtain a final asset set:
/// <list type="number">
///     <item>
///     From a <see cref="ResourceAssetDefinition"/> that is independent from any other assets (it must not define any override).
///     <para>
///     <see cref="ResourceAssetDefinitionSet.ToInitialFinalSet(IActivityMonitor)"/> provides this kind of
///     terminal, dependency-less final set.
///     </para>
///     </item>
///     <item>
///     By aggregating 2 final sets together. If the same target path associated to different resource exists,
///     the resulting set can be <see cref="IsAmbiguous"/> (it contains at least one non null <see cref="FinalResourceAsset.Ambiguities"/>).
///     <para>
///     <see cref="Aggregate(IActivityMonitor, FinalResourceAssetSet)"/> provides this kind of aggregated set.
///     </para>
///     </item>
///     <item>
///     By combining a <see cref="ResourceAssetDefinitionSet"/> to an existing <see cref="FinalResourceAssetSet"/>:
///     the override definitions can resolve ambiguities from the final set.
///     <para>
///     <see cref="ResourceAssetDefinitionSet.Combine(IActivityMonitor, FinalResourceAssetSet)"/> provides this kind of set.
///     </para>
///     </item>
/// </list>
/// The ultimate final set of a Direct Acyclic Graph must not be ambiguous.
/// <para>
/// This operation is commutative, associative and idempotent.
/// </para>
/// </summary>
public sealed class FinalResourceAssetSet
{
    readonly Dictionary<NormalizedPath, FinalResourceAsset> _assets;
    readonly bool _isAmbiguous;

    internal FinalResourceAssetSet( Dictionary<NormalizedPath, FinalResourceAsset> assets, bool isAmbiguous )
    {
        _assets = assets;
        _isAmbiguous = isAmbiguous;
    }

    /// <summary>
    /// Gets the final resource asset indexed by their target path.
    /// </summary>
    public IReadOnlyDictionary<NormalizedPath, FinalResourceAsset> Assets => _assets;

    /// <summary>
    /// Gets whether at least one of the <see cref="Assets"/> has a non null <see cref="FinalResourceAsset.Ambiguities"/>.
    /// </summary>
    public bool IsAmbiguous => _isAmbiguous;

    /// <summary>
    /// Aggregate this set with an other one. Even if both sets are not <see cref="IsAmbiguous"/>,
    /// if a comman target paths is mapped to different resource, the result will be amiguous.
    /// generate ambiguities.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public FinalResourceAssetSet? Aggregate( IActivityMonitor monitor, FinalResourceAssetSet other )
    {
        if( _assets.Count == 0 ) return other;
        if( other._assets.Count == 0 ) return this;
        // Iterate on the smallest to add/update into the biggest.
        var (s1, s2) = (this, other);
        if( _assets.Count > other._assets.Count ) (s1, s2) = (s2, s1);
        var result = new Dictionary<NormalizedPath, FinalResourceAsset>( s2._assets );
        bool isAmbiguous = s2._isAmbiguous;
        foreach( var (path,f1) in s1._assets )
        {
            if( result.TryGetValue( path, out var f2 ) )
            {
                var f = f1.AddAmbiguities( f2.Ambiguities );
                isAmbiguous |= f.Ambiguities != null;
                result[path] = f;
            }
            else
            {
                result.Add( path, f1 );
            }
        }
        return new FinalResourceAssetSet( result, isAmbiguous );
    }

}
