using CK.BinarySerialization;
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core;

[SerializationVersion(0)]
sealed class StoreContainer : IResourceContainer, ICKSlicedSerializable
{
    readonly IResourceContainer _container;
    readonly HashSet<ResourceLocator> _codeHandledResources;
    readonly string? _localPath;

    public StoreContainer( ResPackageDescriptorContext context, IResourceContainer container )
    {
        Throw.DebugAssert( context.CodeHandledResources is HashSet<ResourceLocator> );
        _codeHandledResources = Unsafe.As<HashSet<ResourceLocator>>( context.CodeHandledResources );
        _container = container;
        _localPath = container is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport
                        ? fs.ResourcePrefix
                        : null;
    }

    public StoreContainer( IBinaryDeserializer d, ITypeReadInfo info )
    {
        _codeHandledResources = d.ReadObject<HashSet<ResourceLocator>>();
        _container = d.ReadObject<IResourceContainer>();
        _localPath = _container is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport
                        ? fs.ResourcePrefix
                        : null;
    }

    public static void Write( IBinarySerializer s, in StoreContainer o )
    {
        s.WriteObject( o._codeHandledResources );
        s.WriteObject( o._container );
    }

    /// <summary>
    /// Gets the real container: resources can be looked up skipping the CodeHandledResources
    /// set of removed resources.
    /// The StoreContainer is not a ResourceContainerWrapper: it doesn't subsitute its identity
    /// to the inner one, it just acts as a filter on resources that have been removed from the
    /// 
    /// </summary>
    public IResourceContainer InnerContainer => _container;

    /// <summary>
    /// Gets the local resources path if this StoreContainer is a FileSystemResourceContainer with a
    /// true HasLocalFilePathSupport.
    /// </summary>
    public string? LocalPath => _localPath;

    public bool IsValid => _container.IsValid;

    public string DisplayName => _container.DisplayName;

    public string ResourcePrefix => _container.ResourcePrefix;

    public char DirectorySeparatorChar => _container.DirectorySeparatorChar;

    public IEnumerable<ResourceLocator> AllResources => _container.AllResources.Where( r => !_codeHandledResources.Contains( r ) );

    public bool HasLocalFilePathSupport => _container.HasLocalFilePathSupport;

    public IEnumerable<ResourceLocator> GetAllResources( ResourceFolder folder )
    {
        return _container.GetAllResources( folder ).Where( r => !_codeHandledResources.Contains( r ) );
    }

    public ResourceFolder GetFolder( ReadOnlySpan<char> folderName )
    {
        return _container.GetFolder( folderName );
    }

    public ResourceFolder GetFolder( ResourceFolder folder, ReadOnlySpan<char> folderName )
    {
        return _container.GetFolder( folder, folderName );
    }

    public IEnumerable<ResourceFolder> GetFolders( ResourceFolder folder )
    {
        return _container.GetFolders( folder );
    }

    public string? GetLocalFilePath( in ResourceLocator resource )
    {
        return _codeHandledResources.Contains( resource )
                    ? null
                    : _container.GetLocalFilePath( resource );
    }

    public ResourceLocator GetResource( ReadOnlySpan<char> resourceName )
    {
        var r = _container.GetResource( resourceName );
        return r.IsValid && !_codeHandledResources.Contains( r )
                ? r
                : default;
    }

    public ResourceLocator GetResource( ResourceFolder folder, ReadOnlySpan<char> resourceName )
    {
        var r = _container.GetResource( folder, resourceName );
        return r.IsValid && !_codeHandledResources.Contains( r )
                ? r
                : default;
    }

    bool IsNotCodeHandled( ResourceLocator resource ) => !_codeHandledResources.Contains( resource );

    public IEnumerable<ResourceLocator> GetResources( ResourceFolder folder )
    {
        return _container.GetResources( folder ).Where( r => !_codeHandledResources.Contains( r ) );
    }

    public Stream GetStream( in ResourceLocator resource )
    {
        Throw.CheckArgument( IsNotCodeHandled( resource ) );
        return _container.GetStream( resource );
    }

    public string ReadAsText( in ResourceLocator resource )
    {
        Throw.CheckArgument( IsNotCodeHandled( resource ) );
        return _container.ReadAsText( resource );
    }

    public void WriteStream( in ResourceLocator resource, Stream target )
    {
        Throw.CheckArgument( IsNotCodeHandled( resource ) );
        _container.WriteStream( resource, target );
    }

    public override string ToString() => _container.ToString();
}
