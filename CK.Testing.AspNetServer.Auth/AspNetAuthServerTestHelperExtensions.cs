using CK.AspNet.Auth;
using CK.Auth;
using CK.Core;
using CK.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Testing
{
    /// <summary>
    /// Offers <see cref="CreateAspNetAuthServerAsync(IStObjMap, Action{IServiceCollection}?, Action{IApplicationBuilder}?, Action{WebFrontAuthOptions}?)"/>
    /// and <see cref="CreateAspNetAuthServerAsync(BinPathConfiguration, Action{IServiceCollection}?, Action{IApplicationBuilder}?, Action{WebFrontAuthOptions}?)"/>
    /// helpers.
    /// </summary>
    public static class AspNetAuthServerTestHelperExtensions
    {
        /// <summary>
        /// Creates, configures and starts a <see cref="RunningAspNetServer"/> that supports authentication.
        /// <para>
        /// This <see cref="IStObjMap"/> must have the <see cref="WebFrontAuthService"/> and a <see cref="IWebFrontAuthLoginService"/>
        /// implementation otherwise nothing will work. Use the <see cref="BinPathConfiguration"/>.CreateAspNetAuthServerAsync extension
        /// method to create a running server "from scratch".
        /// </para>
        /// </summary>
        /// <param name="map">This StObjMap.</param>
        /// <param name="configureServices">Optional application services configurator.</param>
        /// <param name="configureApplication">Optional application configurator.</param>
        /// <param name="webFrontAuthOptions">
        /// Optional authentication options configurator.
        /// By default <see cref="WebFrontAuthOptions.SlidingExpirationTime"/> is set to 10 minutes.
        /// </param>
        /// <returns>A running Asp.NET server with authentication support.</returns>
        public static Task<RunningAspNetServer> CreateAspNetAuthServerAsync( this IStObjMap map,
                                                                             Action<IServiceCollection>? configureServices = null,
                                                                             Action<IApplicationBuilder>? configureApplication = null,
                                                                             Action<WebFrontAuthOptions>? webFrontAuthOptions = null )
        {
            static void ConfigureServices( IServiceCollection services,
                                           IStObjMap map,
                                           Action<IServiceCollection>? configureServices,
                                           Action<WebFrontAuthOptions>? webFrontAuthOptions )
            {
                services.AddCors();
                services.AddAuthentication( WebFrontAuthOptions.OnlyAuthenticationScheme )
                        .AddWebFrontAuth( webFrontAuthOptions ?? (options =>
                        {
                            options.SlidingExpirationTime = TimeSpan.FromMinutes( 10 );
                        }) );

                configureServices?.Invoke( services );

                services.AddStObjMap( TestHelper.Monitor, map );
            }

            static void ConfigureApplication( IApplicationBuilder app, Action<IApplicationBuilder>? configureApplication )
            {
                app.UseCors( o => o.AllowAnyMethod().AllowCredentials().AllowAnyHeader().SetIsOriginAllowed( _ => true ) );
                app.UseAuthentication();

                configureApplication?.Invoke( app );
            }

            return TestHelper.CreateMinimalAspNetServerAsync( configureServices: services => ConfigureServices( services, map, configureServices, webFrontAuthOptions ),
                                                              configureApplication: app => ConfigureApplication( app, configureApplication ) );
        }

        /// <summary>
        /// Creates, configures and starts a <see cref="RunningAspNetServer"/> that supports authentication from
        /// a <see cref="BinPathConfiguration"/>: the CKomposable engine is ran with the <see cref="FakeWebFrontLoginService"/>
        /// and <see cref="FakeUserDatabase"/> (that may be specialized) and a <see cref="WebFrontAuthService"/> that will
        /// handle "/.webFront" requests.
        /// </summary>
        /// <param name="binPath">This StObjMap.</param>
        /// <param name="configureServices">Optional application services configurator.</param>
        /// <param name="configureApplication">Optional application configurator.</param>
        /// <param name="webFrontAuthOptions">
        /// Optional authentication info.
        /// By default <see cref="WebFrontAuthOptions.SlidingExpirationTime"/> is set to 10 minutes.
        /// </param>
        /// <returns>A running Asp.NET server with authentication support.</returns>
        public static Task<RunningAspNetServer> CreateAspNetAuthServerAsync( this BinPathConfiguration binPath,
                                                                             Action<IServiceCollection>? configureServices = null,
                                                                             Action<IApplicationBuilder>? configureApplication = null,
                                                                             Action<WebFrontAuthOptions>? webFrontAuthOptions = null )
        {
            Throw.CheckArgument( "The BinPathConfiguration must belong to an EngineConfiguration.", binPath.Owner != null );

            // These 2 services are required by the WebFrontAuthService.
            binPath.Types.Add( typeof( WebFrontAuthService ),
                               typeof( AuthenticationInfoTokenService ),
                               typeof( StdAuthenticationTypeSystem ),
                               typeof( FakeUserDatabase ),
                               typeof( FakeWebFrontLoginService ) );
            var map = binPath.Owner.RunSuccessfully().LoadMap( binPath.Name );

            return CreateAspNetAuthServerAsync( map, configureServices, configureApplication, webFrontAuthOptions );
        }


    }
}
