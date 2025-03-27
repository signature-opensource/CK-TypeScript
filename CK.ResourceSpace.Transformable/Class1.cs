using CK.Core;
using System.Collections.Immutable;

namespace CK.ResourceSpace.Transformable;

public sealed class TransformableFileHandler : ResourceSpaceFileHandler
{
    public TransformableFileHandler( params ImmutableArray<string> fileExtensions )
        : base( fileExtensions )
    {
    }

    protected override bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData, FolderExclusion folderFilter )
    {
        throw new System.NotImplementedException();
    }
}
