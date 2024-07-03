using NUnit.Framework;
using System.Threading.Tasks;
using CK.Testing;
using static CK.Testing.StObjEngineTestHelper;
using CK.Setup;

namespace CK.TS.ObservableDomain.Tests
{
    [TestFixture]
    public class TSTests
    {
        [Test]
        public async Task CK_TS_ObservableDomain_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptBuildModeTargetProjectPath();

            var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
            engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.ObservableDomain" );
            engineConfig.RunSuccessfully();

            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
            runner.Run();
        }

    }
}
