using CK.Core;
using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using System.IO;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests
{
    [TestFixture]
    public class FromScratchTests
    {
        [Test]
        public void Test()
        {
            NormalizedPath root = Path.GetTempPath();
            try
            {
                bool withTest = false;
                var configuration = TestHelper.CreateDefaultEngineConfiguration();
                configuration.EnsureAspect<TypeScriptAspectConfiguration>();
                var b = configuration.FirstBinPath;
                var ts = b.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
                ts.AutoInstallYarn = true;
                ts.AutoInstallVSCodeSupport = true;
                ts.EnsureTestSupport = withTest;
                ts.CKGenBuildMode = false;
            }
            finally
            {
                TestHelper.CleanupFolder( root, ensureFolderAvailable: false );
            }
        }
    }
}
