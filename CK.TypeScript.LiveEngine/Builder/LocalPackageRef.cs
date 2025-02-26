using CK.Core;

namespace CK.TypeScript.LiveEngine;

sealed class LocalPackageRef
{
    readonly AssemblyResourceContainer _resources;
    readonly NormalizedPath _typeScriptFolder;
    readonly int _idxLocal;

    internal LocalPackageRef( AssemblyResourceContainer resources,
                              NormalizedPath typeScriptFolder,
                              int idxLocal )
    {
        _resources = resources;
        _typeScriptFolder = typeScriptFolder;
        _idxLocal = idxLocal;
    }

    public AssemblyResourceContainer Resources => _resources;

    public NormalizedPath TypeScriptFolder => _typeScriptFolder;

    public int IdxLocal => _idxLocal;

}
