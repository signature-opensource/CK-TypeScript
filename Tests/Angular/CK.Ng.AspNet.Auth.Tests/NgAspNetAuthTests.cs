using CK.Testing;
using Microsoft.AspNetCore.Builder;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Ng.AspNet.Auth.Tests;

[TestFixture]
public class NgAspNetAuthTests
{
    [Test]
    public async Task CK_NG_AspNet_Auth_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();

        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        var tsConfig = configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
        configuration.FirstBinPath.Assemblies.Add( "CK.Ng.AspNet.Auth" );
        var map = (await configuration.RunSuccessfullyAsync()).LoadMap();

        var builder = WebApplication.CreateSlimBuilder();

        await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, server.ServerAddress );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }
}
