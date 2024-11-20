using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace CK.Core;

/// <summary>
/// An empty file container with no contents.
/// </summary>
public sealed class EmptyResourceContainer : IResourceContainer
{
    readonly string _displayName;

    /// <summary>
    /// A default empty container named "Generated Code Container".
    /// </summary>
    public static readonly EmptyResourceContainer GeneratedCode = new EmptyResourceContainer( "Generated Code Container" );

    /// <summary>
    /// Initiaizes a new empty container with a display name.
    /// </summary>
    /// <param name="displayName">The name of this empty container.</param>
    public EmptyResourceContainer( string displayName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument(displayName);
        _displayName = displayName;
    }

    /// <summary>
    /// Always true: an empty container is valid.
    /// </summary>
    public bool IsValid => true;

    /// <inheritdoc />
    public string DisplayName => _displayName;

    /// <summary>
    /// Always the empty string.
    /// </summary>
    public string ResourcePrefix => "";

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> AllResources => [];

    /// <inheritdoc />
    public StringComparer NameComparer => StringComparer.Ordinal;

    /// <inheritdoc />
    public Stream GetStream( in ResourceLocator resource ) => Throw.InvalidOperationException<Stream>();

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
