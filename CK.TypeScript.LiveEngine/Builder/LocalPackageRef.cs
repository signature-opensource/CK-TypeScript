namespace CK.TypeScript.LiveEngine;

sealed class LocalPackageRef
{
    readonly string _localResPath;
    readonly string _displayName;
    readonly int _idxLocal;

    internal LocalPackageRef( string localResPath, string displayName, int idxLocal )
    {
        _localResPath = localResPath;
        _displayName = displayName;
        _idxLocal = idxLocal;
    }

    public string LocalResPath => _localResPath;

    public string DisplayName => _displayName;

    public int IdxLocal => _idxLocal;
}
