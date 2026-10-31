using CK.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace CK.TypeScript;

/// <summary>
/// Serves the client applications of the <see cref="ClientDirectory"/>.
/// </summary>
public static class ClientsApplicationExtensions
{
    /// <summary>
    /// Serves each client application of the <see cref="ClientDirectory"/> under its <see cref="ClientInfo.RequestPath"/>
    /// (the default one, served at "/", is mapped last):
    /// <list type="bullet">
    ///   <item>
    ///   In Development, by its dev server: the host starts it ("ng serve" on its <see cref="ClientInfo.DevServerPort"/>, stopped
    ///   with the host) unless something already listens on this port, and forwards to it what the host doesn't handle under the
    ///   request path: the host stays the origin.
    ///   </item>
    ///   <item>
    ///   Otherwise, or when its "dist" is used (<see cref="ClientInfo.UseDist"/>, that the host's configuration overrides with
    ///   "Clients:{Name}:UseDist" or "Clients:UseDist"), by its built files (<see cref="ClientInfo.ProjectPath"/>/<see cref="ClientInfo.OutputPath"/>,
    ///   relative to the content root) with a fallback to its "index.html" for its client routes. A client application that is
    ///   not built is skipped with a warning.
    ///   </item>
    /// </list>
    /// </summary>
    /// <param name="app">This application.</param>
    /// <returns>This application.</returns>
    public static WebApplication MapClients( this WebApplication app )
    {
        var monitor = ActivityMonitor.StaticLogger;
        var isDevelopment = app.Environment.IsDevelopment();
        foreach( var client in app.Services.GetRequiredService<ClientDirectory>().Clients )
        {
            // The default client application ("/") has an empty path prefix.
            var requestPath = client.RequestPath.TrimEnd( '/' );
            var projectPath = Path.Combine( app.Environment.ContentRootPath, client.ProjectPath );
            if( isDevelopment && !UseDist( monitor, app.Configuration, client ) )
            {
                DevServer.EnsureRunning( monitor, app, client, projectPath );
                app.MapForwarder( $"{requestPath}/{{**catch-all}}", $"http://localhost:{client.DevServerPort}" )
                   .Add( b => ((RouteEndpointBuilder)b).Order = int.MaxValue );
            }
            else
            {
                MapDist( monitor, app, client, requestPath, Path.Combine( projectPath, client.OutputPath ) );
            }
        }
        return app;
    }

    static bool UseDist( IStaticLogger monitor, IConfiguration configuration, ClientInfo client )
    {
        var key = $"Clients:{client.Name}:UseDist";
        var value = configuration[key];
        if( value == null )
        {
            key = "Clients:UseDist";
            value = configuration[key];
        }
        if( value == null ) return client.UseDist;
        if( bool.TryParse( value, out var useDist ) ) return useDist;
        monitor.Warn( $"Invalid '{key}' configuration '{value}' (expecting true or false): client application '{client}' uses its default (UseDist = {client.UseDist})." );
        return client.UseDist;
    }

    static void MapDist( IStaticLogger monitor, WebApplication app, ClientInfo client, string requestPath, string output )
    {
        if( !Directory.Exists( output ) )
        {
            monitor.Warn( $"Client application '{client}' is not built ('{output}' not found): it is not served." );
            return;
        }
        monitor.Info( $"Client application '{client}' is served from '{output}'." );
        var files = new PhysicalFileProvider( output );
        app.UseDefaultFiles( new DefaultFilesOptions { FileProvider = files, RequestPath = requestPath } );
        app.UseStaticFiles( new StaticFileOptions { FileProvider = files, RequestPath = requestPath } );
        // The fallback rewrites the request path to the file path: it must be under the RequestPath.
        app.MapFallbackToFile( $"{requestPath}/{{*path:nonfile}}",
                               $"{requestPath.TrimStart( '/' )}/index.html".TrimStart( '/' ),
                               new StaticFileOptions { FileProvider = files, RequestPath = requestPath } );
    }
}
