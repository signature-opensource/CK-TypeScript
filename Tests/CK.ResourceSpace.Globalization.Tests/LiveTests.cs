using CK.Core;
using CK.TypeScript.LiveEngine;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.ResourceSpace.Globalization.Tests;

[TestFixture]
public class LiveTests
{
    public enum Scenario
    {
        None,
        ModifyAppResources,
    }

    [TestCase( Scenario.ModifyAppResources, LocalesResourceHandler.InstallOption.Minimal, "en-GB,en-US,fr" )]
    public async Task LiveState_in_action_Async( Scenario scenario,
                                                 LocalesResourceHandler.InstallOption install,
                                                 string activeCultures,
                                                 CancellationToken testCancellation )
    {
        var name = $"{scenario}-{install}[{activeCultures}]";
        var testRootPath = TestHelper.TestProjectFolder.AppendPart( name );
        ResSpace space = Install( testRootPath, activeCultures, install );

        var cts = new CancellationTokenSource();
        using var fromTest = testCancellation.UnsafeRegister( cts => ((CancellationTokenSource)cts!).Cancel(), cts );
        var runningMonitor = new ActivityMonitor( "Live monitor." );
        var running = Runner.RunAsync( runningMonitor, space.Data.LiveStatePath + ResSpace.LiveStateFileName, cts.Token );

        space.Data.AppPackage.Resources.LocalPath.ShouldNotBeNull( "AppResourcesLocalPath is necessarily a local folder." );

        var finalAppPath = testRootPath.AppendPart( "App" );

        switch( scenario )
        {
            case Scenario.ModifyAppResources:
                await ModifyAppResourcesAsync( TestHelper.Monitor, space.Data.AppPackage.Resources.LocalPath, finalAppPath );
                break;
            default:
                await Task.Delay( 50, testCancellation );
                break;
        }

        cts.Cancel( throwOnFirstException: true );
        await running.WaitAsync( testCancellation );
    }

    static async Task ModifyAppResourcesAsync( IActivityMonitor monitor, string resAppPath, NormalizedPath finalAppPath )
    {
        // The final file is a json (not a jsonc).
        var enFinalPath = finalAppPath.Combine( "locales/en.json" );

        // We must create the locales/ folder.
        var appResPathLocales = resAppPath + "locales" + Path.DirectorySeparatorChar;
        Directory.CreateDirectory( appResPathLocales );
        try
        {
            await ChangePublicTitleAsync( enFinalPath, appResPathLocales, "MODIFIED-1" );
            await ChangePublicTitleAsync( enFinalPath, appResPathLocales, "MODIFIED-2" );
        }
        finally
        {
            TestHelper.CleanupFolder( appResPathLocales, ensureFolderAvailable: false );
        }

        static async Task ChangePublicTitleAsync( NormalizedPath enFinalPath, string appResPathLocales, string changeString )
        {
            var enAppResPath = appResPathLocales + "en.jsonc";
            File.WriteAllText( enAppResPath, $$"""
                {
                  "Public.Title": "{{changeString}}",
                }
                """ );
            await Task.Delay( 200 );
            var changed = File.ReadAllText( enFinalPath );
            changed.ShouldContain( $$"""
                    "Public.Title": "{{changeString}}"
                    """ );
        }
    }

    static ResSpace Install( NormalizedPath testRootPath,
                             string activeCultures,
                             LocalesResourceHandler.InstallOption install )
    {
        var config = new ResSpaceConfiguration();
        config.AppResourcesLocalPath = testRootPath.AppendPart( "AppResources" );
        var spaceCollector = config.Build( TestHelper.Monitor ).ShouldNotBeNull();
        spaceCollector.RegisterPackage( TestHelper.Monitor, typeof( Demo.Gear.SystemState.Package ) );
        spaceCollector.RegisterPackage( TestHelper.Monitor, typeof( Demo.Public.TopBar.Package ) );
        spaceCollector.RegisterPackage( TestHelper.Monitor, typeof( Demo.Public.Footer.Package ) );
        spaceCollector.RegisterPackage( TestHelper.Monitor, typeof( Demo.PublicSection.Package ) );

        var spaceDataBuilder = new ResSpaceDataBuilder( spaceCollector );
        var spaceData = spaceDataBuilder.Build( TestHelper.Monitor ).ShouldNotBeNull();

        var spaceBuilder = new ResSpaceBuilder( spaceData );
        var installer = new InitialFileSystemInstaller( testRootPath.AppendPart("App") );
        var localesHandler = new LocalesResourceHandler( installer,
                                                         spaceData.ResPackageDataCache,
                                                         "locales",
                                                         new ActiveCultureSet( activeCultures.Split( ",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries )
                                                                                             .Select( NormalizedCultureInfo.EnsureNormalizedCultureInfo ) ),
                                                         install );
        spaceBuilder.RegisterHandler( TestHelper.Monitor, localesHandler ).ShouldBeTrue();
        var space = spaceBuilder.Build( TestHelper.Monitor ).ShouldNotBeNull();
        space.Install( TestHelper.Monitor ).ShouldBeTrue();
        // Install has created the folders.
        File.WriteAllText( testRootPath.AppendPart( ".gitignore" ), "*" );
        return space;
    }
}
