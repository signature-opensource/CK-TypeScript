using CK.Core;
using CK.Setup;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Tests;

[TestFixture]
public class TypeScriptAppBuilderContributorTests
{
    static (AppBuilderContext Context, AppIdentityAppBuilderContributor AppIdentity) Contribute( string binPathJson )
    {
        var engine = new EngineConfiguration();
        var section = ImmutableConfigurationSection.CreateFromJson( "ckonf:App:BinPaths", "Front", binPathJson );
        var binPath = new AppBinPath( "Front", TestHelper.TestProjectFolder, TestHelper.TestProjectFolder, section, engine.FirstBinPath );
        var context = new AppBuilderContext( engine, [binPath] );
        var appIdentity = new AppIdentityAppBuilderContributor();
        appIdentity.Contribute( TestHelper.Monitor, context ).ShouldBeTrue();
        new TypeScriptAppBuilderContributor( appIdentity ).Contribute( TestHelper.Monitor, context ).ShouldBeTrue();
        return (context, appIdentity);
    }

    [Test]
    public void the_TypeScript_clients_declared_by_the_party_configure_the_aspect()
    {
        var (context, appIdentity) = Contribute( """
            {
              "CK-AppIdentity": {
                "FullName": "Sample/$Front",
                "Local": {
                  "PartyClient": {
                    "App": { "Type": "TypeScript", "Kind": "DefaultApp", "DevServerPort": 4200 },
                    "MiniApp": {
                      "Type": "TypeScript", "Kind": "App", "DevServerPort": 4201, "UseDist": true, "ActiveCultures": [ "en", "fr" ],
                      "Types": [ "CK.TypeScript.Tests.TypeScriptAppBuilderContributorTests, CK.TypeScript.Tests" ],
                      "StaticFiles": { "AssetsMaxAge": "01:00:00" }
                    },
                    "Tool": { "Type": "Local" }
                  }
                }
              }
            }
            """ );
        context.EngineConfiguration.FindAspect<TypeScriptAspectConfiguration>().ShouldNotBeNull();
        var head = context.EngineConfiguration.FirstBinPath.FindAspect<TypeScriptBinPathAspectConfiguration>().ShouldNotBeNull();
        var all = head.AllConfigurations.Cast<TypeScriptBinPathAspectConfiguration>().ToDictionary( c => c.Name! );
        all.Keys.ShouldBe( ["App", "MiniApp"], ignoreOrder: true );
        all["App"].Kind.ShouldBe( ClientKind.DefaultApp );
        all["App"].DevServerPort.ShouldBe( 4200 );
        all["MiniApp"].Kind.ShouldBe( ClientKind.App );
        all["MiniApp"].UseDist.ShouldBeTrue();
        all["MiniApp"].ActiveCultures.Select( c => c.Name ).ShouldBe( ["en", "fr"], ignoreOrder: true );
        all["MiniApp"].Types.Keys.ShouldBe( [typeof( TypeScriptAppBuilderContributorTests )] );

        // The TypeScript clients are handled, not the "Local" one.
        appIdentity.Clients.Where( c => c.IsHandled ).Select( c => c.Name ).ShouldBe( ["App", "MiniApp"], ignoreOrder: true );
        // The compile time keys: the "Type" (AppIdentity) and the aspect's keys; not the options of the client.
        var keys = context.BinPaths[0].CompileKeys;
        keys.ShouldContain( "CK-AppIdentity:Local:PartyClient:MiniApp:DevServerPort" );
        keys.ShouldContain( "CK-AppIdentity:Local:PartyClient:MiniApp:Types" );
        keys.ShouldContain( "CK-AppIdentity:Local:PartyClient:MiniApp:Type" );
        keys.ShouldNotContain( "CK-AppIdentity:Local:PartyClient:MiniApp:StaticFiles" );
    }

    [Test]
    public void without_TypeScript_client_there_is_no_aspect()
    {
        var (context, _) = Contribute( """{ "CK-AppIdentity": { "FullName": "Sample/$Front" } }""" );
        context.EngineConfiguration.FindAspect<TypeScriptAspectConfiguration>().ShouldBeNull();
    }
}
