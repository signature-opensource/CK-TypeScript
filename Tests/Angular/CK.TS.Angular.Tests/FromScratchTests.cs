using CK.Core;
using CK.MiscDemo;
using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class FromScratchTests
{
    [Test]
    [Explicit( "Takes about 3 minutes!" )]
    public async Task in_a_TempPath_folder_Async()
    {
        NormalizedPath root = FileUtil.CreateUniqueTimedFolder( Path.GetTempPath(), null, DateTime.UtcNow );
        try
        {
            var configuration = TestHelper.CreateDefaultEngineConfiguration();
            configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
            configuration.FirstBinPath.Types.Add( typeof( DemoNgModule ) );

            configuration.EnsureAspect<TypeScriptAspectConfiguration>();
            var ts = configuration.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
            ts.TargetProjectPath = root;
            ts.IntegrationMode = CKGenIntegrationMode.Inline;
            await configuration.RunSuccessfullyAsync();

            await using var runner = TestHelper.CreateTypeScriptRunner( root );
            await TestHelper.SuspendAsync( resume => resume );
            runner.Run();
        }
        finally
        {
            TestHelper.CleanupFolder( root, ensureFolderAvailable: false );
        }
    }
}
