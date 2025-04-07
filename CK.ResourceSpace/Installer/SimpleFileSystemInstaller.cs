using CK.EmbeddedResources;
using System;
using System.IO;

namespace CK.Core;

/// <summary>
/// Simple implementation of <see cref="IFileSystemInstaller"/> that can be specialized.
/// </summary>
public class SimpleFileSystemInstaller : IFileSystemInstaller
{
    readonly string _targetPath;
    readonly char[] _pathBuffer;

    /// <summary>
    /// Initialize a new installer on a target path.
    /// </summary>
    /// <param name="targetPath">The target folder.</param>
    public SimpleFileSystemInstaller( string targetPath )
    {
        Throw.CheckArgument( Path.IsPathFullyQualified( targetPath ) && targetPath.EndsWith( Path.DirectorySeparatorChar ) );
        _targetPath = targetPath;
        _pathBuffer = new char[1024];
        targetPath.CopyTo( _pathBuffer );
    }

    /// <summary>
    /// See <see cref="IResourceSpaceItemInstaller.Open(IActivityMonitor, ResourceSpace)"/>.
    /// Ensures that the <see cref="TargetPath"/> exists by creating it if needed.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resSpace">The resource space to install.</param>
    /// <returns>True on sucess, false on error.</returns>
    public virtual bool Open( IActivityMonitor monitor, ResourceSpace resSpace )
    {
        if( !Path.Exists( TargetPath ) )
        {
            monitor.Info( $"Creating code generated target path: {TargetPath}" );
            try
            {
                Directory.CreateDirectory( TargetPath );
            }
            catch( Exception ex )
            {
                monitor.Error( $"While creating code generated target path: {TargetPath}", ex );
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// See <see cref="IResourceSpaceItemInstaller.Close(IActivityMonitor, bool)"/>.
    /// Does nothing at this level.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="success">Whether the install succeeded or not.</param>
    public virtual void Close( IActivityMonitor monitor, bool success )
    {
    }

    /// <summary>
    /// Gets the target path. Ends with <see cref="Path.DirectorySeparatorChar"/>.
    /// </summary>
    public string TargetPath => _targetPath;

    /// <summary>
    /// Called before each write with the full path of the file that will be written.
    /// <para>
    /// Does nothing at this level.
    /// </para>
    /// </summary>
    /// <param name="path">The full file path.</param>
    protected virtual void OnWrite( string path )
    {
    }

    /// <inheritdoc />
    public void Write( ResourceLocator resource )
    {
        string path = GetNormalizedTargetPath( resource.ResourceName );
        DoWrite( path, resource );
    }

    /// <inheritdoc />
    public void Write( NormalizedPath filePath, ResourceLocator resource )
    {
        string path = GetNormalizedTargetPath( filePath.Path );
        DoWrite( path, resource );
    }

    /// <inheritdoc />
    public void Write( NormalizedPath filePath, string text )
    {
        string path = GetNormalizedTargetPath( filePath.Path );
        OnWrite( path );
        File.WriteAllText( path, text );
    }

    /// <inheritdoc />
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

    public override string ToString() => $"Installer: {_targetPath}";
}
