using NUnit.Framework;
using System.Threading.Tasks;
using CK.Testing;
using static CK.Testing.StObjEngineTestHelper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using CK.AspNet.Auth;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;
using System;
using CK.Setup;

namespace CK.TS.AspNet.Auth.Tests
{
    [TestFixture]
    public class TSTests
    {
        [Test]
        public async Task CK_TS_AspNet_Auth_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptBuildModeTargetProjectPath();

            var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
            engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.AspNet.Auth" );
            var map = engineConfig.RunSuccessfully().LoadMap();

            await using var server = await TestHelper.CreateMinimalAspNetServerAsync(
                services =>
                {
                    services.AddCors();
                    services.AddAuthentication( WebFrontAuthOptions.OnlyAuthenticationScheme )
                            .AddWebFrontAuth( options =>
                            {
                                options.SlidingExpirationTime = TimeSpan.FromMinutes( 10 );
                            } );
                    services.AddSingleton<FakeUserDatabase>();
                    services.AddSingleton<IWebFrontAuthLoginService,FakeWebFrontLoginService>();
                    services.AddStObjMap( TestHelper.Monitor, map );
                },
                app =>
                {
                    app.UseCors( o => o.AllowAnyMethod().AllowCredentials().AllowAnyHeader().SetIsOriginAllowed( _ => true ) );
                    app.UseAuthentication();
                    app.UseWelcomePage();
                } );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
            await TestHelper.SuspendAsync( resume => resume );
            runner.Run();
        }

    }

}
