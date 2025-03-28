using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace CK.Core;

public sealed class TransformableFileHandler : ResourceSpaceFileHandler
{
    readonly TransformerHost _host;

    /// <summary>
    /// Initializes a new <see cref="TransformableFileHandler"/> for all the file extensions from
    /// <see cref="TransformerHost.LockLanguages()"/>'s <see cref="TransformLanguage.FileExtensions"/>.
    /// </summary>
    /// <param name="transformerHost">The transfomer host that will be used by this file handler.</param>
    public TransformableFileHandler( TransformerHost transformerHost )
        : base( transformerHost.LockLanguages().SelectMany( l => l.TransformLanguage.FileExtensions ).Distinct().ToImmutableArray() )
    {
        _host = transformerHost;
    }

    protected override bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData, FolderExclusion folderFilter )
    {
        bool success = true;
        var mappings = new Dictionary<NormalizedPath, ResourceLocator>();
        foreach( var r in spaceData.AllPackageResources )
        {
            success &= Register( monitor, r.Resources, mappings );
        }
        return success;
    }
}
