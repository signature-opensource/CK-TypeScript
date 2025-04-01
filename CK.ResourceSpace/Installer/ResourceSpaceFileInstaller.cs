using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Simple implementation of <see cref="IResourceSpaceFileInstaller"/> that can be specialized.
/// </summary>
public class ResourceSpaceFileInstaller : IResourceSpaceFileInstaller
{
    readonly string _targetPath;
    readonly char[] _pathBuffer;

    /// <summary>
    /// Initialize a new installer on a target path.
    /// </summary>
    /// <param name="targetPath">The target folder. The directory must exist.</param>
    public ResourceSpaceFileInstaller( string targetPath )
    {
        Throw.CheckArgument( Path.IsPathFullyQualified( targetPath ) && targetPath.EndsWith( Path.DirectorySeparatorChar ) );
        Throw.CheckState( Directory.Exists( targetPath ) );
        _targetPath = targetPath;
        _pathBuffer = new char[1024];
        targetPath.CopyTo( _pathBuffer );
    }

    /// <summary>
    /// Gets the target path. Ends with <see cref="Path.DirectorySeparatorChar"/>.
    /// </summary>
    protected string TargetPath => _targetPath;

    /// <summary>
    /// Called before each write with the full path of the file that will be written.
    /// </summary>
    /// <param name="path">The full file path.</param>
    protected virtual void OnWrite( string path )
    {
    }

    public void Write( ResourceLocator resource )
    {
        string path = GetNormalizedTargetPath( resource.ResourceName );
        DoWrite( path, resource );
    }

    public void Write( NormalizedPath filePath, ResourceLocator resource )
    {
        string path = GetNormalizedTargetPath( filePath.Path );
        DoWrite( path, resource );
    }

    public void Write( NormalizedPath filePath, string text )
    {
        string path = GetNormalizedTargetPath( filePath.Path );
        OnWrite( path );
        File.WriteAllText( path, text );
    }

    public Stream OpenWriteStream( NormalizedPath filePath )
    {
        string path = GetNormalizedTargetPath( filePath.Path );
        OnWrite( path );
        return new FileStream( path, FileMode.Create );
    }

    void DoWrite( string filePath, ResourceLocator resource )
    {
        OnWrite( filePath );
        if( resource.LocalFilePath != null )
        {
            File.Copy( resource.LocalFilePath, filePath, overwrite: true );
        }
        else
        {
            using( var f = new FileStream( filePath, FileMode.Create ) )
            {
                resource.WriteStream( f );
            }
        }
    }

    string GetNormalizedTargetPath( ReadOnlySpan<char> resName )
    {
        var dest = _pathBuffer.AsSpan( _targetPath.Length, resName.Length );
        resName.CopyTo( dest );
        if( Path.DirectorySeparatorChar != NormalizedPath.DirectorySeparatorChar )
        {
            dest.Replace( NormalizedPath.DirectorySeparatorChar, Path.DirectorySeparatorChar );
        }
        var path = _pathBuffer.AsSpan( 0, _targetPath.Length + resName.Length ).ToString();
        return path;
    }
}
