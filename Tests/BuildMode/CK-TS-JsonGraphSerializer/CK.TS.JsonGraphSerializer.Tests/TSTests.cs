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
        var ts1 = new TypeScriptBinPathAspectConfiguration()
        {
            TargetProjectPath = "Clients/C1",
            TypeFilterName = "TypeScriptC1",
        };
        ts1.OldTypes.Add( new TypeScriptTypeConfiguration( typeof( ISamplePoco ) ) );

        var ts2 = new TypeScriptBinPathAspectConfiguration()
        {
            TargetProjectPath = "Clients/C2",
            IntegrationMode = CKGenIntegrationMode.Inline,
            TypeFilterName = "TypeScriptC2",
        };
        ts2.OldTypes.Add( new TypeScriptTypeConfiguration( typeof( ISamplePoco ) ) );

        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        binPath.AddAspect( ts1 );
        ts1.AddOtherConfiguration( ts2 );

        await engineConfig.RunSuccessfullyAsync();

        // Runs the Jest tests.
        var t1 = TestHelper.TestProjectFolder.Combine( "Clients/C1" );
        await using var r1 = TestHelper.CreateTypeScriptRunner( t1 );
        r1.Run();

        var t2 = TestHelper.TestProjectFolder.Combine( "Clients/C2" );
        await using var r2 = TestHelper.CreateTypeScriptRunner( t2 );
        r2.Run();
    }

}
