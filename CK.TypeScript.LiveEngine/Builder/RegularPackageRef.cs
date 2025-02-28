using CK.Core;
using CK.EmbeddedResources;

namespace CK.TypeScript.LiveEngine;

sealed class RegularPackageRef
{
    readonly AssemblyResourceContainer _resources;
    readonly NormalizedPath _typeScriptFolder;
    readonly int _idxRegular;

    internal RegularPackageRef( AssemblyResourceContainer resources,
                                NormalizedPath typeScriptFolder,
                                int idxRegular )
    {
        _resources = resources;
        _typeScriptFolder = typeScriptFolder;
        _idxRegular = idxRegular;
    }

    public AssemblyResourceContainer Resources => _resources;

    public NormalizedPath TypeScriptFolder => _typeScriptFolder;

    public int IdxRegular => _idxRegular;
}
