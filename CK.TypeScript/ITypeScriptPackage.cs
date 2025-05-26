using CK.Core;

namespace CK.TypeScript;

/// <summary>
/// Category interface for <see cref="TypeScriptPackage"/>.
/// </summary>
public interface ITypeScriptPackage : IResourcePackage
{
    internal void LocalImplementationOnly();
}
