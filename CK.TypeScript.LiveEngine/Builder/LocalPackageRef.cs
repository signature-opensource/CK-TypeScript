using CK.Core;

namespace CK.TypeScript.LiveEngine;

sealed class LocalPackageRef
{
    readonly EmptyResourceContainer _resources;
    readonly NormalizedPath _typeScriptFolder;
    readonly int _idxLocal;

    internal LocalPackageRef( string localResPath,
                              string displayName,
                              NormalizedPath typeScriptFolder,
                              int idxLocal )
    {
        _resources = new EmptyResourceContainer( displayName, localResPath );
        _typeScriptFolder = typeScriptFolder;
        _idxLocal = idxLocal;
    }

    public EmptyResourceContainer Resources => _resources;

    public NormalizedPath TypeScriptFolder => _typeScriptFolder;

    public int IdxLocal => _idxLocal;
}
