using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Specialized <see cref="ResourceSpaceFileInstaller"/> that tracks written files
/// and can cleanup any previously existing files.
/// <para>
/// Deferred files cleanup minimizes impacts on file watchers: we don't destroy/recreate the
/// target folder. Instead we update the existing files in place and then remove any files that
/// have not been generated.
/// </para>
/// </summary>
sealed class InitialFileInstaller : ResourceSpaceFileInstaller
{
    readonly HashSet<string> _existing;

    InitialFileInstaller( string targetPath )
        : base( targetPath )
    {
        _existing = new HashSet<string>( Directory.EnumerateFiles( TargetPath, "*", SearchOption.AllDirectories ) );
        _existing.Remove( ".gitignore" );
    }

    public static InitialFileInstaller? Create( IActivityMonitor monitor, string ckGenPath )
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
            var r = new InitialFileInstaller( ckGenPath );
            monitor.Info( $"Code generated target path contains {r._existing.Count} files that will be updated." );
            return r;
        }
        catch( Exception ex )
        {
            monitor.Error( $"While collecting existing files in code generated target path: {ckGenPath}", ex );
            return null;
        }
    }

    protected override void OnWrite( string path ) => _existing.Remove( path );

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
                        monitor.Debug( $"Deleting '{p.AsSpan( TargetPath.Length )}'." );
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
                                {_existing.Order().Concatenate( " > " + Environment.NewLine )}They may be deleted on the the next successful run.
                                """ );
            }
        }
    }
}
