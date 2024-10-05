using CK.Setup;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.TypeScriptFileAttr;

[TypeScriptPackage]
[TypeScriptFile( "IAmHere.ts", typeName: "IAmHere" )]
[TypeScriptImportLibrary( "tslib", "^2.6.0", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@stdlib/utils-native-class", ">=0.0.0-0", DependencyKind.Dependency, ForceUse = true )]
public sealed class Embedded : TypeScriptPackage { }

[TypeScriptPackage]
[TypeScriptImportLibrary( "tslib", "2.7.0", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@stdlib/number-ctor", "~0.1.0", DependencyKind.DevDependency, ForceUse = true )]
[TypeScriptImportLibrary( "@stdlib/symbol-ctor", "*", DependencyKind.PeerDependency, ForceUse = true )]
[TypeScriptFile( "IAmAlsoHere.ts", "IAmAlsoHere", "IWantToBeHereToo" )]
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

    // This test uses NpmPackage integration.
    [Test( Description = "This test may fail when starting from scratch because the \"latest\" (>=0.0.0-0) @stdlib/symbol-ctor and @stdlib/utils-native-class are installed." )]
    public void TypeScriptFile_and_TypeScriptImportLibrary()
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

        // Note that a PeerDependency is also a DevDependency (otherwise nothing works: this trick makes
        // our PeerDependencies de facto transitive dependencies).

        // The ck-gen/package.json has resolved ">=0.0.0-0" dependencies.
        var ckGenPackage = File.ReadAllText( targetProjectPath.Combine( "ck-gen/package.json" ) );
        ckGenPackage.ReplaceLineEndings().Should().Be( """
            {
              "name": "@local/ck-gen",
              "private": true,
              "main": "./dist/cjs/index.js",
              "scripts": {
                "build": "tsc -p tsconfig.json"
              },
              "devDependencies": {
                "@stdlib/number-ctor": "~0.1",
                "@stdlib/symbol-ctor": "~0.2.2",
                "typescript": "=5.4.5"
              },
              "dependencies": {
                "@stdlib/utils-native-class": "~0.2.2",
                "tslib": "=2.7.0"
              },
              "peerDependencies": {
                "@stdlib/symbol-ctor": "~0.2.2"
              }
            }
            """.ReplaceLineEndings() );

        // The target package.json reproduces the ck-gen/package.json peer dependencies.
        var targetPackage = File.ReadAllText( targetProjectPath.AppendPart( "package.json" ) );
        targetPackage.ReplaceLineEndings().Should().Be( """
            {
              "name": "typescriptfile_and_typescriptimportlibrary",
              "private": true,
              "scripts": {
                "test": "jest"
              },
              "workspaces": [
                "ck-gen"
              ],
              "devDependencies": {
                "@local/ck-gen": "workspace:*",
                "@stdlib/symbol-ctor": "~0.2.2",
                "@types/jest": "^29.5.13",
                "@types/node": "^22.7.4",
                "@yarnpkg/sdks": "^3.2",
                "jest": "^29.7",
                "jest-environment-jsdom": "^29.7",
                "ts-jest": "^29.2.5",
                "typescript": "=5.4.5"
              },
              "ckVersion": 1,
              "peerDependencies": {
                "@stdlib/symbol-ctor": "~0.2.2"
              }
            }

            """.ReplaceLineEndings() );

    }

}
