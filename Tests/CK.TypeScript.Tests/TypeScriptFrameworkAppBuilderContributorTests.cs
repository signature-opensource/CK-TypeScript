using CK.AppIdentity;
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

    // What a framework package declares (CK.TS.Angular for "NgApp"): its client feature and its compile time model.
    [PartyClientFeature( "NgApp", TypeActivation.Optional )]
    [CompileConfiguration<TypeScriptClientCompileConfiguration>]
    public sealed class FakeNgAppClientFeature : ClientFeature
    {
        public FakeNgAppClientFeature( PartyClient client, ImmutableConfigurationSection configuration ) : base( client, configuration ) { }
    }

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
        app.ConfigurableTypes = [typeof( FakeNgAppClientFeature )];
        var context = new AppBuilderContext( engine, [app] );
        var appIdentity = new AppIdentityAppBuilderContributor();
        var success = appIdentity.Contribute( TestHelper.Monitor, context );
        success &= new TestFrameworkContributor( appIdentity ).Contribute( TestHelper.Monitor, context );
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
                  "App": { "NgApp": { "RequestPath": "/", "DevServerPort": 4200 } },
                  "MiniApp": {
                    "Types": [ "CK.TypeScript.Tests.TypeScriptFrameworkAppBuilderContributorTests, CK.TypeScript.Tests" ],
                    "NgApp": {
                      "ActiveCultures": [ "en", "fr" ],
                      "DevServerPort": 4201,
                      "Types": [ { "Name": "CK.TypeScript.Tests.TypeScriptFrameworkAppBuilderContributorTests+Configured, CK.TypeScript.Tests", "TypeName": "Renamed" } ]
                    },
                    "Hosting": { "UseDist": true, "StaticFiles": { "AssetsMaxAge": "01:00:00" } }
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
        all["MiniApp"].DevServerPort.ShouldBe( 4201 );
        all["MiniApp"].ActiveCultures.Select( c => c.Name ).ShouldBe( ["en", "fr"], ignoreOrder: true );
        // The type set of the client is read by the TypeScript engine from the computed set: the TypeScript settings of
        // its types are the "Types" of the framework's model.
        all["MiniApp"].Types.Keys.ShouldBe( [typeof( Configured )] );
        all["MiniApp"].Types[typeof( Configured )].ShouldNotBeNull().TypeName.ShouldBe( "Renamed" );

        // The NgApp clients are handled, not the "Native" one.
        appIdentity.Clients.Where( c => c.IsHandled ).Select( c => c.Name ).ShouldBe( ["App", "MiniApp"], ignoreOrder: true );
        // The compile time keys: the type set and the keys of the framework's compile time model (the AppIdentity
        // contributor declares them); not the run time keys of "Hosting".
        var app = context.Apps[0];
        app.CompileKeys.ShouldBe( ["Local:PartyClient:MiniApp:Types",
                                   "Local:PartyClient:App:NgApp:RequestPath",
                                   "Local:PartyClient:App:NgApp:DevServerPort",
                                   "Local:PartyClient:MiniApp:NgApp:ActiveCultures",
                                   "Local:PartyClient:MiniApp:NgApp:DevServerPort",
                                   "Local:PartyClient:MiniApp:NgApp:Types"], ignoreOrder: true );
        // The features stay declared in the projection.
        app.DeclarationKeys.ShouldBe( ["Local:PartyClient:App:NgApp", "Local:PartyClient:MiniApp:NgApp"], ignoreOrder: true );
    }

    [TestCase( """{ "NgApp": { "Kind": "App" } }""", "Unknown key 'ckonf:Apps:Front:Local:PartyClient:App:NgApp:Kind'" )]
    [TestCase( """{ "NgApp": "yes" }""", "a feature is declared by true or by its section" )]
    [TestCase( """{ "NgApp": { "RequestPath": "app" } }""", "must start with a '/'." )]
    [TestCase( """{ "NgApp": { "DevServerPort": "many" } }""", "DevServerPort" )]
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
    public void a_client_that_its_party_does_not_serve_is_built()
    {
        var (context, _, success) = Contribute( """{ "FullName": "Sample/$Front", "Local": { "PartyClient": { "App": { "NgApp": true } } } }""" );
        success.ShouldBeTrue();
        var head = context.EngineConfiguration.FirstBinPath.FindAspect<TypeScriptBinPathAspectConfiguration>().ShouldNotBeNull();
        head.AllConfigurations.Cast<TypeScriptBinPathAspectConfiguration>().ShouldHaveSingleItem().RequestPath.ShouldBe( "/app" );
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
                    "App": { "NgApp": { "RequestPath": "/mini-app", "DevServerPort": 4200 } },
                    "MiniApp": { "NgApp": { "DevServerPort": 4201 } } } }
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
