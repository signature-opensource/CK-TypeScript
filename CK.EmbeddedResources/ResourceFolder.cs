using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.EmbeddedResources;

/// <summary>
/// Resource folder in a <see cref="IResourceContainer"/>.
/// <para>
/// The <c>default</c> value is <see cref="IsValid"/> false: this makes <see cref="Nullable{T}"/> useless for this type.
/// </para>
/// </summary>
public readonly struct ResourceFolder : IEquatable<ResourceFolder>
{
    /// <summary>
    /// Initializes a new resource folder.
    /// </summary>
    /// <param name="container">The resources that contains this folder.</param>
    /// <param name="fullFolderName">the resource name in the <paramref name="container"/>. Must not be null, empty or whitespace.</param>
    public ResourceFolder( IResourceContainer container, string fullFolderName )
    {
        Throw.CheckNotNullArgument( container );
        Throw.CheckNotNullOrWhiteSpaceArgument( fullFolderName );
        Container = container;
        FullFolderName = fullFolderName;
    }

    /// <summary>
    /// Gets whether this folder is valid: the <see cref="Container"/> is not null
    /// and the <see cref="FullFolderName"/> is not null, not empty nor whitespace.
    /// <para>
    /// Whether the resource actuallly exists or not is not known.
    /// </para>
    /// Only the <c>default</c> of this type is invalid.
    /// </summary>
    public bool IsValid => Container != null;

    /// <summary>
    /// Gets the container of this resource.
    /// </summary>
    public IResourceContainer Container { get; }

    /// <summary>
    /// Gets the folder name in the <see cref="Container"/>.
    /// <para>
    /// This is the full name that includes the <see cref="IResourceContainer.ResourcePrefix"/>.
    /// </para>
    /// </summary>
    public string FullFolderName { get; }

    /// <summary>
    /// Gets the folder name without the <see cref="IResourceContainer.ResourcePrefix"/>.
    /// </summary>
    public ReadOnlyMemory<char> FolderName
    {
        get
        {
            Throw.CheckState( IsValid );
            return FullFolderName.AsMemory( Container.ResourcePrefix.Length.. );
        }
    }

    /// <summary>
    /// Gets the name of this folder.
    /// <para>
    /// This is a simple relay to <see cref="IResourceContainer.GetFolderName(ResourceFolder)"/>.
    /// </para>
    /// </summary>
    public ReadOnlySpan<char> Name
    {
        get
        {
            Throw.CheckState( IsValid );
            return Container.GetFolderName( this );
        }
    }

    /// <summary>
    /// Gets an existing resource or a locator with <see cref="ResourceLocator.IsValid"/> false
    /// if the resource doesn't exist.
    /// </summary>
    /// <param name="localResourceName">The local resource name (can contain any folder prefix).</param>
    /// <returns>The resource locator that may not be valid.</returns>
    public ResourceLocator GetResource( ReadOnlySpan<char> localResourceName )
    {
        Throw.CheckState( IsValid );
        return Container.GetResource( this, localResourceName );
    }

    /// <summary>
    /// Gets an existing folder or a ResourceFolder with <see cref="ResourceLocator.IsValid"/> false
    /// if the folder doesn't exist.
    /// </summary>
    /// <param name="localFolderName">The local resource folder name (can contain any folder prefix).</param>
    /// <returns>The resource folder that may not be valid.</returns>
    public ResourceFolder GetFolder( ReadOnlySpan<char> localFolderName )
    {
        Throw.CheckState( IsValid );
        return Container.GetFolder( this, localFolderName );
    }

    /// <summary>
    /// Gets the resources contained in this folder.
    /// <para>
    /// This is a simple relay to <see cref="IResourceContainer.GetResources(ResourceFolder)"/>.
    /// </para>
    /// </summary>
    public IEnumerable<ResourceLocator> Resources
    {
        get
        {
            Throw.CheckState( IsValid );
            return Container.GetResources( this );
        }
    }

    /// <summary>
    /// Gets all the resources contained in this folder, regardless of any subordinated folders.
    /// <para>
    /// This is a simple relay to <see cref="IResourceContainer.GetAllResources(ResourceFolder)"/>.
    /// </para>
    /// </summary>
    public IEnumerable<ResourceLocator> AllResources
    {
        get
        {
            Throw.CheckState( IsValid );
            return Container.GetAllResources( this );
        }
    }

    /// <summary>
    /// Gets the direct children folders contained in this folder.
    /// <para>
    /// This is a simple relay to <see cref="IResourceContainer.GetFolders(ResourceFolder)"/>.
    /// </para>
    /// </summary>
    public IEnumerable<ResourceFolder> Folders
    {
        get
        {
            Throw.CheckState( IsValid );
            return Container.GetFolders( this );
        }
    }

    /// <summary>
    /// To be equal the <see cref="Container"/> must be the same and <see cref="FullFolderName"/>
    /// must be equal for the <see cref="IResourceContainer.NameComparer"/>.
    /// </summary>
    /// <param name="other">The other locator.</param>
    /// <returns>True if the resources are the same.</returns>
    public bool Equals( ResourceFolder other )
    {
        return Container == other.Container
               && (!IsValid || Container.NameComparer.Equals( FullFolderName, other.FullFolderName ));
    }

    /// <summary>
    /// Gets "Folder "Folder 'LocalFolderName' in 'Container'" when <see cref="IsValid"/> is true
    /// or the empty string when IsValid is false.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return IsValid
                ? $"Folder '{FolderName}' in '{Container.DisplayName}'"
                : "";
    }

    public override bool Equals( object? obj ) => obj is ResourceFolder locator && Equals( locator );

    public static bool operator ==( ResourceFolder left, ResourceFolder right ) => left.Equals( right );

    public static bool operator !=( ResourceFolder left, ResourceFolder right ) => !(left == right);

    public override int GetHashCode() => IsValid
                                            ? HashCode.Combine( Container.GetHashCode(), Container.NameComparer.GetHashCode( FullFolderName ) )
                                            : 0;


    internal void CheckContainer( IResourceContainer expectedContainer )
    {
        if( Container != expectedContainer )
        {
            if( !IsValid )
            {
                throw new ArgumentException( $"The folder is invalid." );
            }
            throw new ArgumentException( $"'{ToString()}' doesn't belong to this '{expectedContainer.DisplayName}'." );
        }
    }


}
