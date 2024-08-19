using CK.Auth;
using CK.Core;
using CK.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.AspNet.Auth.Tests
{
    [TestFixture]
    public class TSTests
    {
        [Test]
        public async Task CK_TS_AspNet_Auth_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptNpmPackageTargetProjectPath();
            targetProjectPath.Parts[^2].Should().Be( "TSNpmPackageTests" );

            var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
            engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.AspNet.Auth" );
            var tsConfig = engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
            tsConfig.GitIgnoreCKGenFolder = true;
            var map = engineConfig.RunSuccessfully().LoadMap();

            var builder = WebApplication.CreateSlimBuilder();

            // Temp
            Throw.DebugAssert( !builder.Services.Any( item => item.ServiceType == typeof( FakeUserDatabase ) ) );
            builder.Services.AddSingleton<FakeUserDatabase>();
            builder.AddUnsafeAllowAllCors();

            await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
            await TestHelper.SuspendAsync( resume => resume );
            runner.Run();
        }

        [Test]
        public async Task CK_TS_AspNet_Auth_with_SrcFolder_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptNpmPackageTargetProjectPath();
            targetProjectPath.Parts[^2].Should().Be( "TSNpmPackageTests" );

            var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
            engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.AspNet.Auth" );
            var tsConfig = engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
            tsConfig.GitIgnoreCKGenFolder = true;
            tsConfig.UseSrcFolder = true;
            var map = engineConfig.RunSuccessfully().LoadMap();

            var builder = WebApplication.CreateSlimBuilder();

            // Temp
            Throw.DebugAssert( !builder.Services.Any( item => item.ServiceType == typeof( FakeUserDatabase ) ) );
            builder.Services.AddSingleton<FakeUserDatabase>();
            builder.AddUnsafeAllowAllCors();

            await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
            await TestHelper.SuspendAsync( resume => resume );
            runner.Run();
        }

        [Test]
        public async Task CK_TS_AspNet_Auth_Inline_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();
            targetProjectPath.Parts[^2].Should().Be( "TSInlineTests" );

            var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
            engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.AspNet.Auth" );
            var tsConfig = engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
            tsConfig.GitIgnoreCKGenFolder = true;

            var map = engineConfig.RunSuccessfully().LoadMap();

            var builder = WebApplication.CreateSlimBuilder();

            // Temp
            Throw.DebugAssert( !builder.Services.Any( item => item.ServiceType == typeof( FakeUserDatabase ) ) );
            builder.Services.AddSingleton<FakeUserDatabase>();
            builder.AddUnsafeAllowAllCors();

            await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
            await TestHelper.SuspendAsync( resume => resume );
            runner.Run();
        }
    }

}
