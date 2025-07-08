namespace CK.TS.Angular;

/// <summary>
/// Category interface for a page component.
/// <para>
/// A class (a <see cref="NgComponent"/>) that implements this interface is a page component that is
/// associated to a layout that can be a class that implements <see cref="INgPageLayoutComponent{T}"/>
/// or an interface that extends it.
/// </para>
/// <para>
/// An interface that extends this one defines a "type" of page component: there must be a
/// single class (a <see cref="NgComponent"/>) that implements it: <see cref="INgPublicPageComponent"/>
/// is the kind of the public page.
/// </para>
/// </summary>
public interface INgPageComponent : INgComponent 
{
}

