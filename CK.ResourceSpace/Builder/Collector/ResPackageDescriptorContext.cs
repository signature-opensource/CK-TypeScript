using CK.EmbeddedResources;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Provided context to the <see cref="ResPackageDescriptor"/>.
/// </summary>
sealed class ResPackageDescriptorContext
{
    readonly HashSet<ResourceLocator> _codeHandledResources;
    bool _closed;

    public ResPackageDescriptorContext()
    {
        _codeHandledResources = new HashSet<ResourceLocator>();
    }

    public bool Closed => _closed;

    /// <summary>
    /// IReadOnlySet to force the use of RegisterCodeHandledResources.
    /// StoreContainer constructor casts this to back to HashSet<ResourceLocator>.
    /// </summary>
    public IReadOnlySet<ResourceLocator> CodeHandledResources => _codeHandledResources;

    public void Close()
    {
        Throw.DebugAssert( !Closed );
        _closed = true;
    }

    public void RegisterCodeHandledResources( ResourceLocator resource )
    {
        Throw.CheckState( !Closed );
        _codeHandledResources.Add( resource );
    }
}
