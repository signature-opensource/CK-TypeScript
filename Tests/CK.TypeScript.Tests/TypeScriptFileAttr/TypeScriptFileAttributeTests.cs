using CK.Setup;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests.TypeScriptFileAttr;

[TypeScriptPackage( ConsiderExplicitResourceOnly = true )]
[TypeScriptFile( "IAmHere.ts", typeName: "IAmHere", TargetFolder = "" )]
[TypeScriptImportLibrary( "tslib", "^2.6.0", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@stdlib/utils-native-class", ">=0.0.0-0", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptFile( "Some.private.ts" )]
public sealed class Embedded : TypeScriptPackage { }

[TypeScriptPackage( ConsiderExplicitResourceOnly = true )]
[TypeScriptImportLibrary( "tslib", "2.7.0", DependencyKind.Dependency, ForceUse = true )]
[TypeScriptImportLibrary( "@stdlib/number-ctor", "~0.1.0", DependencyKind.DevDependency, ForceUse = true )]
[TypeScriptImportLibrary( "@stdlib/symbol-ctor", "*", DependencyKind.PeerDependency, ForceUse = true )]
[TypeScriptFile( "IAmAlsoHere.ts", "IAmAlsoHere", "IWantToBeHereToo", TargetFolder = "" )]
public sealed class OtherEmbedded : TypeScriptPackage { }

[TypeScriptPackage( ConsiderExplicitResourceOnly = true )]
[TypeScriptImportLibrary( "axios", "*", DependencyKind.Dependency, ForceUse = true )]
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

    // This test uses NpmPackage integration.
    [Test( Description = "This test may fail when starting from scratch because the \"latest\" (>=0.0.0-0) libraries are installed." )]
    [Explicit( "NpmPackage mode versions resolution doesn't work as expected. NpmPackage mode should no longer be used anyway." )]
    public async Task TypeScriptFile_and_TypeScriptImportLibrary_Async()
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
        // tsConfig.UseSrcFolder = true NpmPackage => UseSrcFolder = true (a warinig is emitted).
        engineConfig.FirstBinPath.Types.Add( typeof( Embedded ), typeof( OtherEmbedded ), typeof( WithAxios ) );
        await engineConfig.RunSuccessfullyAsync();

        File.Exists( targetProjectPath.Combine( "ck-gen/src/IAmHere.ts" ) )
            .ShouldBeTrue();
        File.Exists( targetProjectPath.Combine( "ck-gen/src/IAmAlsoHere.ts" ) )
            .ShouldBeTrue();

        var barrel = File.ReadAllText( targetProjectPath.Combine( "ck-gen/src/index.ts" ) );
        barrel.ShouldContain( "export * from './IAmHere';" );
        barrel.ShouldContain( "export * from './IAmAlsoHere';" );
        barrel.ShouldNotContain( "private" );

        // Note that a PeerDependency is also a DevDependency (otherwise nothing works: this trick makes
        // our PeerDependencies de facto transitive dependencies).

        // The ck-gen/package.json has resolved ">=0.0.0-0" dependencies.
        var ckGenPackage = File.ReadAllText( targetProjectPath.Combine( "ck-gen/package.json" ) );
        ckGenPackage.ReplaceLineEndings().ShouldBe( """
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
                "axios": "^1.8.4",
                "tslib": "=2.7.0"
              },
              "peerDependencies": {
                "@stdlib/symbol-ctor": "~0.2.2"
              }
            }
            """.ReplaceLineEndings() );

        // The target package.json reproduces the ck-gen/package.json peer dependencies.
        var targetPackage = File.ReadAllText( targetProjectPath.AppendPart( "package.json" ) );
        targetPackage.ReplaceLineEndings().ShouldBe( """
            {
              "name": "typescriptfile_and_typescriptimportlibrary",
              "private": true,
              "packageManager": "yarn@4.6.0",
              "scripts": {
                "test": "jest"
              },
              "workspaces": [
                "ck-gen"
              ],
              "devDependencies": {
                "@local/ck-gen": "workspace:*",
                "@stdlib/symbol-ctor": "~0.2.2",
                "@types/jest": "^29.5.14",
                "@types/node": "^22.10.9",
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
