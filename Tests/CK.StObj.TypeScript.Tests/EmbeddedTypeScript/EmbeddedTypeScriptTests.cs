using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.EmbeddedTypeScript
{
    [EmbeddedTypeScript( "IAmHere.ts" )]
    public class Embedded { }

    [ImportTypeScriptLibrary( "someLibDep", "^1.0.0", DependencyKind.Dependency )]
    [ImportTypeScriptLibrary( "someLibDevDep", "^2.0.0", DependencyKind.DevDependency )]
    [ImportTypeScriptLibrary( "someLibPeer", "^3.0.0", DependencyKind.PeerDependency )]
    [EmbeddedTypeScript( "IAmAlsoHere.ts" )]
    [EmbeddedTypeScript( "IAmHereToo.ts" )]
    public static class OtherEmbedded { }


    [TestFixture]
    public class EmbeddedTypeScriptTests
    {
        [Test]
        public void embedded_type_script()
        {
            // NotGeneratedByDefault is generated because it is referenced by IGeneratedByDefault.
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.RunSuccessfulEngineWithTypeScript( targetProjectPath, TestHelper.CreateTypeCollector( typeof( Embedded ) ), Type.EmptyTypes );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/EmbeddedTypeScript/IAmHere.ts" ) )
                .Should().BeTrue();
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/EmbeddedTypeScript/IAmAlsoHere.ts" ) )
                .Should().BeFalse();
        }

        [Test]
        public void embedded_type_script_and_import_library()
        {
            // NotGeneratedByDefault is generated because it is referenced by IGeneratedByDefault.
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            var c = TestHelper.CreateTypeCollector( typeof( Embedded ), typeof( OtherEmbedded ) );
            TestHelper.RunSuccessfulEngineWithTypeScript( targetProjectPath, c, Type.EmptyTypes );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/EmbeddedTypeScript/IAmHere.ts" ) )
                .Should().BeTrue();
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/EmbeddedTypeScript/IAmAlsoHere.ts" ) )
                .Should().BeTrue();
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/EmbeddedTypeScript/IAmHereToo.ts" ) )
                .Should().BeTrue();

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
