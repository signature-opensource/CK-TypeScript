namespace CK.TS.Angular;

/// <summary>
/// Category interface for the root private page that can be accessed
/// only by authenticated users.
/// <para>
/// Implementations must have a true <see cref="NgComponentAttribute.HasRoutes"/>.
/// </para>
/// </summary>
[NgSingleAbstractComponent( "private-page" )]
public interface INgPrivatePageComponent : INgComponent
{
}
