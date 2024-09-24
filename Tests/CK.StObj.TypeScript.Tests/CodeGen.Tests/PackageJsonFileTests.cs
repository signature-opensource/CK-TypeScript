using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CSemVer;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

namespace CK.StObj.TypeScript.Tests;

[TestFixture]
public class PackageJsonFileTests
{
    [Test]
    public void parsing_empty_package_json()
    {
        var p = PackageJsonFile.Parse( TestHelper.Monitor, "{}", "/A_B/package.json", ignoreVersionsBound: true );
        Throw.DebugAssert( p != null );
        p.FilePath.Should().Be( "/A_B/package.json" );
        p.SafeName.Should().Be( "a_b" );
        p.Name.Should().BeNull();
        p.Dependencies.Should().BeEmpty();
        p.Scripts.Should().BeEmpty();
        p.Workspaces.Should().BeNull();
        p.Version.Should().BeNull();
        p.Module.Should().BeNull();
        p.Main.Should().BeNull();
        p.Private.Should().BeNull();
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

        var p = PackageJsonFile.Parse( TestHelper.Monitor, c, "some://path/package.json", ignoreVersionsBound: true );
        Throw.DebugAssert( p != null );
        p.FilePath.Should().Be( "some://path/package.json" );

        p.Name.Should().Be( "basic-tests" );
        p.SafeName.Should().Be( "basic-tests" );
        Throw.DebugAssert( p.Version != null );
        p.Version.ToString().Should().Be( "1.1.25-alpha" );
        p.Module.Should().Be( "./dist/es6" );
        p.Main.Should().Be( "./dist/cjs" );
        p.Private.Should().BeTrue();

        p.Dependencies.Should().HaveCount( 10 );
        p.Scripts.Should().HaveCount( 1 );
        p.Workspaces.Should().HaveCount( 1 );

        var reformatted = p.WriteAsString( peerDependenciesAsDepencies: false );
        reformatted.Should().Be( """
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
        p.Dependencies.Remove( "jest" ).Should().BeTrue();
        p.Dependencies.Remove( "ts-jest" ).Should().BeTrue();
        p.Dependencies.Remove( "@types/jest" ).Should().BeTrue();
        p.Dependencies.Remove( "@types/luxon" ).Should().BeTrue();
        p.Dependencies.Remove( "luxon" ).Should().BeTrue();
        p.EnsureWorkspace( "new-w" ).Should().BeTrue();

        p.Dependencies.AddOrUpdate( TestHelper.Monitor, new PackageDependency( "HelloPeer", SVersionBound.All, CK.TypeScript.CodeGen.DependencyKind.PeerDependency ) );

        p.Name = "new-name";
        p.Version = null;
        p.Main = "./dist/o/cjs";
        p.Module = null;

        p.WriteAsString().Should().Be( """
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
                "decimal.js-light": ">=2.5.1",
                "HelloPeer": "^0.0.0-0",
                "some-peer": ">=1.2.3"
              },
              "devDependencies": {
                "@local/ck-gen": "workspace:*",
                "@types/node": "^20.14.2",
                "typescript": ">=5.4.5"
              },
              "peerDependencies": {
                "HelloPeer": "^0.0.0-0",
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
