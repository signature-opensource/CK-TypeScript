using CK.Core;
using CK.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Category interface for <see cref="NgComponent"/>.
/// </summary>
public interface INgComponent : ITypeScriptPackage
{
    internal void LocalImplementationOnly();
}
