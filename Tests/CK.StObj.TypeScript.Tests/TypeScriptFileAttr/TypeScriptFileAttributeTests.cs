using CK.Setup;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.TypeScriptFileAttr;

[TypeScriptPackage]
[TypeScriptFile( "TypeScriptFileAttr/Res/IAmHere.ts", "IAmHere" )]
[ImportTypeScriptLibrary( "tslib", "^2.6.0", DependencyKind.Dependency, ForceUse = true )]
public sealed class Embedded : TypeScriptPackage { }

[TypeScriptPackage]
[ImportTypeScriptLibrary( "tslib", "2.7.0", DependencyKind.Dependency, ForceUse = true )]
[ImportTypeScriptLibrary( "@stdlib/number-ctor", "^0.1.0", DependencyKind.DevDependency, ForceUse = true )]
[ImportTypeScriptLibrary( "@stdlib/symbol-ctor", "~0.2.2", DependencyKind.PeerDependency, ForceUse = true )]
[TypeScriptFile( "TypeScriptFileAttr/Res/IAmAlsoHere.ts", "IAmAlsoHere", "IWantToBeHereToo" )]
public sealed class OtherEmbedded : TypeScriptPackage { }


[TestFixture]
public class TypeScriptFileAttributeTests
{
    [Test]
    public void TypeScriptFile_attribute()
    {
        var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.FirstBinPath.Types.Add( typeof( Embedded ) );
        configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        configuration.RunSuccessfully();

        File.Exists( targetProjectPath.Combine( "ck-gen/CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmHere.ts" ) )
            .Should().BeTrue();
        File.Exists( targetProjectPath.Combine( "ck-gen/CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmAlsoHere.ts" ) )
            .Should().BeFalse();

        var barrel = File.ReadAllText( targetProjectPath.Combine( "ck-gen/index.ts" ) );
        barrel.Should().Contain( "export * from './CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmHere';" );
    }

    // This test uses NpmPackage integration mode with /src folder.
    [Test]
    public void TypeScriptFile_and_ImportTypeScriptLibrary()
    {
        var targetProjectPath = TestHelper.GetTypeScriptNpmPackageTargetProjectPath();

        //
        // Using TSModuleSystem.CJS we read the project.json
        // so we can check that the package.json has only "main": "./dist/cjs/index.js" and
        // the "build" only builds the single tsconfig.json.
        //
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
        var tsConfig = engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        tsConfig.ModuleSystem = TSModuleSystem.CJS;
        tsConfig.UseSrcFolder = true;
        engineConfig.FirstBinPath.Types.Add( typeof( Embedded ), typeof( OtherEmbedded ) );
        engineConfig.RunSuccessfully();

        File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmHere.ts" ) )
            .Should().BeTrue();
        File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmAlsoHere.ts" ) )
            .Should().BeTrue();

        var barrel = File.ReadAllText( targetProjectPath.Combine( "ck-gen/src/index.ts" ) );
        barrel.Should().Contain( "export * from './CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmHere';" )
                   .And.Contain( "export * from './CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmAlsoHere';" );

        // Note that a PeerDependency is also a regular dependency to be able to build the /ck-gen.
        var package = File.ReadAllText( targetProjectPath.Combine( "ck-gen/package.json" ) );
        package.ReplaceLineEndings().Should().Be( """
            {
              "name": "@local/ck-gen",
              "main": "./dist/cjs/index.js",
              "private": true,
              "scripts": {
                "build": "tsc -p tsconfig.json"
              },
              "devDependencies": {
                "@stdlib/number-ctor": "~0.1",
                "typescript": "=5.4.5"
              },
              "dependencies": {
                "tslib": "=2.7.0",
                "@stdlib/symbol-ctor": "~0.2.2"
              },
              "peerDependencies": {
                "@stdlib/symbol-ctor": "~0.2.2"
              }
            }
            """.ReplaceLineEndings() );
    }

}
