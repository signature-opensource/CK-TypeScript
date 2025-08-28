using CK.Core;
using CK.Setup;
using CK.Testing;
using Microsoft.AspNetCore.Builder;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Ng.AspNet.Auth.Basic.Tests;

[TestFixture]
public class FromScratchTests
{
    [Test]
    public async Task From_Scratch_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();
        TestHelper.CleanupFolder( targetProjectPath.AppendPart( "ck-gen" ), ensureFolderAvailable: false );
        TestHelper.CleanupFolder( targetProjectPath.AppendPart( "public" ), ensureFolderAvailable: false );
        TestHelper.CleanupFolder( targetProjectPath.AppendPart( "src" ), ensureFolderAvailable: false );
        TestHelper.CleanupFolder( targetProjectPath.AppendPart( ".vscode" ), ensureFolderAvailable: false );
        DeleteFiles( targetProjectPath,
                     ".editorconfig", ".gitignore", ".pnp.cjs", ".pnp.loader.mjs",
                     "angular.json", "package.json",
                     "README.md",
                     "tsconfig.app.json", "tsconfig.json", "tsconfig.spec.json",
                     "yarn.lock" );
        await RunInFolderAsync( targetProjectPath );

        static void DeleteFiles( NormalizedPath targetProjectPath, params string[] fileName )
        {
            foreach( var f in fileName )
            {
                var path = targetProjectPath.AppendPart( f );
                if( File.Exists( path ) )
                {
                    File.Delete( path );
                }
            }
        }
    }

    [Test]
    [Explicit( "Takes about 3 minutes!" )]
    public async Task in_a_TempPath_folder_Async()
    {
        NormalizedPath root = FileUtil.CreateUniqueTimedFolder( Path.GetTempPath(), null, DateTime.UtcNow );
        try
        {
            await RunInFolderAsync( root );
        }
        finally
        {
            TestHelper.CleanupFolder( root, ensureFolderAvailable: false );
        }
    }

    static async Task RunInFolderAsync( NormalizedPath root )
    {
        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.EnsureAspect<TypeScriptAspectConfiguration>();
        configuration.FirstBinPath.Path = TestHelper.BinFolder;
        NgAspNetAuthBasicTests.AddAssembliesAndTypes( configuration );

        var tsConfig = configuration.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        tsConfig.TargetProjectPath = root;
        tsConfig.ActiveCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ) );
        var map = (await configuration.RunSuccessfullyAsync()).LoadMap();

        var thisAppName = root.LastPart.Replace( ' ', '_' );
        using( TestHelper.Monitor.OpenInfo( $"""
            Copying '*.spec.ts' from 'TSInlineTests/CK_Ng_AspNet_Auth_Basic/src/app' to this 'src/app' folder.
            Replacing "CK_Ng_AspNet_Auth_Basic" by "{thisAppName}" in tests.            
            """ ) )
            
        {
            var persistentAppFolder = TestHelper.TestProjectFolder.Combine( "TSInlineTests/CK_Ng_AspNet_Auth_Basic/src/app" );
            var targetAppFolder = root.Combine( "src/app" );

            foreach( var specFile in Directory.EnumerateFiles( persistentAppFolder, "*.spec.ts" ) )
            {
                var text = File.ReadAllText( specFile );
                text = text.Replace( "CK_Ng_AspNet_Auth_Basic", thisAppName );
                File.WriteAllText( targetAppFolder.AppendPart( Path.GetFileName( specFile ) ), text );
            }
        }

        // Checks that NgZorro AppStyleImport works as expected.
        var srcStyles = File.ReadAllLines( root.Combine( "src/styles.less" ) );
        srcStyles.ShouldContain( "@import 'ng-zorro-antd/ng-zorro-antd.less';" )
                 .ShouldContain( "@import '../ck-gen/styles/styles.less';" );


        var builder = WebApplication.CreateSlimBuilder();
        await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
        await using var runner = TestHelper.CreateTypeScriptRunner( root, server.ServerAddress );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
        // It's not because the "src/app.spec.ts" succeeds that
        // build is successful: even "src/app.ts" may not compile...
        runner.Run( "build" );
    }
}
