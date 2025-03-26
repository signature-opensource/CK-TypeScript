using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core;


public sealed class ResourceSpaceFileInstaller
{
    readonly string _targetPath;
    readonly char[] _pathBuffer;
    readonly HashSet<string> _existing;

    public ResourceSpaceFileInstaller( string targetPath )
    {
        Throw.CheckArgument( Path.IsPathFullyQualified( targetPath ) && targetPath.EndsWith( Path.DirectorySeparatorChar ) ); 
        _targetPath = targetPath;
        _pathBuffer = new char[1024];
        targetPath.CopyTo( _pathBuffer );
        _existing = new HashSet<string>( Directory.EnumerateFiles( _targetPath, "*", SearchOption.AllDirectories ) );
        _existing.Remove( ".gitignore" );
    }

    public void Write( ResourceLocator resource )
    {
        string path = GetNormalizedTargetPath( resource.ResourceName );
        DoWrite( path, resource );
    }

    public void Write( NormalizedPath target, ResourceLocator resource )
    {
        string path = GetNormalizedTargetPath( target.Path );
        DoWrite( path, resource );
    }

    public void Write( NormalizedPath filePath, string text )
    {
        string path = GetNormalizedTargetPath( filePath.Path );
        _existing.Remove( path );
        File.WriteAllText( path, text );
    }

    public Stream OpenWriteStream( NormalizedPath fPath )
    {
        string path = GetNormalizedTargetPath( fPath.Path );
        _existing.Remove( path );
        return new FileStream( path, FileMode.Create );
    }



    void DoWrite( string path, ResourceLocator resource )
    {
        _existing.Remove( path );
        if( resource.LocalFilePath != null )
        {
            File.Copy( resource.LocalFilePath, path, overwrite: true );
        }
        else
        {
            using( var f = new FileStream( path, FileMode.Create ) )
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
