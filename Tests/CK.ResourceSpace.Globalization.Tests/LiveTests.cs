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

    public enum LocalFileSupport
    {
        /// <summary>
        /// All the packages are stable (bound to the assembly resources).
        /// </summary>
        NoLocal,
        /// <summary>
        /// All the packages are local (bound to the resource folders).
        /// </summary>
        AllLocal,

        /// <summary>
        /// 0.5 probability for each package to have its resources bound to the assambly's resources
        /// or to the local folder.
        /// </summary>
        Random,
    }

    [TestCase( Scenario.ModifyAppResources,
               LocalesResourceHandler.InstallOption.Minimal|LocalesResourceHandler.InstallOption.WithSortedKeys,
               "en-GB,en-US,fr",
               LocalFileSupport.AllLocal )]
    [TestCase( Scenario.ModifyAppResources,
               LocalesResourceHandler.InstallOption.Minimal|LocalesResourceHandler.InstallOption.WithSortedKeys,
               "en-GB,en-US,fr",
               LocalFileSupport.Random )]
    [TestCase( Scenario.ModifyAppResources,
               LocalesResourceHandler.InstallOption.Minimal|LocalesResourceHandler.InstallOption.WithSortedKeys,
               "en-GB,en-US,fr",
               LocalFileSupport.NoLocal )]
    public async Task LiveState_in_action_Async( Scenario scenario,
                                                 LocalesResourceHandler.InstallOption install,
                                                 string activeCultures,
                                                 LocalFileSupport localFileSupport,
                                                 CancellationToken testCancellation )
    {
        var name = $"{scenario}-{install}[{activeCultures}]";
        var testRootPath = TestHelper.TestProjectFolder.AppendPart( name );
        ResSpace space = Install( testRootPath, activeCultures, install, localFileSupport );

        var cts = new CancellationTokenSource();
        using var fromTest = testCancellation.UnsafeRegister( cts => ((CancellationTokenSource)cts!).Cancel(), cts );
        var runningMonitor = new ActivityMonitor( "Live monitor." );
        var running = Runner.RunAsync( runningMonitor,
                                       space.Data.LiveStatePath + ResSpace.LiveStateFileName,
                                       100,
                                       cts.Token );

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

#pragma warning disable VSTHRD103 // Call async methods when in an async method
        cts.Cancel( throwOnFirstException: true );
#pragma warning restore VSTHRD103 // Call async methods when in an async method
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
            RetryAction( () => File.WriteAllText( enAppResPath, $$"""
                                                        {
                                                          "Public.Title": "{{changeString}}",
                                                        }
                                                        """ ) );

            const int waitMs = 300;
            using( TestHelper.Monitor.OpenInfo( $"Waiting {waitMs} ms." ) )
            {
                await Task.Delay( waitMs );
            }
            var changed = File.ReadAllText( enFinalPath );
            changed.ShouldContain( $$"""
                    "Public.Title": "{{changeString}}"
                    """ );
        }
    }

    static void RetryAction( Action a )
    {
        const int maxRetryCount = 5;
        const int perRetryDelay = 30;
        int retryCount = 0;
        retry:
        try
        {
            a();
            return;
        }
        catch( Exception ex )
        {
            if( ++retryCount <= maxRetryCount )
            {
                TestHelper.Monitor.Warn( $"Failed. Retring n°{retryCount} in {retryCount * perRetryDelay}ms.", ex );
                Thread.Sleep( retryCount * perRetryDelay );
                goto retry;
            }
            TestHelper.Monitor.Error( $"Failed after {maxRetryCount} tries.", ex );
            throw;
        }
    }

    static ResSpace Install( NormalizedPath testRootPath,
                             string activeCultures,
                             LocalesResourceHandler.InstallOption install,
                             LocalFileSupport localFileSupport )
    {
        var config = new ResSpaceConfiguration();
        config.AppResourcesLocalPath = testRootPath.AppendPart( "AppResources" );
        var spaceCollector = config.Build( TestHelper.Monitor ).ShouldNotBeNull();

        var random = new Random();
        bool IgnoreLocal()
        {
            return localFileSupport switch
            {
                LocalFileSupport.AllLocal => false,
                LocalFileSupport.NoLocal => true,
                _ => random.Next( 2 ) != 0
            };
        }

        spaceCollector.RegisterPackage( TestHelper.Monitor, typeof( Demo.Gear.SystemState.Package ), ignoreLocal: IgnoreLocal() );
        spaceCollector.RegisterPackage( TestHelper.Monitor, typeof( Demo.Public.TopBar.Package ), ignoreLocal: IgnoreLocal() );
        spaceCollector.RegisterPackage( TestHelper.Monitor, typeof( Demo.Public.Footer.Package ), ignoreLocal: IgnoreLocal() );
        spaceCollector.RegisterPackage( TestHelper.Monitor, typeof( Demo.PublicSection.Package ), ignoreLocal: IgnoreLocal() );

        var spaceDataBuilder = new ResSpaceDataBuilder( spaceCollector );
        var spaceData = spaceDataBuilder.Build( TestHelper.Monitor ).ShouldNotBeNull();

        var spaceBuilder = new ResSpaceBuilder( spaceData );
        var installer = new InitialFileSystemInstaller( testRootPath.AppendPart("App") );
        var localesHandler = new LocalesResourceHandler( installer,
                                                         spaceData.SpaceDataCache,
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
