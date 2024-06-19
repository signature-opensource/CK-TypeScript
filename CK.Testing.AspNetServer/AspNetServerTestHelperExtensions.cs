using CK.Core;
using CK.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CK.Testing
{
    public static class AspNetServerTestHelperExtensions
    {
        /// <summary>
        /// Creates, configures and starts a <see cref="RunningAspNetServer"/> (that must be disposed once done with it).
        /// <para>
        /// This services configuration is minimal:
        /// <code>
        ///             builder.WebHost.UseScopedHttpContext();
        ///             configureServices?.Invoke( builder.Services );
        ///             // Don't UseCKMonitoring() here or the GrandOutput.Default will be reconfigured:
        ///             // only register the IActivityMonitor and its ParallelLogger (if they are not already registered).
        ///             builder.Services.TryAddScoped&lt;IActivityMonitor, ActivityMonitor&gt;();
        ///             builder.Services.TryAddScoped( sp => sp.GetRequiredService&lt;IActivityMonitor&gt;().ParallelLogger );
        /// </code>
        /// As well as the the pipeline:
        /// <code>
        ///             // This chooses a random, free port.
        ///             app.Urls.Add( "http://[::1]:0" );
        ///             app.UseGuardRequestMonitor();
        ///             configureApplication?.Invoke( app );
        /// </code>
        /// </para>
        /// </summary>
        /// <param name="helper">This helper.</param>
        /// <param name="configureServices">Application services configurator.</param>
        /// <param name="configureApplication">Optional application configurator.</param>
        /// <returns>A running .NET server or null if an error occurred or the server failed to start.</returns>
        public static async Task<RunningAspNetServer> CreateMinimalAspNetServerAsync( this IMonitorTestHelper helper,
                                                                                       Action<IServiceCollection>? configureServices,
                                                                                       Action<IApplicationBuilder>? configureApplication = null )
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseScopedHttpContext();
            configureServices?.Invoke( builder.Services );
            // Don't UseCKMonitoring() here or the GrandOutput.Default will be reconfigured:
            // only register the IActivityMonitor and its ParallelLogger (if they are not already registered).
            builder.Services.TryAddScoped<IActivityMonitor, ActivityMonitor>();
            builder.Services.TryAddScoped( sp => sp.GetRequiredService<IActivityMonitor>().ParallelLogger );

            var app = builder.Build();
            try
            {
                // This chooses a random, free port.
                app.Urls.Add( "http://[::1]:0" );
                app.UseGuardRequestMonitor();
                configureApplication?.Invoke( app );
                await app.StartAsync().ConfigureAwait( false );

                // The IServer's IServerAddressesFeature feature has the address resolved.
                var server = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
                var addresses = server.Features.Get<IServerAddressesFeature>();
                Throw.DebugAssert( addresses != null && addresses.Addresses.Count > 0 );

                var serverAddress = addresses.Addresses.First();
                helper.Monitor.Info( $"Server started. Server address: '{serverAddress}'." );
                return new RunningAspNetServer( app, serverAddress );
            }
            catch( Exception ex )
            {
                helper.Monitor.Error( "Unhandled error while starting http server.", ex );
                await app.DisposeAsync();
                throw;
            }
        }
    }

}
