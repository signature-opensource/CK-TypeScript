using CK.Core;
using CK.TypeScript;

namespace CK.TS.Angular;

/// <summary>
/// Category interface for the root public page that is displayed to 
/// unauthenticated users (the anonymous).
/// Only one <see cref="NgComponent"/> of this kind can be registered.
/// <para>
/// Implementations must have a true <see cref="NgComponentAttribute.HasRoutes"/>.
/// </para>
/// </summary>
public interface INgPublicPageComponent : INgPageComponent
{
}

