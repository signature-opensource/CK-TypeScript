using CK.Setup;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.TypeScriptFileAttr
{
    [TypeScriptPackage]
    [TypeScriptFile( "TypeScriptFileAttr/Res/IAmHere.ts", "IAmHere" )]
    [ImportTypeScriptLibrary( "someLibDep", "^1.1.1", DependencyKind.Dependency, ForceUse = true )]
    public sealed class Embedded : TypeScriptPackage { }

    [TypeScriptPackage]
    [ImportTypeScriptLibrary( "someLibDep", "^1.0.0", DependencyKind.Dependency, ForceUse = true )]
    [ImportTypeScriptLibrary( "someLibDevDep", "^2.0.0", DependencyKind.DevDependency, ForceUse = true )]
    [ImportTypeScriptLibrary( "someLibPeer", "^3.0.0", DependencyKind.PeerDependency, ForceUse = true )]
    [TypeScriptFile( "TypeScriptFileAttr/Res/IAmAlsoHere.ts", "IAmAlsoHere", "IWantToBeHereToo" )]
    public sealed class OtherEmbedded : TypeScriptPackage { }


    [TestFixture]
    public class TypeScriptFileAttributeTests
    {
        [Test]
        public void TypeScriptFile_attribute()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.RunSuccessfulEngineWithTypeScript( targetProjectPath, TestHelper.CreateTypeCollector( typeof( Embedded ) ), Type.EmptyTypes );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmHere.ts" ) )
                .Should().BeTrue();
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmAlsoHere.ts" ) )
                .Should().BeFalse();

            var barrel = File.ReadAllText( targetProjectPath.Combine( "ck-gen/src/index.ts" ) );
            barrel.Should().Contain( "export * from './CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmHere';" );
        }

        [Test]
        public void TypeScriptFile_and_ImportTypeScriptLibrary()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

            //
            // Using TSModuleSystem.CJS and EnableTSProjectReferences because we read the project.json
            // so we can check that no "es6" exists and
            // the "build" only builds the single tsconfig.json.
            //
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
            var tsAspect = Testing.TypeScriptConfigurationExtensions.EnsureTypeScriptConfigurationAspect( TestHelper, engineConfig, targetProjectPath, Type.EmptyTypes );
            tsAspect.ModuleSystem = TSModuleSystem.CJS;

            var types = TestHelper.CreateTypeCollector( typeof( Embedded ), typeof( OtherEmbedded ) );
            var r = TestHelper.RunEngine( engineConfig, types );
            r.Success.Should().BeTrue( "Engine.Run worked." );

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
                  "workspaces": [],
                  "devDependencies": {
                    "someLibDevDep": "^2",
                    "typescript": "=5.4.5"
                  },
                  "dependencies": {
                    "someLibDep": "^1.1.1",
                    "someLibPeer": "^3"
                  },
                  "peerDependencies": {
                    "someLibPeer": "^3"
                  }
                }
                """.ReplaceLineEndings() );
        }

    }

}
