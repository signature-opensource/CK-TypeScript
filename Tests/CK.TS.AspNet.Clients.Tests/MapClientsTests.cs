using CK.Core;
using CK.Setup;
using CK.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Tests;

[TestFixture]
public class MapClientsTests
{
    static void CreateBuiltClient( NormalizedPath projectPath, string name, string content )
    {
        var output = projectPath.Combine( $"dist/{name}/browser" );
        Directory.CreateDirectory( output );
        File.WriteAllText( output.AppendPart( "index.html" ), content );
        File.WriteAllText( output.AppendPart( "main.js" ), $"// {name}" );
    }

    // Runs the engine with the App (default client application) and the MiniApp, both built ("dist").
    static async Task<IStObjMap> CreateMapAsync( NormalizedPath root, int appPort, int miniAppPort, bool miniAppUseDist = false )
    {
        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.FirstBinPath.Types.Add( typeof( ClientDirectory ), typeof( ClientsHosting ) );
        configuration.EnsureAspect<TypeScriptAspectConfiguration>();
        var app = configuration.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        app.Name = "App";
        app.Kind = ClientKind.DefaultApp;
        app.DevServerPort = appPort;
        app.TargetProjectPath = root.AppendPart( "App" );
        app.IntegrationMode = CKGenIntegrationMode.None;
        app.AddOtherConfiguration( new TypeScriptBinPathAspectConfiguration
        {
            Name = "MiniApp",
            Kind = ClientKind.App,
            DevServerPort = miniAppPort,
            UseDist = miniAppUseDist,
            TargetProjectPath = root.AppendPart( "MiniApp" ),
            IntegrationMode = CKGenIntegrationMode.None
        } );
        var map = (await configuration.RunSuccessfullyAsync()).LoadMap();
        CreateBuiltClient( root.AppendPart( "App" ), "App", "THE-APP" );
        CreateBuiltClient( root.AppendPart( "MiniApp" ), "MiniApp", "THE-MINI-APP" );
        return map;
    }

    // A fake dev server ("ng serve") that answers with its name and the path it receives.
    static async Task<WebApplication> StartDevServerAsync( string name )
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls( "http://127.0.0.1:0" );
        var devServer = builder.Build();
        devServer.MapGet( "/{**path}", ( Microsoft.AspNetCore.Http.HttpContext ctx ) => $"{name}:{ctx.Request.Path}" );
        await devServer.StartAsync();
        return devServer;
    }

    static int Port( WebApplication app ) => new Uri( app.Urls.First() ).Port;

    // The client projects are under the content root (the test project folder): their ProjectPath is relative.
    static Task<RunningAspNetServer> CreateHostAsync( IStObjMap map, string environment, params (string Key, string Value)[] configuration )
    {
        var builder = WebApplication.CreateSlimBuilder( new WebApplicationOptions
        {
            ContentRootPath = TestHelper.TestProjectFolder,
            EnvironmentName = environment
        } );
        builder.Configuration.AddInMemoryCollection( configuration.Select( c => new KeyValuePair<string, string?>( c.Key, c.Value ) ) );
        return builder.CreateRunningAspNetServerAsync( map, app => app.MapClients() );
    }

    static async Task<string> GetAsync( RunningAspNetServer server, string path ) => await (await server.Client.GetAsync( path )).Content.ReadAsStringAsync();

    [Test]
    public async Task in_Production_the_built_client_applications_are_served_Async()
    {
        var map = await CreateMapAsync( TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false ), 4200, 4201 );
        await using var server = await CreateHostAsync( map, "Production" );

        (await GetAsync( server, "/" )).ShouldBe( "THE-APP" );
        (await GetAsync( server, "/main.js" )).ShouldBe( "// App" );
        // A client route of the default application.
        (await GetAsync( server, "/some/route" )).ShouldBe( "THE-APP" );

        (await GetAsync( server, "/mini-app/" )).ShouldBe( "THE-MINI-APP" );
        (await GetAsync( server, "/mini-app/main.js" )).ShouldBe( "// MiniApp" );
        // A client route of the MiniApp.
        (await GetAsync( server, "/mini-app/some/route" )).ShouldBe( "THE-MINI-APP" );
    }

    [Test]
    public async Task in_Development_the_client_applications_are_served_by_their_dev_server_Async()
    {
        // The dev servers are already running: the host uses them (it doesn't start "ng serve").
        await using var appDev = await StartDevServerAsync( "APP-DEV" );
        await using var miniDev = await StartDevServerAsync( "MINI-DEV" );
        var map = await CreateMapAsync( TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false ), Port( appDev ), Port( miniDev ) );
        await using var server = await CreateHostAsync( map, "Development" );

        // The host forwards what it doesn't handle under the request path: it stays the origin.
        (await GetAsync( server, "/some/route" )).ShouldBe( "APP-DEV:/some/route" );
        (await GetAsync( server, "/mini-app/some/route" )).ShouldBe( "MINI-DEV:/mini-app/some/route" );
    }

    [Test]
    public async Task in_Development_UseDist_serves_the_built_files_and_the_host_configuration_overrides_it_Async()
    {
        await using var appDev = await StartDevServerAsync( "APP-DEV" );
        await using var miniDev = await StartDevServerAsync( "MINI-DEV" );
        // The MiniApp is configured to use its dist.
        var map = await CreateMapAsync( TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false ), Port( appDev ), Port( miniDev ), miniAppUseDist: true );
        await using( var server = await CreateHostAsync( map, "Development" ) )
        {
            (await GetAsync( server, "/mini-app/some/route" )).ShouldBe( "THE-MINI-APP" );
            (await GetAsync( server, "/some/route" )).ShouldBe( "APP-DEV:/some/route" );
        }
        // For a run: all the client applications use their dist...
        await using( var server = await CreateHostAsync( map, "Development", ("Clients:UseDist", "true") ) )
        {
            (await GetAsync( server, "/some/route" )).ShouldBe( "THE-APP" );
            (await GetAsync( server, "/mini-app/some/route" )).ShouldBe( "THE-MINI-APP" );
        }
        // ...or the MiniApp uses its dev server.
        await using( var server = await CreateHostAsync( map, "Development", ("Clients:MiniApp:UseDist", "false") ) )
        {
            (await GetAsync( server, "/mini-app/some/route" )).ShouldBe( "MINI-DEV:/mini-app/some/route" );
        }
    }
}
