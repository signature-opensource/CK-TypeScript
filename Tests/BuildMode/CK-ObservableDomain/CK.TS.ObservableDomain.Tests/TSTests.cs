using NUnit.Framework;
using System.Threading.Tasks;
using CK.Testing;
using static CK.Testing.StObjEngineTestHelper;

namespace CK.TS.ObservableDomain.Tests
{
    [TestFixture]
    public class TSTests
    {
        [Test]
        public async Task CK_TS_ObservableDomain_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptBuildModeTargetProjectPath();
            TestHelper.RunSuccessfulEngineWithTypeScript( targetProjectPath, typeof( CK.ObservableDomain.Package ).Assembly );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
            runner.Run();
        }

    }
}
