namespace CK.TS.Angular;

/// <summary>
/// Category interface for the root public page is displayed to 
/// unauthenticated users (the anonymous).
/// <para>
/// Implementations must have a true <see cref="NgComponentAttribute.HasRoutes"/>.
/// </para>
/// </summary>
public interface INgPublicPageComponent : INgComponent
{
}
