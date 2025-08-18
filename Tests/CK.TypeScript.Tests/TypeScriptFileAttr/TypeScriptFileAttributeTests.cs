using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS0618 // Type or member is obsolete

namespace CK.TypeScript.Tests.TypeScriptFileAttr;

[TypeScriptPackage]
[TypeScriptFile( "IAmHere.ts", typeName: "IAmHere", TargetFolder = "" )]
[TypeScriptImportLibrary( "tslib", "^2.6.0", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@stdlib/utils-native-class", ">=0.0.0-0", DependencyKind.Dependency )]
[TypeScriptFile( "Some.private.ts" )]
public sealed class Embedded : TypeScriptPackage { }

[TypeScriptPackage]
[TypeScriptImportLibrary( "tslib", "2.7.0", DependencyKind.Dependency )]
[TypeScriptImportLibrary( "@stdlib/number-ctor", "~0.1.0", DependencyKind.DevDependency )]
[TypeScriptImportLibrary( "@stdlib/symbol-ctor", "*", DependencyKind.PeerDependency )]
[TypeScriptFile( "IAmAlsoHere.ts", "IAmAlsoHere", "IWantToBeHereToo", TargetFolder = "" )]
public sealed class OtherEmbedded : TypeScriptPackage { }

[TypeScriptPackage]
[TypeScriptImportLibrary( "axios", "*", DependencyKind.Dependency )]
[TypeScriptFile( "HttpCrisEndpoint.ts", "HttpCrisEndpoint", TargetFolder = "" )]
public sealed class WithAxios : TypeScriptPackage { }


[TestFixture]
public class TypeScriptFileAttributeTests
{
    [Test]
    public async Task TypeScriptFile_attribute_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.FirstBinPath.Types.Add( typeof( Embedded ) );
        configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        await configuration.RunSuccessfullyAsync();

        File.Exists( targetProjectPath.Combine( "ck-gen/IAmHere.ts" ) )
            .ShouldBeTrue();
        File.Exists( targetProjectPath.Combine( "ck-gen/IAmAlsoHere.ts" ) )
            .ShouldBeFalse();

        var barrel = File.ReadAllText( targetProjectPath.Combine( "ck-gen/index.ts" ) );
        barrel.ShouldContain( "export * from './IAmHere';" );
    }


}
