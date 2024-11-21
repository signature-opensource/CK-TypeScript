using System;
using System.IO;

namespace CK.Core;

/// <summary>
/// Locator for a resource in a <see cref="IResourceContainer"/>.
/// <para>
/// The <c>default</c> value is <see cref="IsValid"/> false: this makes <see cref="Nullable{T}"/> useless for this type.
/// </para>
/// </summary>
public readonly struct ResourceLocator : IEquatable<ResourceLocator>
{
    readonly IResourceContainer _container;
    readonly string _resourceName;

    /// <summary>
    /// Initializes a new resource locator.
    /// </summary>
    /// <param name="container">The type resources that contains this resource.</param>
    /// <param name="fullResourceName">the resource name in the <paramref name="container"/>. Must not be null, empty or whitespace.</param>
    public ResourceLocator( IResourceContainer container, string fullResourceName )
    {
        Throw.CheckNotNullArgument( container );
        Throw.CheckNotNullOrWhiteSpaceArgument( fullResourceName );
        _container = container;
        _resourceName = fullResourceName;
    }

    /// <summary>
    /// Gets whether this locator is valid: the <see cref="Container"/> is not null
    /// and the <see cref="ResourceName"/> is not null, not empty nor whitespace.
    /// <para>
    /// Whether the resource actuallly exists or not is not known.
    /// </para>
    /// Only the <c>default</c> of this type is invalid.
    /// </summary>
    public bool IsValid => _container != null;

    /// <summary>
    /// Gets the type resources that contains this resource.
    /// </summary>
    public IResourceContainer Container => _container;

    /// <summary>
    /// Gets the resource name in the <see cref="Container"/>.
    /// <para>
    /// This is the full resource name that includes the <see cref="IResourceContainer.ResourcePrefix"/>.
    /// </para>
    /// </summary>
    public string ResourceName => _resourceName;

    /// <summary>
    /// Gets the resource name without the <see cref="IResourceContainer.ResourcePrefix"/>.
    /// </summary>
    public ReadOnlyMemory<char> LocalResourceName
    {
        get
        {
            Throw.CheckState( IsValid );
            return _resourceName.AsMemory( _container.ResourcePrefix.Length.. );
        }
    }

    /// <summary>
    /// Gets the name of this resource.
    /// <para>
    /// This is a simple relay to <see cref="IResourceContainer.GetResourceName(ResourceLocator)"/>.
    /// </para>
    /// </summary>
    public ReadOnlySpan<char> Name
    {
        get
        {
            Throw.CheckState( IsValid );
            return _container.GetResourceName( this );
        }
    }

    /// <summary>
    /// To be equal the <see cref="Container"/> must be the same and <see cref="ResourceName"/>
    /// must be equal for the <see cref="IResourceContainer.NameComparer"/>.
    /// </summary>
    /// <param name="other">The other locator.</param>
    /// <returns>True if the resources are the same.</returns>
    public bool Equals( ResourceLocator other )
    {
        return _container == other._container
               && (!IsValid || _container.NameComparer.Equals( _resourceName, other._resourceName ));
    }

    /// <summary>
    /// Gets the resource content.
    /// See <see cref="IResourceContainer.GetStream(in ResourceLocator)"/>.
    /// </summary>
    /// <returns>The resource's content stream.</returns>
    public Stream GetStream()
    {
        Throw.CheckState( IsValid );
        return _container.GetStream( this );
    }

    /// <summary>
    /// Writes the content of this resource to a stream.
    /// See <see cref="IResourceContainer.WriteStream(ResourceLocator, Stream)"/>.
    /// </summary>
    /// <param name="target">The target stream.</param>
    public void WriteStream( Stream target )
    {
        Throw.CheckState( IsValid );
        _container.WriteStream( this, target );
    }

    /// <summary>
    /// Gets "'LocalResourceName' in 'Container'" when <see cref="IsValid"/> is true
    /// or the empty string when IsValid is false.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return IsValid
                ? $"'{LocalResourceName}' in '{_container.DisplayName}'"
                : "";
    }

    public override bool Equals( object? obj ) => obj is ResourceLocator locator && Equals( locator );

    public static bool operator ==( ResourceLocator left, ResourceLocator right ) => left.Equals( right );

    public static bool operator !=( ResourceLocator left, ResourceLocator right ) => !(left == right);

    public override int GetHashCode() => IsValid
                                            ? HashCode.Combine( Container.GetHashCode(), _container.NameComparer.GetHashCode( ResourceName ) )
                                            : 0;

    internal void CheckContainer( IResourceContainer expectedContainer )
    {
        if( _container != expectedContainer )
        {
            if( !IsValid )
            {
                throw new ArgumentException( $"The resource is invalid." );
            }
            throw new ArgumentException( $"The resource {ToString()} doesn't belong to this '{expectedContainer.DisplayName}'." );
        }
    }

}
