using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Ng.AspNet.Auth.Tests;

[TestFixture]
public class NgAspNetAuthTests
{
    [Test]
    public async Task Demo_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();

        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
        configuration.FirstBinPath.Assemblies.Add( "CK.Ng.AspNet.Auth" );
        await configuration.RunSuccessfullyAsync();

        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }
}
