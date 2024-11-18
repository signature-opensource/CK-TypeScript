using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace CK.Core;

/// <summary>
/// An empty file container with no contents.
/// </summary>
public sealed class EmptyResourceContainer : IResourceContainer
{
    static readonly NullFileProvider _nullFileProvider = new NullFileProvider();

    readonly string _displayName;

    /// <summary>
    /// A default empty containaer named "Empty resource container".
    /// </summary>
    public static readonly EmptyResourceContainer Default = new EmptyResourceContainer( "Empty resource container" );

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
    public IEnumerable<ResourceLocator> AllResources => ImmutableArray<ResourceLocator>.Empty;

    /// <inheritdoc />
    public StringComparer ResourceNameComparer => StringComparer.Ordinal;

    /// <inheritdoc />
    public IFileProvider GetFileProvider() => _nullFileProvider;

    /// <inheritdoc />
    public ResourceLocator GetResourceLocator( IFileInfo fileInfo ) => default;

    /// <inheritdoc />
    public Stream GetStream( ResourceLocator resource ) => Throw.InvalidOperationException<Stream>();

    /// <inheritdoc />
    public bool HasDirectory( ReadOnlySpan<char> localResourceName ) => false;

    /// <inheritdoc />
    public bool TryGetResource( ReadOnlySpan<char> localResourceName, out ResourceLocator locator )
    {
        locator = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => _displayName;
}
