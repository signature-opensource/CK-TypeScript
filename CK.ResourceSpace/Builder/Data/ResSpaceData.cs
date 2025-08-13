using CK.EmbeddedResources;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core;

/// <summary>
/// <see cref="ResSpace"/> data is produced by the <see cref="ResSpaceDataBuilder"/>.
/// It contains the final, definitive set of <see cref="ResPackage"/> topologically ordered
/// but still allows resources to be altered.
/// <para>
/// This can be consumed by a <see cref="ResSpaceBuilder"/> that is configured with resource handlers
/// to eventually produce a <see cref="ResSpace"/>.
/// </para>
/// </summary>
public sealed class ResSpaceData
{
    readonly ResCoreData _data;
    readonly HashSet<ResourceLocator> _codeHandledResources;

    internal ResSpaceData( ResCoreData data, HashSet<ResourceLocator> codeHandledResources )
    {
        _data = data;
        _codeHandledResources = codeHandledResources;
    }

    /// <summary>
    /// Gets the core data that contains the <see cref="ResPackage"/>.
    /// </summary>
    public ResCoreData CoreData => _data;

    /// <summary>
    /// Gets or sets the configured Code generated resource container.
    /// This can only be set if this has not been previously set (ie. this is null).
    /// See <see cref="ResSpaceConfiguration.GeneratedCodeContainer"/>.
    /// </summary>
    [DisallowNull]
    public IResourceContainer? GeneratedCodeContainer
    {
        get => _data._generatedCodeContainer;
        set
        {
            Throw.CheckNotNullArgument( value );
            Throw.CheckState( "This can be set only once.", GeneratedCodeContainer is null );
            _data._generatedCodeContainer = value;
            // Ignore auto assignation: this just lock the container
            // (If the value is another ResourceContainerWrapper, this will throw and it's fine: we
            // don't want cycles!)
            if( _data._codePackage.AfterResources.Resources != value )
            {
                ((ResourceContainerWrapper)_data._codePackage.AfterResources.Resources).InnerContainer = value;
            }
        }
    }

}

