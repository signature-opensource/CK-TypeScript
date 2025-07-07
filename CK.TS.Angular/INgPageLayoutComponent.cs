namespace CK.TS.Angular;

/// <summary>
/// Category interface for the layout of a page component: the <see cref="NgComponent"/> that
/// implements this interface provides the layout of the <see cref="INgPageComponent"/>.
/// </summary>
public interface INgPageLayoutComponent<T> : INgComponent where T : class, INgPageComponent
{
}

