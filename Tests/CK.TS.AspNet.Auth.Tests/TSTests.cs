using NUnit.Framework;
using System.Threading.Tasks;
using CK.Testing;
using static CK.Testing.StObjEngineTestHelper;

namespace CK.TS.AspNet.Auth.Tests
{
    [TestFixture]
    public class TSTests
    {
        [Test]
        public async Task CK_TS_AspNet_Auth_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptBuildModeTargetProjectPath();
            TestHelper.RunSuccessfulEngineWithTypeScript( targetProjectPath, typeof( CK.AspNet.Auth.TSPackage ).Assembly );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
            runner.Run();
        }

    }
}
