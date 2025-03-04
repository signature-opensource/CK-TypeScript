using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace CK.EmbeddedResources;

/// <summary>
/// An empty resource container with no contents.
/// </summary>
[SerializationVersion(0)]
public sealed class EmptyResourceContainer : IResourceContainer, ICKVersionedBinarySerializable
{
    readonly string _displayName;
    readonly bool _isDisabled;
    readonly string _resourcePrefix;
    readonly bool _isValid;

    /// <summary>
    /// A default empty container named "Final Container".
    /// Can be used as an empty object where a container is required but without direct content in it.
    /// </summary>
    public static readonly EmptyResourceContainer FakeFinalContainer = new EmptyResourceContainer( "Final Container", false );

    /// <summary>
    /// Initializes a new empty container with a display name.
    /// </summary>
    /// <param name="displayName">The name of this empty container.</param>
    /// <param name="isDisabled">
    /// True for a by design disabled container. When true, the <paramref name="displayName"/> is automatically
    /// prefixed by "disabled ".
    /// </param>
    /// <param name="resourcePrefix">Optional resource prefix. Defaults to the empty string.</param>
    /// <param name="isValid">Optionally creates an invalid container. By default, an empty container is valid.</param>
    public EmptyResourceContainer( string displayName, bool isDisabled, string? resourcePrefix = null, bool isValid = true )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( displayName );
        _displayName = isDisabled
                        ? "disabled " + displayName
                        : displayName;
        _isDisabled = isDisabled;
        _resourcePrefix = resourcePrefix ?? string.Empty;
        _isValid = isValid;
    }

    /// <summary>
    /// Reads an empty resource container previously serialized by <see cref="WriteData(ICKBinaryWriter)"/>.
    /// </summary>
    /// <param name="r">The reader.</param>
    /// <param name="version">The serialized version.</param>
    /// <returns>The container.</returns>
    public static EmptyResourceContainer Read( ICKBinaryReader r, int version )
    {
        Throw.CheckArgument( version == 0 );
        if( r.ReadByte() == 0 )
        {
            return FakeFinalContainer;
        }
        return new EmptyResourceContainer( r.ReadString(), r.ReadBoolean(), r.ReadString(), r.ReadBoolean() );
    }

    /// <summary>
    /// Writes this empty container. The singleton <see cref="FakeFinalContainer"/> is handled,
    /// <see cref="Read(ICKBinaryReader, int)"/> will recover it.
    /// </summary>
    /// <param name="w">The target writer.</param>
    public void WriteData( ICKBinaryWriter w )
    {
        if( this == FakeFinalContainer )
        {
            w.Write( (byte)0 );
        }
        else
        {
            w.Write( (byte)1 );
            w.Write( _displayName );
            w.Write( _isDisabled );
            w.Write( _resourcePrefix );
            w.Write( _isValid );
        }
    }

    /// <inheritdoc />
    public bool IsValid => _isValid;

    /// <summary>
    /// Gets whether this container is disabled by designed.
    /// </summary>
    public bool IsDisabled => _isDisabled;

    /// <inheritdoc />
    public string DisplayName => _displayName;

    /// <summary>
    /// Gets the display name regardless of the <see cref="IsDisabled"/> state.
    /// </summary>
    public ReadOnlySpan<char> NonDisabledDisplayName => _isDisabled ? _displayName.AsSpan( 9 ) : _displayName;

    /// <inheritdoc />
    public string ResourcePrefix => _resourcePrefix;

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> AllResources => [];

    /// <inheritdoc />
    public StringComparer NameComparer => StringComparer.Ordinal;

    /// <summary>
    /// Always false.
    /// </summary>
    public bool HasLocalFilePathSupport => false;

    /// <summary>
    /// Always returns null.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <returns>A null local file path.</returns>
    public string? GetLocalFilePath( in ResourceLocator resource ) => null;

    /// <inheritdoc />
    public Stream GetStream( in ResourceLocator resource ) => Throw.InvalidOperationException<Stream>();

    /// <inheritdoc />
    public void WriteStream( in ResourceLocator resource, Stream target ) => Throw.InvalidOperationException();

    /// <inheritdoc />
    public string ReadAsText( in ResourceLocator resource ) => Throw.InvalidOperationException<string>();

    /// <inheritdoc />
    public ResourceLocator GetResource( ReadOnlySpan<char> resourceName ) => default;

    /// <inheritdoc />
    public ResourceLocator GetResource( ResourceFolder folder, ReadOnlySpan<char> resourceName ) => default;

    /// <inheritdoc />
    public ResourceFolder GetFolder( ReadOnlySpan<char> folderName ) => default;

    /// <inheritdoc />
    public ResourceFolder GetFolder( ResourceFolder folder, ReadOnlySpan<char> folderName ) => default;

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetAllResources( ResourceFolder folder ) => [];

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetResources( ResourceFolder folder ) => [];

    /// <inheritdoc />
    public IEnumerable<ResourceFolder> GetFolders( ResourceFolder folder ) => [];

    /// <inheritdoc />
    public ReadOnlySpan<char> GetFolderName( ResourceFolder folder ) => default;

    /// <inheritdoc />
    public ReadOnlySpan<char> GetResourceName( ResourceLocator resource ) => default;

    /// <inheritdoc />
    public override string ToString() => _displayName;

}
