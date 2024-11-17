using System;
using System.IO;
using System.Text;

namespace CK.Core;

/// <summary>
/// Locator for a resource from a type and a resource name in a <see cref="IResourceContainer"/>.
/// <para>
/// This is a record struct that benefits of the ToString (PrintMembers) and equality code generation but with
/// an explicit constructor to handle the only <c>default</c> value that is <see cref="IsValid"/> false.
/// The <c>default</c> value makes <see cref="Nullable{T}"/> useless for this type.
/// </para>
/// </summary>
public readonly struct ResourceLocator : IEquatable<ResourceLocator>
{
    /// <summary>
    /// Initializes a new resource locator.
    /// </summary>
    /// <param name="container">The type resources that contains this resource.</param>
    /// <param name="fullResourceName">the resource name in the <paramref name="container"/>. Must not be null, empty or whitespace.</param>
    public ResourceLocator( IResourceContainer container, string fullResourceName )
    {
        Throw.CheckNotNullArgument( container );
        Throw.CheckNotNullOrWhiteSpaceArgument( fullResourceName );
        Container = container;
        ResourceName = fullResourceName;
    }

    /// <summary>
    /// Gets whether this locator is valid: the <see cref="Container"/> is not null
    /// and the <see cref="ResourceName"/> is not null, not empty nor whitespace.
    /// <para>
    /// Whether the resource actuallly exists or not is not known.
    /// </para>
    /// Only the <c>default</c> of this type is invalid.
    /// </summary>
    public bool IsValid => Container != null;

    /// <summary>
    /// Gets the type resources that contains this resource.
    /// </summary>
    public IResourceContainer Container { get; }

    /// <summary>
    /// Gets the resource name in the <see cref="Container"/>.
    /// <para>
    /// This is the full resource name that includes the <see cref="IResourceContainer.ResourcePrefix"/>.
    /// </para>
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets the resource name without the <see cref="IResourceContainer.ResourcePrefix"/>.
    /// </summary>
    public ReadOnlyMemory<char> LocalResourceName => ResourceName.AsMemory( Container.ResourcePrefix.Length.. );

    /// <summary>
    /// To be equal the <see cref="Container"/> must be the same and <see cref="ResourceName"/>
    /// must be equal for the <see cref="IResourceContainer.ResourceNameComparer"/>.
    /// </summary>
    /// <param name="other">The other locator.</param>
    /// <returns>True if the resources are the same.</returns>
    public bool Equals( ResourceLocator other )
    {
        return Container == other.Container
               && (!IsValid || Container.ResourceNameComparer.Equals( ResourceName, other.ResourceName ));
    }

    /// <summary>
    /// Gets the resource content.
    /// If a stream cannot be obtained, a detailed <see cref="IOException"/> is raised.
    /// </summary>
    /// <returns>The resource's content stream.</returns>
    public Stream GetStream()
    {
        Throw.CheckState( IsValid );
        return Container.GetStream( this );
    }

    /// <summary>
    /// Gets "{ ResourceName: '...', Container: '...' }" when <see cref="IsValid"/> is true
    /// or the empty string when IsValid is false.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return IsValid
                ? $"{{ ResourceName: '{ResourceName}', Container: '{Container.DisplayName}' }}"
                : "";
    }

    public override bool Equals( object? obj ) => obj is ResourceLocator locator && Equals( locator );

    public static bool operator ==( ResourceLocator left, ResourceLocator right ) => left.Equals( right );

    public static bool operator !=( ResourceLocator left, ResourceLocator right ) => !(left == right);

    public override int GetHashCode() => IsValid
                                            ? HashCode.Combine( Container.GetHashCode(), Container.ResourceNameComparer.GetHashCode( ResourceName ) )
                                            : 0;


}
