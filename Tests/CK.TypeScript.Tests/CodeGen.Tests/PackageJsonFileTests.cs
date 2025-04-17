using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CSemVer;
using Shouldly;
using NUnit.Framework;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

namespace CK.TypeScript.Tests;

[TestFixture]
public class PackageJsonFileTests
{
    [Test]
    public void parsing_empty_package_json()
    {
        var p = PackageJsonFile.Parse( TestHelper.Monitor, "{}", "/A_B/package.json", "Empty package.json", ignoreVersionsBound: true );
        Throw.DebugAssert( p != null );
        p.FilePath.ShouldBe( "/A_B/package.json" );
        p.SafeName.ShouldBe( "a_b" );
        p.Name.ShouldBeNull();
        p.Dependencies.ShouldBeEmpty();
        p.Scripts.ShouldBeEmpty();
        p.Workspaces.ShouldBeNull();
        p.Version.ShouldBeNull();
        p.Module.ShouldBeNull();
        p.Main.ShouldBeNull();
        p.Private.ShouldBeNull();
    }

    [Test]
    public void parsing_and_writing_package_json()
    {
        const string c = """
            {
              "name": "basic-tests",
              "private": true,
              "alien1": "OTHER",
              "version": "1.1.25-alpha",
              "alien2": { "unknown": true },
              "main": "./dist/cjs",
              "module": "./dist/es6",
              "workspaces": [ "ck-gen" ],
              "dependencies": { "decimal.js-light": ">=2.5.1", "luxon": "=3.4.4" },
              "devDependencies": { "@local/ck-gen": "workspace:*", "@types/jest": "^29.5.12", "@types/luxon": "=3.3.7",
                                   "@types/node": "^20.14.2", "jest": "^29.7.0", "ts-jest": "^29.1.4", "typescript": ">=5.4.5" },
              "peerDependencies": { "some-peer": ">=1.2.3" },
              "scripts": { "test": "jest" },
              "alien3": { "some": [ "exotic", "data" ] },
            }
            """;

        var p = PackageJsonFile.Parse( TestHelper.Monitor, c, "some://path/package.json", "Test content", ignoreVersionsBound: true );
        Throw.DebugAssert( p != null );
        p.FilePath.ShouldBe( "some://path/package.json" );

        p.Name.ShouldBe( "basic-tests" );
        p.SafeName.ShouldBe( "basic-tests" );
        Throw.DebugAssert( p.Version != null );
        p.Version.ToString().ShouldBe( "1.1.25-alpha" );
        p.Module.ShouldBe( "./dist/es6" );
        p.Main.ShouldBe( "./dist/cjs" );
        p.Private.ShouldNotBeNull().ShouldBeTrue();

        p.Dependencies.Count.ShouldBe( 10 );
        p.Dependencies.Values.All( d => d.DefinitionSource == "Test content" ).ShouldBeTrue();
        p.Scripts.Count.ShouldBe( 1 );
        p.Workspaces.ShouldHaveSingleItem();

        var reformatted = p.WriteAsString( peerDependenciesAsDepencies: false );
        reformatted.ShouldBe( """
            {
              "name": "basic-tests",
              "private": true,
              "alien1": "OTHER",
              "version": "1.1.25-alpha",
              "alien2": {
                "unknown": true
              },
              "main": "./dist/cjs",
              "module": "./dist/es6",
              "workspaces": [
                "ck-gen"
              ],
              "dependencies": {
                "decimal.js-light": ">=2.5.1",
                "luxon": "=3.4.4"
              },
              "devDependencies": {
                "@local/ck-gen": "workspace:*",
                "@types/jest": "^29.5.12",
                "@types/luxon": "=3.3.7",
                "@types/node": "^20.14.2",
                "jest": "^29.7",
                "ts-jest": "^29.1.4",
                "typescript": ">=5.4.5"
              },
              "peerDependencies": {
                "some-peer": ">=1.2.3"
              },
              "scripts": {
                "test": "jest"
              },
              "alien3": {
                "some": [
                  "exotic",
                  "data"
                ]
              }
            }
            """, "It has been reformatted." );

        p.Scripts.Add( "build", "tsc" );
        p.Dependencies.Remove( "jest" ).ShouldBeTrue();
        p.Dependencies.Remove( "ts-jest" ).ShouldBeTrue();
        p.Dependencies.Remove( "@types/jest" ).ShouldBeTrue();
        p.Dependencies.Remove( "@types/luxon" ).ShouldBeTrue();
        p.Dependencies.Remove( "luxon" ).ShouldBeTrue();
        p.EnsureWorkspace( "new-w" ).ShouldBeTrue();

        p.Dependencies.AddOrUpdate( TestHelper.Monitor, new PackageDependency( "HelloPeer", SVersionBound.All, CK.TypeScript.CodeGen.DependencyKind.PeerDependency, "Code" ) );

        p.Name = "new-name";
        p.Version = null;
        p.Main = "./dist/o/cjs";
        p.Module = null;

        p.WriteAsString().ShouldBe( """
           {
             "name": "new-name",
             "private": true,
             "alien1": "OTHER",
             "alien2": {
               "unknown": true
             },
             "main": "./dist/o/cjs",
             "workspaces": [
               "ck-gen",
               "new-w"
             ],
             "dependencies": {
               "decimal.js-light": ">=2.5.1"
             },
             "devDependencies": {
               "HelloPeer": ">=0.0.0-0",
               "@local/ck-gen": "workspace:*",
               "@types/node": "^20.14.2",
               "typescript": ">=5.4.5",
               "some-peer": ">=1.2.3"
             },
             "peerDependencies": {
               "HelloPeer": ">=0.0.0-0",
               "some-peer": ">=1.2.3"
             },
             "scripts": {
               "test": "jest",
               "build": "tsc"
             },
             "alien3": {
               "some": [
                 "exotic",
                 "data"
               ]
             }
           }
           """ );

    }


}
