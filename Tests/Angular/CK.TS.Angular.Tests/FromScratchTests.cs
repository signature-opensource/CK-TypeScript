using CK.Core;
using CK.MiscDemo;
using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using Shouldly;
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
            // A client application: its base href is its request path.
            ts.Name = "DemoApp";
            ts.Kind = ClientKind.App;
            ts.DevServerPort = 4277;
            await configuration.RunSuccessfullyAsync();

            var angularJson = File.ReadAllText( root.AppendPart( "angular.json" ) );
            angularJson.ShouldContain( "\"baseHref\": \"/demo-app/\"" );
            angularJson.ShouldContain( "\"port\": 4277" );

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
