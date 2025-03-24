using CK.Setup;
using CK.TypeScript.LiveEngine;
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
    readonly string _ckGenTransformPath;

    public TypeScriptPathContext( TypeScriptBinPathAspectConfiguration tsConfig )
    {
        var targetProjectPath = Path.GetFullPath( tsConfig.TargetProjectPath );
        if( !Path.EndsInDirectorySeparator( targetProjectPath ) ) targetProjectPath += Path.DirectorySeparatorChar;
        _targetProjectPath = targetProjectPath;
        _ckGenPath = targetProjectPath + "ck-gen" + Path.DirectorySeparatorChar;
        _ckGenTransformPath = targetProjectPath + "ck-gen-transform" + Path.DirectorySeparatorChar;
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
    /// Gets the fully qualified "ck-gen-transform/" path ending with <see cref="Path.DirectorySeparatorChar"/>.
    /// </summary>
    public string CKGenTransformPath => _ckGenTransformPath;
}
