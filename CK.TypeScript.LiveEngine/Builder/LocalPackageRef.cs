using CK.Core;
using CK.EmbeddedResources;

namespace CK.TypeScript.LiveEngine;

sealed class LocalPackageRef
{
    readonly FileSystemResourceContainer _resources;
    readonly NormalizedPath _typeScriptFolder;
    readonly int _idxLocal;

    internal LocalPackageRef( FileSystemResourceContainer resources,
                              NormalizedPath typeScriptFolder,
                              int idxLocal )
    {
        _resources = resources;
        _typeScriptFolder = typeScriptFolder;
        _idxLocal = idxLocal;
    }

    public FileSystemResourceContainer Resources => _resources;

    public NormalizedPath TypeScriptFolder => _typeScriptFolder;

    public int IdxLocal => _idxLocal;

}
