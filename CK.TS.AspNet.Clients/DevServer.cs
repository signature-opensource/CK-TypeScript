using CK.Core;
using Microsoft.AspNetCore.Builder;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace CK.TypeScript;

/// <summary>
/// Starts the dev server of a client application ("ng serve" through its yarn) and stops it with the host.
/// </summary>
static class DevServer
{
    internal static void EnsureRunning( IStaticLogger monitor, WebApplication app, ClientInfo client, string projectPath )
    {
        var port = client.DevServerPort;
        if( IsListening( port ) )
        {
            monitor.Info( $"Something listens on port {port}: it is used as the dev server of the client application '{client}'." );
            return;
        }
        var yarn = FindYarn( projectPath );
        if( yarn == null )
        {
            monitor.Warn( $"No yarn found (.yarn/releases/yarn*.cjs in or above '{projectPath}'): the dev server of the client application '{client}' must be started (\"ng serve --port {port}\")." );
            return;
        }
        var process = Process.Start( new ProcessStartInfo( "node", $"\"{yarn}\" ng serve --port {port}" )
        {
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        } );
        if( process == null )
        {
            monitor.Warn( $"Unable to start the dev server of the client application '{client}'." );
            return;
        }
        process.OutputDataReceived += ( _, e ) => { if( !string.IsNullOrWhiteSpace( e.Data ) ) monitor.Info( $"[{client.Name}] {e.Data}" ); };
        process.ErrorDataReceived += ( _, e ) => { if( !string.IsNullOrWhiteSpace( e.Data ) ) monitor.Warn( $"[{client.Name}] {e.Data}" ); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        app.Lifetime.ApplicationStopping.Register( () =>
        {
            try
            {
                if( !process.HasExited ) process.Kill( entireProcessTree: true );
            }
            catch( Exception ex )
            {
                monitor.Warn( $"While stopping the dev server of the client application '{client}'.", ex );
            }
            process.Dispose();
        } );
        monitor.Info( $"Started the dev server of the client application '{client}' on port {port} (it compiles before it answers)." );
    }

    static bool IsListening( int port )
    {
        try
        {
            using var c = new TcpClient();
            return c.ConnectAsync( "localhost", port ).Wait( 500 ) && c.Connected;
        }
        catch( Exception )
        {
            return false;
        }
    }

    // Same lookup as the TypeScript engine: the first ".yarn/releases/yarn*js" in or above the project folder.
    static string? FindYarn( string projectPath )
    {
        for( var d = new DirectoryInfo( projectPath ); d != null; d = d.Parent )
        {
            var releases = Path.Combine( d.FullName, ".yarn", "releases" );
            if( Directory.Exists( releases ) )
            {
                var yarn = Directory.GetFiles( releases )
                                    .FirstOrDefault( f => Path.GetFileName( f ).StartsWith( "yarn" ) && f.EndsWith( "js" ) );
                if( yarn != null ) return yarn;
            }
        }
        return null;
    }
}
