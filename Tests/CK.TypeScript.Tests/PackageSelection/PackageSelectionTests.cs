using CK.Core;
using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests.PackageSelection;

public enum EnumA { A }
public enum EnumB { B }
public enum EnumC { C }

[TypeScriptPackage]
[RegisterTypeScriptType( typeof( EnumA ) )]
[Requires<PackageB>]
public sealed class PackageA : TypeScriptPackage { }

[TypeScriptPackage]
[RegisterTypeScriptType( typeof( EnumB ) )]
public sealed class PackageB : TypeScriptPackage { }

[TypeScriptPackage]
[RegisterTypeScriptType( typeof( EnumC ) )]
public sealed class PackageC : TypeScriptPackage { }

// Its library has an invalid version: registering it is an error.
[TypeScriptPackage]
[TypeScriptImportLibrary( "some-library", "not a version", DependencyKind.Dependency )]
public sealed class BadLibraryPackage : TypeScriptPackage { }

[TestFixture]
public class PackageSelectionTests
{
    static EngineConfiguration CreateConfiguration( out TypeScriptBinPathAspectConfiguration ts, NormalizedPath targetProjectPath )
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.Types.Add( typeof( PackageA ), typeof( PackageB ), typeof( PackageC ) );
        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        ts = engineConfig.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        ts.TargetProjectPath = targetProjectPath;
        ts.IntegrationMode = CKGenIntegrationMode.None;
        return engineConfig;
    }

    static string[] GeneratedEnums( NormalizedPath targetProjectPath )
    {
        var folder = targetProjectPath.Combine( "ck-gen/CK/TypeScript/Tests/PackageSelection" );
        return Directory.Exists( folder )
                ? Directory.GetFiles( folder, "Enum*.ts" ).Select( p => Path.GetFileNameWithoutExtension( p ) ).Order().ToArray()
                : [];
    }

    [Test]
    public async Task a_configuration_selects_the_closure_of_its_listed_packages_Async()
    {
        var root = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
        var all = TestHelper.CleanupFolder( root.AppendPart( "all" ), false );
        var listed = TestHelper.CleanupFolder( root.AppendPart( "listed" ), false );

        var engineConfig = CreateConfiguration( out var ts, all );
        // The second configuration lists PackageA: it gets PackageA and PackageB (required by PackageA).
        var tsListed = new TypeScriptBinPathAspectConfiguration
        {
            TargetProjectPath = listed,
            IntegrationMode = CKGenIntegrationMode.None,
            TypeFilterName = "TypeScriptListed"
        };
        tsListed.Types.Add( typeof( PackageA ), null );
        ts.AddOtherConfiguration( tsListed );
        await engineConfig.RunSuccessfullyAsync();

        // Without any listed package, all the (non optional) packages are selected.
        GeneratedEnums( all ).ShouldBe( ["EnumA", "EnumB", "EnumC"] );
        GeneratedEnums( listed ).ShouldBe( ["EnumA", "EnumB"] );
    }

    [Test]
    public async Task an_excluded_package_is_not_selected_Async()
    {
        var targetProjectPath = TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false );
        var engineConfig = CreateConfiguration( out var ts, targetProjectPath );
        ts.ExcludedTypes.Add( typeof( PackageC ) );
        await engineConfig.RunSuccessfullyAsync();

        GeneratedEnums( targetProjectPath ).ShouldBe( ["EnumA", "EnumB"] );
    }

    [Test]
    public async Task excluding_a_required_package_is_an_error_Async()
    {
        var engineConfig = CreateConfiguration( out var ts, TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath() );
        ts.ExcludedTypes.Add( typeof( PackageB ) );
        await engineConfig.GetFailedAutomaticServicesAsync(
            "'CK.TypeScript.Tests.PackageSelection.PackageA' references 'CK.TypeScript.Tests.PackageSelection.PackageB' (Requires) that is not a candidate package." );
    }

    [TestCase( "Selected" )]
    [TestCase( "NotListed" )]
    [TestCase( "Excluded" )]
    public async Task an_unselected_package_does_not_import_its_libraries_Async( string mode )
    {
        var targetProjectPath = TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false );
        var engineConfig = CreateConfiguration( out var ts, targetProjectPath );
        engineConfig.FirstBinPath.Types.Add( typeof( BadLibraryPackage ) );
        switch( mode )
        {
            case "Selected":
                await engineConfig.GetFailedAutomaticServicesAsync( "not a version" );
                return;
            case "NotListed": ts.Types.Add( typeof( PackageA ), null ); break;
            case "Excluded": ts.ExcludedTypes.Add( typeof( BadLibraryPackage ) ); break;
        }
        await engineConfig.RunSuccessfullyAsync();
        GeneratedEnums( targetProjectPath ).ShouldBe( mode == "NotListed" ? ["EnumA", "EnumB"] : ["EnumA", "EnumB", "EnumC"] );
    }
}
