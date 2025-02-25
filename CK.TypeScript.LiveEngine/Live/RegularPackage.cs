using CK.Core;

namespace CK.TypeScript.LiveEngine;

/// <summary>
/// Captures a regular package. Its <see cref="Resources"/> are in
/// a <see cref="AssemblyResourceContainer"/>.
/// </summary>
public sealed class RegularPackage
{
    readonly AssemblyResourceContainer _resources;
    readonly NormalizedPath _typeScriptFolder;

    public RegularPackage( AssemblyResourceContainer resources, NormalizedPath typeScriptFolder )
    {
        _resources = resources;
        _typeScriptFolder = typeScriptFolder;
    }

    /// <summary>
    /// Gets the package "Res/" folder resources.
    /// </summary>
    public AssemblyResourceContainer Resources => _resources;

    /// <summary>
    /// Gets the relative path in the ck-gen/ folder for this package.
    /// </summary>
    public NormalizedPath TypeScriptFolder => _typeScriptFolder;
}

