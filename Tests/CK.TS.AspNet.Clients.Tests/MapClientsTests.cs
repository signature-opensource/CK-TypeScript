using CK.Core;
using CK.Setup;
using CK.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
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

    [Test]
    public async Task the_built_client_applications_are_served_under_their_request_path_Async()
    {
        var root = TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false );
        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.FirstBinPath.Types.Add( typeof( ClientDirectory ), typeof( ClientsHosting ) );
        configuration.EnsureAspect<TypeScriptAspectConfiguration>();
        var app = configuration.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        app.Name = "App";
        app.Kind = ClientKind.DefaultApp;
        app.TargetProjectPath = root.AppendPart( "App" );
        app.IntegrationMode = CKGenIntegrationMode.None;
        app.AddOtherConfiguration( new TypeScriptBinPathAspectConfiguration
        {
            Name = "MiniApp",
            Kind = ClientKind.App,
            TargetProjectPath = root.AppendPart( "MiniApp" ),
            IntegrationMode = CKGenIntegrationMode.None
        } );
        var map = (await configuration.RunSuccessfullyAsync()).LoadMap();
        CreateBuiltClient( root.AppendPart( "App" ), "App", "THE-APP" );
        CreateBuiltClient( root.AppendPart( "MiniApp" ), "MiniApp", "THE-MINI-APP" );

        // The client projects are under the content root (the test project folder): their ProjectPath is relative.
        var builder = WebApplication.CreateSlimBuilder( new WebApplicationOptions { ContentRootPath = TestHelper.TestProjectFolder } );
        await using var server = await builder.CreateRunningAspNetServerAsync( map, app => app.MapClients() );

        async Task<string> GetAsync( string path ) => await (await server.Client.GetAsync( path )).Content.ReadAsStringAsync();

        (await GetAsync( "/" )).ShouldBe( "THE-APP" );
        (await GetAsync( "/main.js" )).ShouldBe( "// App" );
        // A client route of the default application.
        (await GetAsync( "/some/route" )).ShouldBe( "THE-APP" );

        (await GetAsync( "/mini-app/" )).ShouldBe( "THE-MINI-APP" );
        (await GetAsync( "/mini-app/main.js" )).ShouldBe( "// MiniApp" );
        // A client route of the MiniApp.
        (await GetAsync( "/mini-app/some/route" )).ShouldBe( "THE-MINI-APP" );
    }

    [Test]
    public async Task in_Development_a_client_application_can_be_served_by_its_dev_server_Async()
    {
        var root = TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false );
        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.FirstBinPath.Types.Add( typeof( ClientDirectory ), typeof( ClientsHosting ) );
        configuration.EnsureAspect<TypeScriptAspectConfiguration>();
        var mini = configuration.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        mini.Name = "MiniApp";
        mini.Kind = ClientKind.App;
        mini.TargetProjectPath = root.AppendPart( "MiniApp" );
        mini.IntegrationMode = CKGenIntegrationMode.None;
        var map = (await configuration.RunSuccessfullyAsync()).LoadMap();

        // The dev server ("ng serve") answers with the path it receives.
        var devBuilder = WebApplication.CreateSlimBuilder();
        devBuilder.WebHost.UseUrls( "http://127.0.0.1:0" );
        await using var devServer = devBuilder.Build();
        devServer.MapGet( "/{**path}", ( Microsoft.AspNetCore.Http.HttpContext ctx ) => $"DEV:{ctx.Request.Path}" );
        await devServer.StartAsync();
        var devAddress = devServer.Urls.First();

        var builder = WebApplication.CreateSlimBuilder( new WebApplicationOptions
        {
            ContentRootPath = TestHelper.TestProjectFolder,
            EnvironmentName = "Development"
        } );
        builder.Configuration.AddInMemoryCollection( [new( "Clients:MiniApp:DevServer", devAddress )] );
        await using var server = await builder.CreateRunningAspNetServerAsync( map, app => app.MapClients() );

        // The host forwards what it doesn't handle under the request path: it stays the origin.
        (await (await server.Client.GetAsync( "/mini-app/some/route" )).Content.ReadAsStringAsync()).ShouldBe( "DEV:/mini-app/some/route" );
    }
}
