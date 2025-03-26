using CK.EmbeddedResources;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using CK.Core;
using System.Text;
using System.Buffers;

namespace CK.TypeScript.CodeGen;

public sealed partial class TypeScriptRoot : IResourceContainer
{
    const string _displayName = "Generated TypeScript";

    bool IResourceContainer.IsValid => true;

    string IResourceContainer.DisplayName => _displayName;

    string IResourceContainer.ResourcePrefix => string.Empty;

    StringComparer IResourceContainer.NameComparer => StringComparer.Ordinal;

    bool IResourceContainer.HasLocalFilePathSupport => false;

    IEnumerable<ResourceLocator> IResourceContainer.AllResources => throw new NotImplementedException();

    string? IResourceContainer.GetLocalFilePath( in ResourceLocator resource ) => null;

    ResourceFolder IResourceContainer.GetFolder( ReadOnlySpan<char> folderName )
    {
        var f = _root.FindFolder( folderName );
        return f != null ? new ResourceFolder( this, f.Path.Path ) : default;
    }

    ResourceFolder IResourceContainer.GetFolder( ResourceFolder folder, ReadOnlySpan<char> folderName )
    {
        folder.CheckContainer( this );
        var f = _root.FindFolder( folder.FullFolderName.AsSpan() )?.FindFolder( folderName );
        return f != null ? new ResourceFolder( this, f.Path.Path ) : default;
    }

    ReadOnlySpan<char> IResourceContainer.GetFolderName( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        return Path.GetFileName( folder.FullFolderName.AsSpan() );
    }

    IEnumerable<ResourceFolder> IResourceContainer.GetFolders( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        var f = _root.FindFolder( folder.FullFolderName.AsSpan() );
        if( f != null )
        {
            return f.Folders.Select( c => new ResourceFolder( this, c.Path.Path ) );
        }
        return Array.Empty<ResourceFolder>();
    }

    IEnumerable<ResourceLocator> IResourceContainer.GetAllResources( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        var f = _root.FindFolder( folder.FullFolderName.AsSpan() );
        if( f != null )
        {
            return f.AllFilesRecursive.Select( f => new ResourceLocator( this, f.Folder.Path.Path + '/' + f.Name ) );
        }
        return Array.Empty<ResourceLocator>();
    }

    ResourceLocator IResourceContainer.GetResource( ReadOnlySpan<char> resourceName )
    {
        var f = _root.FindFile( resourceName );
        return f != null ? new ResourceLocator( this, f.Folder.Path.Path + '/' + f.Name ) : default;
    }

    ResourceLocator IResourceContainer.GetResource( ResourceFolder folder, ReadOnlySpan<char> resourceName )
    {
        folder.CheckContainer( this );
        var f = _root.FindFolder( folder.FullFolderName.AsSpan() )?.FindFile( resourceName );
        return f != null ? new ResourceLocator( this, f.Folder.Path.Path + '/' + f.Name ) : default;
    }

    ReadOnlySpan<char> IResourceContainer.GetResourceName( ResourceLocator resource )
    {
        return Path.GetFileName( resource.FullResourceName.AsSpan() );
    }

    IEnumerable<ResourceLocator> IResourceContainer.GetResources( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        var f = _root.FindFolder( folder.FullFolderName.AsSpan() );
        if( f != null )
        {
            return f.AllFiles.Select( f => new ResourceLocator( this, f.Folder.Path + '/' + f.Name ) );
        }
        return Array.Empty<ResourceLocator>();
    }

    Stream IResourceContainer.GetStream( in ResourceLocator resource )
    {
        resource.CheckContainer( this );
        var f = _root.FindFile( resource.FullResourceName.AsSpan() );
        Throw.CheckState( "Code resources cannot be removed.", f != null );
        // Temporary.
        if( f is not TypeScriptFile ts )
        {
            return Throw.InvalidOperationException<Stream>( "Code resources can only be TypeScriptFile." );
        }
        return Util.RecyclableStreamManager.GetStream( Encoding.UTF8.GetBytes( ts.GetCurrentText() ) );
    }

    string IResourceContainer.ReadAsText( in ResourceLocator resource ) => DoGetText( resource );

    string DoGetText( ResourceLocator resource )
    {
        resource.CheckContainer( this );
        var f = _root.FindFile( resource.FullResourceName.AsSpan() );
        Throw.CheckState( "Code resources cannot be removed.", f != null );
        // Temporary.
        if( f is not TypeScriptFile ts )
        {
            return Throw.InvalidOperationException<string>( "Code resources can only be TypeScriptFile." );
        }
        return ts.GetCurrentText();
    }

    void IResourceContainer.WriteStream( in ResourceLocator resource, Stream target )
    {
        var str = DoGetText( resource );
        var len = Encoding.UTF8.GetMaxByteCount( str.Length );
        var buffer = ArrayPool<byte>.Shared.Rent( len );
        len = Encoding.UTF8.GetBytes( str, buffer.AsSpan() );
        target.Write( buffer.AsSpan( 0, len ) );
        ArrayPool<byte>.Shared.Return( buffer );
    }

    public override string ToString() => _displayName;
}

