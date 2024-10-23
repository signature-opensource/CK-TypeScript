using CK.Demo;
using CK.NG.AspNet.Auth;
using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularTests
{
    [Test]
    public async Task Demo_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();

        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        var ts = configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        ts.CKGenBuildMode = false;

        configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
        configuration.FirstBinPath.Types.Add( typeof( DemoNgModule ), typeof( LoginComponent ), typeof( PasswordLostComponent ) );
        await configuration.RunSuccessfullyAsync();

        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }
}
