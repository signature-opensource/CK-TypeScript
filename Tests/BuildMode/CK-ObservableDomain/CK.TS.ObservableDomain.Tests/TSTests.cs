using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.ObservableDomain.Tests
{
    [TestFixture]
    public class TSTests
    {
        [Test]
        public async Task CK_TS_ObservableDomain_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();

            var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
            engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.ObservableDomain" );
            var tsConfig = engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
            tsConfig.GitIgnoreCKGenFolder = true;
            engineConfig.RunSuccessfully();

            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
            runner.Run();
        }

    }
}
