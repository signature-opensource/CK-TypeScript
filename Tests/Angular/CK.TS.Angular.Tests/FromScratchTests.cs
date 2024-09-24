using CK.Core;
using CK.Demo;
using CK.Setup;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using static CK.Setup.EngineResult;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class FromScratchTests
{
    [Test]
    [Explicit( "Takes around 40 seconds." )]
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
            ts.AutoInstallYarn = true;
            ts.AutoInstallJest = true;
            configuration.RunSuccessfully();

            File.Exists( root.Combine( "src/sample.spec.ts" ) ).Should().BeTrue();

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
