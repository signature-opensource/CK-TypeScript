using CK.Core;
using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

[TestFixture]
public class ClientConfigurationTests
{
    [Test]
    public void the_Name_drives_the_TypeFilterName_and_the_RequestPath()
    {
        var c = new TypeScriptBinPathAspectConfiguration();
        c.TypeFilterName.ShouldBe( "TypeScript" );
        c.RequestPath.ShouldBeNull();

        c.Name = "MiniApp";
        c.TypeFilterName.ShouldBe( "TypeScriptMiniApp" );
        // Kind is None: not served.
        c.RequestPath.ShouldBeNull();
        c.Kind = ClientKind.App;
        c.RequestPath.ShouldBe( "/mini-app" );
        c.Kind = ClientKind.DefaultApp;
        c.RequestPath.ShouldBe( "/" );

        // An explicit TypeFilterName wins.
        c.TypeFilterName = "TypeScriptOther";
        c.TypeFilterName.ShouldBe( "TypeScriptOther" );
    }

    [Test]
    public async Task a_named_configuration_targets_Clients_Name_Async()
    {
        var projectPath = TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false );
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.ProjectPath = projectPath;
        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        var ts = engineConfig.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        ts.Name = "MiniApp";
        ts.Kind = ClientKind.App;
        ts.IntegrationMode = CKGenIntegrationMode.None;
        await engineConfig.RunSuccessfullyAsync();

        ts.TargetProjectPath.ShouldBe( projectPath.Combine( "Clients/MiniApp" ) );
        Directory.Exists( projectPath.Combine( "Clients/MiniApp/ck-gen" ) ).ShouldBeTrue();
    }

    static EngineConfiguration CreateTwoConfigurations( TypeScriptBinPathAspectConfiguration c1, TypeScriptBinPathAspectConfiguration c2 )
    {
        var root = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        c1.TargetProjectPath = root.AppendPart( "c1" );
        c1.IntegrationMode = CKGenIntegrationMode.None;
        c2.TargetProjectPath = root.AppendPart( "c2" );
        c2.IntegrationMode = CKGenIntegrationMode.None;
        engineConfig.FirstBinPath.AddAspect( c1 );
        c1.AddOtherConfiguration( c2 );
        return engineConfig;
    }

    [Test]
    public async Task a_client_application_requires_a_Name_Async()
    {
        var engineConfig = CreateTwoConfigurations( new TypeScriptBinPathAspectConfiguration { Kind = ClientKind.App },
                                                    new TypeScriptBinPathAspectConfiguration { Name = "Other" } );
        await engineConfig.GetFailedAutomaticServicesAsync( "is a client application (Kind=\"App\"): it requires a Name." );
    }

    [Test]
    public async Task the_Name_must_be_an_identifier_Async()
    {
        var engineConfig = CreateTwoConfigurations( new TypeScriptBinPathAspectConfiguration { Name = "Mini-App" },
                                                    new TypeScriptBinPathAspectConfiguration { Name = "Other" } );
        await engineConfig.GetFailedAutomaticServicesAsync( "Invalid TypeScript configuration Name 'Mini-App': it must contain only letters and digits and start with a letter." );
    }

    [Test]
    public async Task there_is_at_most_one_default_client_application_Async()
    {
        var engineConfig = CreateTwoConfigurations( new TypeScriptBinPathAspectConfiguration { Name = "App", Kind = ClientKind.DefaultApp },
                                                    new TypeScriptBinPathAspectConfiguration { Name = "Other", Kind = ClientKind.DefaultApp } );
        await engineConfig.GetFailedAutomaticServicesAsync( "BinPath 'First' has more than one default client application: 'App', 'Other'." );
    }

    [Test]
    public async Task the_names_are_unique_Async()
    {
        var engineConfig = CreateTwoConfigurations( new TypeScriptBinPathAspectConfiguration { Name = "App" },
                                                    new TypeScriptBinPathAspectConfiguration { Name = "APP", TypeFilterName = "TypeScriptOther" } );
        await engineConfig.GetFailedAutomaticServicesAsync( "TypeScript configuration Name \"App\" appears more than once in BinPath 'First'." );
    }
}
