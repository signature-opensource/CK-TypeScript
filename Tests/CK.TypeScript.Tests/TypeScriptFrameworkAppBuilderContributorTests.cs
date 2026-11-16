using CK.Core;
using CK.Setup;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Tests;

[TestFixture]
public class TypeScriptFrameworkAppBuilderContributorTests
{
    public sealed class Configured { }

    // What a framework package does (CK.TS.Angular for "NgApp").
    sealed class TestFrameworkContributor : TypeScriptFrameworkAppBuilderContributor
    {
        public TestFrameworkContributor( AppIdentityAppBuilderContributor appIdentity ) : base( appIdentity, "NgApp" ) { }
    }

    static (AppBuilderContext Context, AppIdentityAppBuilderContributor AppIdentity, bool Success) Contribute( string appJson )
    {
        var engine = new EngineConfiguration();
        var section = ImmutableConfigurationSection.CreateFromJson( "ckonf:Apps", "Front", appJson );
        var app = new AppConfiguration( "Front", TestHelper.TestProjectFolder, TestHelper.TestProjectFolder, section, engine.FirstBinPath );
        var context = new AppBuilderContext( engine, [app] );
        var appIdentity = new AppIdentityAppBuilderContributor();
        appIdentity.Contribute( TestHelper.Monitor, context ).ShouldBeTrue();
        var success = new TestFrameworkContributor( appIdentity ).Contribute( TestHelper.Monitor, context );
        return (context, appIdentity, success);
    }

    [Test]
    public void the_clients_of_a_TypeScript_framework_configure_the_aspect()
    {
        var (context, appIdentity, success) = Contribute( """
            {
              "FullName": "Sample/$Front",
              "Local": {
                "PartyClient": {
                  "App": { "NgApp": true, "Hosting": { "RequestPath": "/", "DevServerPort": 4200 } },
                  "MiniApp": {
                    "Types": [
                      "CK.TypeScript.Tests.TypeScriptFrameworkAppBuilderContributorTests, CK.TypeScript.Tests",
                      { "Name": "CK.TypeScript.Tests.TypeScriptFrameworkAppBuilderContributorTests+Configured, CK.TypeScript.Tests", "TypeName": "Renamed" }
                    ],
                    "NgApp": { "ActiveCultures": [ "en", "fr" ] },
                    "Hosting": { "DevServerPort": 4201, "UseDist": true, "StaticFiles": { "AssetsMaxAge": "01:00:00" } }
                  },
                  "Tool": { "Native": { "Store": "Play" } }
                }
              }
            }
            """ );
        success.ShouldBeTrue();
        context.EngineConfiguration.FindAspect<TypeScriptAspectConfiguration>().ShouldNotBeNull();
        var head = context.EngineConfiguration.FirstBinPath.FindAspect<TypeScriptBinPathAspectConfiguration>().ShouldNotBeNull();
        var all = head.AllConfigurations.Cast<TypeScriptBinPathAspectConfiguration>().ToDictionary( c => c.Name! );
        all.Keys.ShouldBe( ["App", "MiniApp"], ignoreOrder: true );
        all["App"].Kind.ShouldBe( ClientKind.DefaultApp );
        all["App"].RequestPath.ShouldBe( "/" );
        all["App"].DevServerPort.ShouldBe( 4200 );
        all["MiniApp"].Kind.ShouldBe( ClientKind.App );
        all["MiniApp"].RequestPath.ShouldBe( "/mini-app" );
        all["MiniApp"].ActiveCultures.Select( c => c.Name ).ShouldBe( ["en", "fr"], ignoreOrder: true );
        // The type set of the client is read by the TypeScript engine from the computed set: only the entries that
        // configure the TypeScript of a type are given to the aspect.
        all["MiniApp"].Types.Keys.ShouldBe( [typeof( Configured )] );
        all["MiniApp"].Types[typeof( Configured )].ShouldNotBeNull().TypeName.ShouldBe( "Renamed" );

        // The NgApp clients are handled, not the "Native" one.
        appIdentity.Clients.Where( c => c.IsHandled ).Select( c => c.Name ).ShouldBe( ["App", "MiniApp"], ignoreOrder: true );
        // The compile time keys: the type set (AppIdentity), the framework's keys, the Hosting's compile keys; not its
        // run time keys.
        var app = context.Apps[0];
        app.CompileKeys.ShouldContain( "Local:PartyClient:MiniApp:Types" );
        app.CompileKeys.ShouldContain( "Local:PartyClient:MiniApp:NgApp:ActiveCultures" );
        app.CompileKeys.ShouldContain( "Local:PartyClient:MiniApp:Hosting:DevServerPort" );
        app.CompileKeys.ShouldContain( "Local:PartyClient:App:Hosting:RequestPath" );
        app.CompileKeys.ShouldNotContain( "Local:PartyClient:MiniApp:Hosting:UseDist" );
        app.CompileKeys.ShouldNotContain( "Local:PartyClient:MiniApp:Hosting:StaticFiles" );
        // The features stay declared in the projection.
        app.DeclarationKeys.ShouldBe( ["Local:PartyClient:App:NgApp", "Local:PartyClient:App:Hosting", "Local:PartyClient:MiniApp:NgApp", "Local:PartyClient:MiniApp:Hosting"], ignoreOrder: true );
    }

    [TestCase( """{ "NgApp": true }""", "has no \"Hosting\": a NgApp client that its party doesn't serve is not supported yet." )]
    [TestCase( """{ "NgApp": { "Kind": "App" }, "Hosting": { "DevServerPort": 4200 } }""", "Unknown \"ckonf:Apps:Front:Local:PartyClient:App:NgApp:Kind\"" )]
    [TestCase( """{ "NgApp": "yes", "Hosting": { "DevServerPort": 4200 } }""", "a client feature is declared by true or by its section." )]
    [TestCase( """{ "NgApp": true, "Hosting": { "RequestPath": "app", "DevServerPort": 4200 } }""", "must start with a '/'." )]
    public void invalid_clients_are_build_errors( string client, string expected )
    {
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            var (_, _, success) = Contribute( $$"""{ "FullName": "Sample/$Front", "Local": { "PartyClient": { "App": {{client}} } } }""" );
            success.ShouldBeFalse();
            texts.ShouldContain( t => t.Contains( expected ), $"Expected: {expected}" );
        }
    }

    [Test]
    public void two_clients_cannot_have_the_same_request_path()
    {
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            var (_, _, success) = Contribute( """
                {
                  "FullName": "Sample/$Front",
                  "Local": { "PartyClient": {
                    "App": { "NgApp": true, "Hosting": { "RequestPath": "/mini-app", "DevServerPort": 4200 } },
                    "MiniApp": { "NgApp": true, "Hosting": { "DevServerPort": 4201 } } } }
                }
                """ );
            success.ShouldBeFalse();
            texts.ShouldContain( t => t.Contains( "have the same request path '/mini-app'." ) );
        }
    }

    [Test]
    public void without_TypeScript_client_there_is_no_aspect()
    {
        var (context, _, success) = Contribute( """{ "FullName": "Sample/$Front" }""" );
        success.ShouldBeTrue();
        context.EngineConfiguration.FindAspect<TypeScriptAspectConfiguration>().ShouldBeNull();
    }
}
