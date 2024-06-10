using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.TypeScriptFileAttr
{
    [TypeScriptFile( "IAmHere.ts", "IAmHere" )]
    public class Embedded { }

    [ImportTypeScriptLibrary( "someLibDep", "^1.0.0", DependencyKind.Dependency )]
    [ImportTypeScriptLibrary( "someLibDevDep", "^2.0.0", DependencyKind.DevDependency )]
    [ImportTypeScriptLibrary( "someLibPeer", "^3.0.0", DependencyKind.PeerDependency )]
    [TypeScriptFile( "IAmAlsoHere.ts", "IAmAlsoHere", "IWantToBeHereToo" )]
    public static class OtherEmbedded { }


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
            var c = TestHelper.CreateTypeCollector( typeof( Embedded ), typeof( OtherEmbedded ) );
            TestHelper.RunSuccessfulEngineWithTypeScript( targetProjectPath, c, Type.EmptyTypes );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmHere.ts" ) )
                .Should().BeTrue();
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmAlsoHere.ts" ) )
                .Should().BeTrue();

            var barrel = File.ReadAllText( targetProjectPath.Combine( "ck-gen/src/index.ts" ) );
            barrel.Should().Contain( "export * from './CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmHere';" )
                       .And.Contain( "export * from './CK/StObj/TypeScript/Tests/TypeScriptFileAttr/IAmAlsoHere';" );

            var package = File.ReadAllText( targetProjectPath.Combine( "ck-gen/package.json" ) );
            package.ReplaceLineEndings().Should().Be( """
                {
                  "name": "@local/ck-gen",
                  "dependencies": {
                    "someLibDep": "^1.0.0"
                  },
                  "devDependencies": {
                    "someLibDevDep": "^2.0.0",
                    "typescript": "5.4.5"
                  },
                  "peerDependencies": {
                    "someLibPeer": "^3.0.0"
                  },
                  "private": true,
                  "files": [
                    "dist/"
                  ],
                  "main": "./dist/cjs/index.js",
                  "module": "./dist/esm/index.js",
                  "scripts": {
                    "build": "tsc -p tsconfig.json && tsc -p tsconfig-cjs.json"
                  }
                }
                """.ReplaceLineEndings() );
        }

    }

}
