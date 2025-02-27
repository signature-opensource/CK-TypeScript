using System;
using System.Collections.Generic;
using System.IO;

namespace CK.Core;

/// <summary>
/// An empty resource container with no contents.
/// </summary>
public sealed class EmptyResourceContainer : IResourceContainer
{
    readonly string _displayName;
    readonly string _resourcePrefix;
    readonly bool _isValid;

    /// <summary>
    /// A default empty container named "Generated Code Container".
    /// </summary>
    public static readonly EmptyResourceContainer GeneratedCode = new EmptyResourceContainer( "Generated Code Container" );

    /// <summary>
    /// Initializes a new empty container with a display name.
    /// </summary>
    /// <param name="displayName">The name of this empty container.</param>
    /// <param name="resourcePrefix">Optional resource prefix. Defaults to the empty string.</param>
    /// <param name="isValid">Optionally creates an invalid container. By default, an empty container is valid.</param>
    public EmptyResourceContainer( string displayName, string? resourcePrefix = null, bool isValid = true )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( displayName );
        _displayName = displayName;
        _resourcePrefix = resourcePrefix ?? string.Empty;
        _isValid = isValid;
    }

    /// <inheritdoc />
    public bool IsValid => _isValid;

    /// <inheritdoc />
    public string DisplayName => _displayName;

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
    public ResourceLocator GetResource( ReadOnlySpan<char> localResourceName ) => default;

    /// <inheritdoc />
    public ResourceLocator GetResource( ResourceFolder folder, ReadOnlySpan<char> localResourceName ) => default;

    /// <inheritdoc />
    public ResourceFolder GetFolder( ReadOnlySpan<char> localFolderName ) => default;

    /// <inheritdoc />
    public ResourceFolder GetFolder( ResourceFolder folder, ReadOnlySpan<char> localFolderName ) => default;

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
