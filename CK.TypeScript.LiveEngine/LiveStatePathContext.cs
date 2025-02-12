using System.IO;

namespace CK.TypeScript.LiveEngine;

public sealed class LiveStatePathContext
{
    readonly string _targetProjectPath;
    readonly string _ckGenPath;
    readonly string _ckGenTransformPath;
    readonly string _stateFolderPath;
    readonly string _primaryStateFile;

    public LiveStatePathContext( string targetProjectPath )
    {
        targetProjectPath = Path.GetFullPath( targetProjectPath );
        if( !Path.EndsInDirectorySeparator( targetProjectPath ) ) targetProjectPath += Path.DirectorySeparatorChar;
        _targetProjectPath = targetProjectPath;
        _ckGenPath = targetProjectPath + "ck-gen" + Path.DirectorySeparatorChar;
        _ckGenTransformPath = targetProjectPath + "ck-gen-transform" + Path.DirectorySeparatorChar;
        _stateFolderPath = _ckGenTransformPath + ".ck-watch" + Path.DirectorySeparatorChar;
        _primaryStateFile = _stateFolderPath + LiveState.StateFileName;
    }

    public string TargetProjectPath => _targetProjectPath;

    public string CKGenPath => _ckGenPath;

    public string CKGenTransformPath => _ckGenTransformPath;

    public string StateFolderPath => _stateFolderPath;

    public string PrimaryStateFile => _primaryStateFile;
}
