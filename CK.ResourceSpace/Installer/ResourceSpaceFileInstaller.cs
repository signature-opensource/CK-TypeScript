using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core;

sealed class ResourceSpaceFileInstaller : IResourceSpaceFileInstaller
{
    readonly string _targetPath;
    readonly char[] _pathBuffer;
    readonly HashSet<string> _existing;

    ResourceSpaceFileInstaller( string targetPath )
    {
        Throw.CheckArgument( Path.IsPathFullyQualified( targetPath ) && targetPath.EndsWith( Path.DirectorySeparatorChar ) );
        _targetPath = targetPath;
        _pathBuffer = new char[1024];
        targetPath.CopyTo( _pathBuffer );
        _existing = new HashSet<string>( Directory.EnumerateFiles( _targetPath, "*", SearchOption.AllDirectories ) );
        _existing.Remove( ".gitignore" );
    }

    public static ResourceSpaceFileInstaller? Create( IActivityMonitor monitor, string ckGenPath )
    {
        Throw.CheckArgument( Path.IsPathFullyQualified( ckGenPath ) && ckGenPath.EndsWith( Path.DirectorySeparatorChar ) );
        if( !Path.Exists( ckGenPath ) )
        {
            monitor.Info( $"Creating code generated target path: {ckGenPath}" );
            try
            {
                Directory.CreateDirectory( ckGenPath );
            }
            catch( Exception ex )
            {
                monitor.Error( $"While creating code generated target path: {ckGenPath}", ex );
                return null;
            }
        }
        try
        {
            var r = new ResourceSpaceFileInstaller( ckGenPath );
            monitor.Info( $"Code generated target path contains {r._existing.Count} files that will be updated." );
            return r;
        }
        catch( Exception ex )
        {
            monitor.Error( $"While collecting existing files in code generated target path: {ckGenPath}", ex );
            return null;
        }

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
        _existing.Remove( path );
        File.WriteAllText( path, text );
    }

    public Stream OpenWriteStream( NormalizedPath filePath )
    {
        string path = GetNormalizedTargetPath( filePath.Path );
        _existing.Remove( path );
        return new FileStream( path, FileMode.Create );
    }

    void DoWrite( string filePath, ResourceLocator resource )
    {
        _existing.Remove( filePath );
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

    internal void Cleanup( IActivityMonitor monitor, bool success )
    {
        if( _existing.Count == 0 )
        {
            monitor.Info( "No previous file exist that have not been regenerated. Nothing to delete." );
        }
        else
        {
            if( success )
            {
                using( monitor.OpenInfo( $"Deleting {_existing.Count} previous files." ) )
                {
                    foreach( var p in _existing )
                    {
                        monitor.Debug( $"Deleting '{p.AsSpan( _targetPath.Length )}'." );
                        try
                        {
                            if( File.Exists( p ) ) File.Delete( p );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( $"While deleting '{p}'. Ignoring.", ex );
                        }
                    }
                }
            }
            else
            {
                monitor.Info( $"""
                                Skipping deletion of {_existing.Count} previous files:
                                {_existing.Order().Concatenate( " > " + Environment.NewLine )}They will be deleted on the the next successful run.
                                """ );
            }
        }
    }
}
