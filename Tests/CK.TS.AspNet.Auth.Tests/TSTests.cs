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

namespace CK.TS.AspNet.Auth.Tests
{
    [TestFixture]
    public class TSTests
    {
        [Test]
        public async Task CK_TS_AspNet_Auth_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptBuildModeTargetProjectPath();

            var runResult = TestHelper.RunSuccessfulEngineWithTypeScript( targetProjectPath, typeof( CK.AspNet.Auth.TSPackage ).Assembly );
            runResult.Success.Should().BeTrue();

            var engineMap = runResult.Groups[0].LoadStObjMap( TestHelper.Monitor );

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
                    services.AddStObjMap( TestHelper.Monitor, engineMap );
                },
                app =>
                {
                    app.UseCors( o => o.AllowAnyMethod().AllowCredentials().WithOrigins( "::1" ) );
                    app.UseAuthentication();
                    app.UseWelcomePage();
                } );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
            await TestHelper.SuspendAsync( resume => resume );
            runner.Run();
        }

    }

}
