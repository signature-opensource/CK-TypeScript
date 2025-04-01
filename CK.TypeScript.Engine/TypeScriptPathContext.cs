using CK.Setup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.TypeScript.Engine;

/// <summary>
/// Captures the 3 physical paths required for type script generation.
/// </summary>
public sealed class TypeScriptPathContext
{
    readonly string _targetProjectPath;
    readonly string _ckGenPath;
    readonly string _ckGenAppPath;

    public TypeScriptPathContext( TypeScriptBinPathAspectConfiguration tsConfig )
        : this( tsConfig.TargetProjectPath )
    {
    }

    public TypeScriptPathContext( string targetProjectPath )
    {
        targetProjectPath = Path.GetFullPath( targetProjectPath );
        if( !Path.EndsInDirectorySeparator( targetProjectPath ) ) targetProjectPath += Path.DirectorySeparatorChar;
        _targetProjectPath = targetProjectPath;
        _ckGenPath = targetProjectPath + "ck-gen" + Path.DirectorySeparatorChar;
        _ckGenAppPath = targetProjectPath + "ck-gen-app" + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Gets the fully qualified target project path ending with <see cref="Path.DirectorySeparatorChar"/>.
    /// </summary>
    public string TargetProjectPath => _targetProjectPath;

    /// <summary>
    /// Gets the fully qualified "ck-gen/" path ending with <see cref="Path.DirectorySeparatorChar"/>.
    /// </summary>
    public string CKGenPath => _ckGenPath;

    /// <summary>
    /// Gets the fully qualified "ck-gen-app/" path ending with <see cref="Path.DirectorySeparatorChar"/>.
    /// </summary>
    public string CKGenAppPath => _ckGenAppPath;
}
