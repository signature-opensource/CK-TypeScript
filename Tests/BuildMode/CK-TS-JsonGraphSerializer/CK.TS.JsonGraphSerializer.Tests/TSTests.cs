using CK.Core;
using CK.Setup;
using CK.TypeScript;
using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.JsonGraphSerializer.Tests;

[TestFixture]
public class MultipleTypeScriptTests
{
    public interface ISamplePoco : IPoco
    {
        string Data { get; set; }
        float Value { get; set; }
    }

    [TypeScriptPackage]
    [TypeScriptImportLibrary( "axios", ">=0.0.0-0", DependencyKind.Dependency, ForceUse = true )]
    public sealed class BringAxiosPackageAsDependency : TypeScriptPackage
    {
    }

    [TypeScriptPackage]
    [TypeScriptImportLibrary( "rxjs", ">=0.0.0-0", DependencyKind.PeerDependency, ForceUse = true )]
    public sealed class BringRxJSPackageAsPeerDependency : TypeScriptPackage
    {
    }

    [Test]
    public async Task Multiple_TypeScript_Async()
    {
        // We don't need any C# backend here.
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );

        engineConfig.BasePath = TestHelper.TestProjectFolder;
        var binPath = engineConfig.FirstBinPath;
        binPath.Assemblies.Add( "CK.TS.JsonGraphSerializer" );
        binPath.Types.Add( typeof( ISamplePoco ), typeof( BringAxiosPackageAsDependency ), typeof( BringRxJSPackageAsPeerDependency ) );

        // Removed NpmPackage mode test because of versions resolution.
        //var ts1 = new TypeScriptBinPathAspectConfiguration()
        //{
        //    TargetProjectPath = "Clients/NpmPackage",
        //    IntegrationMode = CKGenIntegrationMode.NpmPackage,
        //    TypeFilterName = "TypeScriptN",
        //};
        //ts1.Types.Add( new TypeScriptTypeConfiguration( typeof( ISamplePoco ) ) );

        var ts2 = new TypeScriptBinPathAspectConfiguration()
        {
            TargetProjectPath = "Clients/Inline",
            IntegrationMode = CKGenIntegrationMode.Inline,
            TypeFilterName = "TypeScriptI",
            CKGenBuildMode = true
        };
        ts2.Types.Add( new TypeScriptTypeConfiguration( typeof( ISamplePoco ) ) );

        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        //binPath.AddAspect( ts1 );
        //ts1.AddOtherConfiguration( ts2 );
        binPath.AddAspect( ts2 );

        await engineConfig.RunSuccessfullyAsync();

        // Removed NpmPackage mode test because of versions resolution.
        //var t1 = TestHelper.TestProjectFolder.Combine( "Clients/NpmPackage" );
        //await using var r1 = TestHelper.CreateTypeScriptRunner( t1 );
        //r1.Run();

        // Runs the Jest tests.
        var t2 = TestHelper.TestProjectFolder.Combine( "Clients/Inline" );
        await using var r2 = TestHelper.CreateTypeScriptRunner( t2 );
        r2.Run();
    }

}
