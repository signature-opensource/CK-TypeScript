using CK.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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
    ///   In Development, when "Clients:{Name}:DevServer" is configured (typically on the command line,
    ///   <c>--Clients:MiniApp:DevServer=http://localhost:4201</c>), what the host doesn't handle under the request path is
    ///   forwarded to this dev server: the host stays the origin.
    ///   </item>
    ///   <item>
    ///   Otherwise, its built files (<see cref="ClientInfo.ProjectPath"/>/<see cref="ClientInfo.OutputPath"/>, relative to
    ///   the content root) are served as static files, with a fallback to its "index.html" for its client routes.
    ///   A client application that is not built is skipped with a warning.
    ///   </item>
    /// </list>
    /// </summary>
    /// <param name="app">This application.</param>
    /// <returns>This application.</returns>
    public static WebApplication MapClients( this WebApplication app )
    {
        var monitor = ActivityMonitor.StaticLogger;
        foreach( var client in app.Services.GetRequiredService<ClientDirectory>().Clients )
        {
            // The default client application ("/") has an empty path prefix.
            var requestPath = client.RequestPath.TrimEnd( '/' );
            var devServer = app.Environment.IsDevelopment() ? app.Configuration[$"Clients:{client.Name}:DevServer"] : null;
            var output = Path.Combine( app.Environment.ContentRootPath, client.ProjectPath, client.OutputPath );
            if( !string.IsNullOrEmpty( devServer ) )
            {
                monitor.Info( $"Client application '{client}' is served by its dev server '{devServer}'." );
                app.MapForwarder( $"{requestPath}/{{**catch-all}}", devServer )
                   .Add( b => ((RouteEndpointBuilder)b).Order = int.MaxValue );
            }
            else if( Directory.Exists( output ) )
            {
                monitor.Info( $"Client application '{client}' is served from '{output}'." );
                var files = new PhysicalFileProvider( output );
                app.UseDefaultFiles( new DefaultFilesOptions { FileProvider = files, RequestPath = requestPath } );
                app.UseStaticFiles( new StaticFileOptions { FileProvider = files, RequestPath = requestPath } );
                // The fallback rewrites the request path to the file path: it must be under the RequestPath.
                app.MapFallbackToFile( $"{requestPath}/{{*path:nonfile}}",
                                       $"{requestPath.TrimStart( '/' )}/index.html".TrimStart( '/' ),
                                       new StaticFileOptions { FileProvider = files, RequestPath = requestPath } );
            }
            else
            {
                monitor.Warn( $"Client application '{client}' is not built ('{output}' not found): it is not served." );
            }
        }
        return app;
    }
}
