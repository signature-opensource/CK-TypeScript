using CK.Core;
using CK.Setup;
using CK.Testing;
using Shouldly;
using Microsoft.AspNetCore.Builder;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.AspNet.Auth.Tests;

[TestFixture]
public class TSTests
{
    [Test]
    public async Task CK_TS_AspNet_Auth_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptNpmPackageTargetProjectPath();

        var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
        engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.AspNet.Auth" );
        var tsConfig = engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        var map = (await engineConfig.RunSuccessfullyAsync()).LoadMap();

        var builder = WebApplication.CreateSlimBuilder();

        await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }

    [Test]
    public async Task CK_TS_AspNet_Auth_Inline_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();
        targetProjectPath.Parts[^2].ShouldBe( "TSInlineTests" );

        var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
        engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.AspNet.Auth" );
        var tsConfig = engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );

        var map = (await engineConfig.RunSuccessfullyAsync()).LoadMap();

        var builder = WebApplication.CreateSlimBuilder();

        await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }

    [Explicit( "Same as CK_TS_AspNet_Auth_Inline but in a Temp folder (no yarn)." )]
    [Test]
    public async Task CK_TS_AspNet_Auth_Inline_from_scrath_Async()
    {
        NormalizedPath targetProjectPath = FileUtil.CreateUniqueTimedFolder( Path.GetTempPath(), null, DateTime.UtcNow );
        try
        {
            var configuration = TestHelper.CreateDefaultEngineConfiguration();
            configuration.FirstBinPath.Assemblies.Add( "CK.TS.AspNet.Auth" );

            configuration.EnsureAspect<TypeScriptAspectConfiguration>();
            var ts = configuration.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
            ts.TargetProjectPath = targetProjectPath;
            ts.IntegrationMode = CKGenIntegrationMode.Inline;
            var r = await configuration.RunSuccessfullyAsync();

            File.Exists( targetProjectPath.Combine( "src/sample.spec.ts" ) ).ShouldBeTrue();

            var map = r.LoadMap();

            var builder = WebApplication.CreateSlimBuilder();

            await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
            await TestHelper.SuspendAsync( resume => resume );
            runner.Run();
        }
        finally
        {
            TestHelper.CleanupFolder( targetProjectPath, ensureFolderAvailable: false );
        }
    }
}
