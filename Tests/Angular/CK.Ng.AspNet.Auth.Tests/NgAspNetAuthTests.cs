using CK.Testing;
using Microsoft.AspNetCore.Builder;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;
using CK.Core;

namespace CK.Ng.AspNet.Auth.Tests;

[TestFixture]
public class NgAspNetAuthTests
{
    [Test]
    public async Task Demo_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();

        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        var tsConfig = configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        Throw.DebugAssert( tsConfig.CKGenBuildMode );

        configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
        configuration.FirstBinPath.Assemblies.Add( "CK.Ng.AspNet.Auth" );
        var map = (await configuration.RunSuccessfullyAsync()).LoadMap();

        var builder = WebApplication.CreateSlimBuilder();

        await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, new Dictionary<string, string> { { "SERVER_ADDRESS", server.ServerAddress } } );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }
}
