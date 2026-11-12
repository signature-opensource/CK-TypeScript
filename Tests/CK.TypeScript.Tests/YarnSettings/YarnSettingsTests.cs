using CK.Core;
using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests.YarnSettingsSupport;

// tslib has no dependencies: the extension adds "is-number" to it, the resolution pins it.
[TypeScriptPackage]
[TypeScriptImportLibrary( "tslib", "^2.6.0", DependencyKind.Dependency )]
[YarnPackageExtension( "tslib@*", DependencyKind.Dependency, "is-number", "^7.0.0", Reason = "Test extension." )]
[PackageResolution( "is-number", "7.0.0", Reason = "Test resolution." )]
public sealed class WithExtension : TypeScriptPackage { }

[TypeScriptPackage]
[TypeScriptImportLibrary( "tslib", "^2.6.0", DependencyKind.Dependency )]
public sealed class WithoutExtension : TypeScriptPackage { }

[TypeScriptPackage]
[YarnPackageExtension( "tslib@*", DependencyKind.DevDependency, "is-number", "*" )]
public sealed class DevExtension : TypeScriptPackage { }

[TypeScriptPackage]
[YarnPackageExtension( "tslib", DependencyKind.PeerDependency, "is-number", "*" )]
public sealed class NoRangeSelector : TypeScriptPackage { }

[TypeScriptPackage]
[YarnPackageExtension( "tslib@*", DependencyKind.PeerDependency, "is-number", "*" )]
public sealed class AnySelector : TypeScriptPackage { }

[TypeScriptPackage]
[YarnPackageExtension( "tslib@^2", DependencyKind.PeerDependency, "is-number", "*" )]
public sealed class OverlappingSelector : TypeScriptPackage { }

[TypeScriptPackage]
[PackageResolution( "is-number", "^6.0.0" )]
public sealed class Resolution6 : TypeScriptPackage { }

[TypeScriptPackage]
[PackageResolution( "is-number", "^7.0.0" )]
public sealed class Resolution7 : TypeScriptPackage { }

[TestFixture]
public class YarnSettingsTests
{
    static YarnSettings? Create( (YarnPackageExtensionAttribute, string)[] extensions, (PackageResolutionAttribute, string)[] resolutions, bool ignoreVersionsBound = false )
    {
        return YarnSettings.Create( TestHelper.Monitor, extensions, resolutions, ignoreVersionsBound );
    }

    [Test]
    public void versions_are_merged_and_the_more_specific_wins()
    {
        var s = Create( [(new YarnPackageExtensionAttribute( "a@*", DependencyKind.PeerDependency, "rxjs", "*" ), "P1"),
                         (new YarnPackageExtensionAttribute( "a@*", DependencyKind.PeerDependency, "rxjs", "^7.4.0" ), "P2"),
                         (new YarnPackageExtensionAttribute( "a@*", DependencyKind.PeerDependency, "rxjs", "*" ), "P3")],
                        [(new PackageResolutionAttribute( "rxjs", "^7.4.0" ), "P1"),
                         (new PackageResolutionAttribute( "rxjs", "7.8.1" ), "P2")] );
        Throw.DebugAssert( s != null );
        s.Extensions.Count.ShouldBe( 1 );
        s.Extensions[0].Range.Value.ShouldBe( "^7.4.0" );
        s.Extensions[0].Range.Source.ShouldBe( "P2" );
        s.Resolutions["rxjs"].Value.ShouldBe( "7.8.1" );
        s.Resolutions["rxjs"].Source.ShouldBe( "P2" );
    }

    [Test]
    public void non_version_values_are_resolved_by_the_last_declaration()
    {
        var s = Create( [],
                        [(new PackageResolutionAttribute( "rxjs", "^7.4.0" ), "P1"),
                         (new PackageResolutionAttribute( "rxjs", "npm:rxjs@7.5.0" ), "P2"),
                         (new PackageResolutionAttribute( "rxjs", "patch:rxjs@npm%3A7.5.0#./p.patch" ), "P3")] );
        Throw.DebugAssert( s != null );
        s.Resolutions["rxjs"].Value.ShouldBe( "patch:rxjs@npm%3A7.5.0#./p.patch" );
        s.Resolutions["rxjs"].Source.ShouldBe( "P3" );
    }

    [Test]
    public void incompatible_versions_are_an_error_unless_IgnoreVersionsBound()
    {
        (PackageResolutionAttribute, string)[] resolutions = [(new PackageResolutionAttribute( "rxjs", "^6.0.0" ), "P1"),
                                                              (new PackageResolutionAttribute( "rxjs", "^7.0.0" ), "P2")];
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            Create( [], resolutions ).ShouldBeNull();
            logs.ShouldContain( t => t.Contains( "incompatible versions" ) );
        }
        var s = Create( [], resolutions, ignoreVersionsBound: true );
        Throw.DebugAssert( s != null );
        s.Resolutions["rxjs"].Value.ShouldBe( "^7.0.0" );
    }

    [Test]
    public void overlapping_selectors_are_an_error()
    {
        Create( [(new YarnPackageExtensionAttribute( "a@*", DependencyKind.PeerDependency, "rxjs", "*" ), "P1"),
                 (new YarnPackageExtensionAttribute( "a@^2", DependencyKind.PeerDependency, "rxjs", "*" ), "P2")], [] )
            .ShouldBeNull();
        // Disjoint ranges, another kind or another name are fine.
        var s = Create( [(new YarnPackageExtensionAttribute( "a@^1", DependencyKind.PeerDependency, "rxjs", "*" ), "P1"),
                         (new YarnPackageExtensionAttribute( "a@^2", DependencyKind.PeerDependency, "rxjs", "*" ), "P2"),
                         (new YarnPackageExtensionAttribute( "a@*", DependencyKind.Dependency, "tslib", "*" ), "P3"),
                         (new YarnPackageExtensionAttribute( "@scope/a@*", DependencyKind.PeerDependency, "rxjs", "*" ), "P4")], [] );
        Throw.DebugAssert( s != null );
        s.Extensions.Count.ShouldBe( 4 );
    }

    [Test]
    public void the_yarnrc_packageExtensions_setting_is_owned_and_the_others_are_kept()
    {
        var s = Create( [(new YarnPackageExtensionAttribute( "ng-zorro-antd@*", DependencyKind.PeerDependency, "rxjs", "*" ) { Reason = "The reason." }, "CK.Ng.Zorro.ZorroPackage"),
                         (new YarnPackageExtensionAttribute( "ng-zorro-antd@*", DependencyKind.Dependency, "tslib", "^2" ), "Other")], [] );
        Throw.DebugAssert( s != null );
        const string block = """
            packageExtensions:
              "ng-zorro-antd@*":
                dependencies:
                  # Other
                  "tslib": "^2"
                peerDependencies:
                  # CK.Ng.Zorro.ZorroPackage: The reason.
                  "rxjs": "*"

            """;
        var nl = Environment.NewLine;
        var expectedBlock = block.ReplaceLineEndings( nl );
        // No file: the block only.
        YarnSettings.UpdateYarnRc( null, s ).ShouldBe( expectedBlock );
        // No file and no extension: no file.
        YarnSettings.UpdateYarnRc( null, null ).ShouldBeNull();

        // Settings written by Yarn (a lockfile migration) are kept.
        var yarnWritten = $"approvedGitRepositories:{nl}  - \"**\"{nl}{nl}enableScripts: true{nl}";
        var withBlock = YarnSettings.UpdateYarnRc( yarnWritten, s );
        withBlock.ShouldBe( yarnWritten + nl + expectedBlock );
        // Idempotent.
        YarnSettings.UpdateYarnRc( withBlock, s ).ShouldBe( withBlock );
        // Replaced in place, whatever its format (Yarn writes it without quotes nor comments), before a column-0 comment.
        var yarnFormat = $"packageExtensions:{nl}  ng-zorro-antd@*:{nl}    peerDependencies:{nl}      rxjs: \"*\"{nl}{nl}# Comment of the next one.{nl}enableScripts: true{nl}";
        YarnSettings.UpdateYarnRc( yarnFormat, s ).ShouldBe( expectedBlock + $"{nl}# Comment of the next one.{nl}enableScripts: true{nl}" );
        // Removed: the other settings stay.
        YarnSettings.UpdateYarnRc( withBlock, null ).ShouldBe( yarnWritten + nl );
        // Removed: nothing else, no file.
        YarnSettings.UpdateYarnRc( expectedBlock, null ).ShouldBeNull();
    }

    [Test]
    public void the_yarn_root_and_the_application_can_share_the_same_yarnrc()
    {
        // Without a git root above the application, Yarn is installed in the application's folder: its .yarnrc.yml
        // holds the yarnPath (EnsureYarnRcFileAtYarnLevel) and the packageExtensions (UpdateYarnRc).
        NormalizedPath root = TestHelper.TestProjectFolder.Combine( "Obj/YarnRootIsTheApplication" );
        TestHelper.CleanupFolder( root );
        var yarnRc = root.AppendPart( ".yarnrc.yml" );
        var yarnPath = root.Combine( ".yarn/releases/yarn-4.18.1.cjs" );
        YarnHelper.EnsureYarnRcFileAtYarnLevel( TestHelper.Monitor, yarnPath );
        var rootOnly = File.ReadAllText( yarnRc );
        rootOnly.ShouldStartWith( "yarnPath: \"./.yarn/releases/yarn-4.18.1.cjs\"" );

        var s = Create( [(new YarnPackageExtensionAttribute( "ng-zorro-antd@*", DependencyKind.PeerDependency, "rxjs", "*" ), "P")], [] );
        Throw.DebugAssert( s != null );
        var both = YarnSettings.UpdateYarnRc( rootOnly, s );
        Throw.DebugAssert( both != null );
        both.ShouldStartWith( rootOnly );
        both.ShouldContain( "\"ng-zorro-antd@*\":" );
        File.WriteAllText( yarnRc, both );

        // The yarnPath fix-up (another Yarn version) keeps the package extensions.
        YarnHelper.EnsureYarnRcFileAtYarnLevel( TestHelper.Monitor, root.Combine( ".yarn/releases/yarn-4.20.0.cjs" ) );
        var upgraded = File.ReadAllText( yarnRc );
        upgraded.ShouldStartWith( "yarnPath: \"./.yarn/releases/yarn-4.20.0.cjs\"" );
        YarnSettings.UpdateYarnRc( upgraded, s ).ShouldBe( upgraded );
        // And the yarnPath stays as is when it is right.
        YarnHelper.EnsureYarnRcFileAtYarnLevel( TestHelper.Monitor, root.Combine( ".yarn/releases/yarn-4.20.0.cjs" ) );
        File.ReadAllText( yarnRc ).ShouldBe( upgraded );

        // No more extensions: the file stays (the yarnPath remains), without the block.
        var removed = YarnSettings.UpdateYarnRc( upgraded, null );
        Throw.DebugAssert( removed != null );
        removed.ShouldNotContain( "packageExtensions" );
        removed.TrimEnd().ShouldBe( rootOnly.Replace( "yarn-4.18.1", "yarn-4.20.0" ).TrimEnd() );
    }

    // The data part of the .pnp.cjs of https://github.com/olivier-spinelli/ng-zorro-antd-bug (ng-zorro-antd 22.2.0
    // without the rxjs peer): two instances of @angular/cdk.
    static string[] DuplicatedCdkPnp() => File.ReadAllLines( TestHelper.TestProjectFolder.Combine( "YarnSettings/duplicated-cdk.pnp.cjs.txt" ) );

    static readonly string[] _reproRuntimeDependencies = [ "@angular/animations", "@angular/cdk", "@angular/common", "@angular/core",
                                                           "@angular/forms", "@angular/platform-browser", "@angular/router",
                                                           "ng-zorro-antd", "rxjs", "tslib" ];

    [Test]
    public void duplicated_instances_of_the_runtime_graph_are_found()
    {
        var duplicates = YarnPnpDuplicatedInstances.Find( DuplicatedCdkPnp(), _reproRuntimeDependencies );
        Throw.DebugAssert( duplicates != null );
        var cdk = duplicates.ShouldHaveSingleItem();
        cdk.Name.ShouldBe( "@angular/cdk" );
        cdk.Instances.Count.ShouldBe( 2 );
        // The application's instance and the one of ng-zorro-antd (without rxjs).
        cdk.Instances.ShouldContain( i => i.Reference == "npm:22.2.6 [e2df7]" && i.UsedBy.Contains( "the application" ) );
        cdk.Instances.ShouldContain( i => i.Reference == "npm:22.2.6 [d1970]" && i.UsedBy.Contains( "ng-zorro-antd@npm:22.2.0 [e2df7]" ) );
    }

    [Test]
    public void duplicated_instances_are_found_at_any_depth_of_the_runtime_graph_only()
    {
        // The application depends on ng-zorro-antd only: the CDK is reached through it (and through
        // @ant-design/icons-angular), its duplicate is still found.
        var duplicates = YarnPnpDuplicatedInstances.Find( DuplicatedCdkPnp(), ["ng-zorro-antd", "@angular/cdk"] );
        Throw.DebugAssert( duplicates != null );
        duplicates.ShouldHaveSingleItem().Name.ShouldBe( "@angular/cdk" );
        // A package outside of the runtime graph (here: only rxjs and tslib are runtime dependencies) is not checked.
        var none = YarnPnpDuplicatedInstances.Find( DuplicatedCdkPnp(), ["rxjs", "tslib"] );
        Throw.DebugAssert( none != null );
        none.ShouldBeEmpty();
    }

    [Test]
    public void an_unknown_pnp_format_is_not_checked()
    {
        YarnPnpDuplicatedInstances.Find( ["// Not a .pnp.cjs file.", "module.exports = {};"], _reproRuntimeDependencies ).ShouldBeNull();
        // The check only traces when there is no .pnp.cjs or an unknown one: it never fails.
        NormalizedPath root = TestHelper.TestProjectFolder.Combine( "Obj/YarnPnpDuplicatedInstances" );
        TestHelper.CleanupFolder( root );
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            YarnPnpDuplicatedInstances.Check( TestHelper.Monitor, root, _reproRuntimeDependencies );
            File.WriteAllText( root.AppendPart( ".pnp.cjs" ), "module.exports = {};" );
            YarnPnpDuplicatedInstances.Check( TestHelper.Monitor, root, _reproRuntimeDependencies );
            logs.ShouldContain( t => t.Contains( "No '.pnp.cjs'" ) );
            logs.ShouldContain( t => t.Contains( "is not recognized" ) );
        }
        // The real file gives the warning.
        File.WriteAllLines( root.AppendPart( ".pnp.cjs" ), DuplicatedCdkPnp() );
        using( TestHelper.Monitor.CollectEntries( out var entries, LogLevelFilter.Warn ) )
        {
            YarnPnpDuplicatedInstances.Check( TestHelper.Monitor, root, _reproRuntimeDependencies );
            entries.ShouldHaveSingleItem().Text.ShouldStartWith( "Package '@angular/cdk' is installed as 2 instances by Yarn Plug'n'Play" );
        }
    }

    [Test]
    public void the_install_marker_is_ignored_by_an_existing_yarn_gitignore()
    {
        NormalizedPath root = TestHelper.TestProjectFolder.Combine( "Obj/YarnInstallMarkerGitIgnore" );
        TestHelper.CleanupFolder( root );
        var yarnPath = root.Combine( ".yarn/releases/yarn-4.18.1.cjs" );
        Directory.CreateDirectory( yarnPath.RemoveLastPart() );
        var gitIgnore = root.AppendPart( ".gitignore" );
        File.WriteAllText( gitIgnore, "bin/\r\n.yarn/*\r\n" );

        YarnHelper.EnsureInstallMarkerIgnored( TestHelper.Monitor, root );
        File.ReadAllLines( gitIgnore ).ShouldContain( "**/.yarn/ck-install-state" );
        var once = File.ReadAllText( gitIgnore );
        YarnHelper.EnsureInstallMarkerIgnored( TestHelper.Monitor, root );
        File.ReadAllText( gitIgnore ).ShouldBe( once );
    }

    static EngineConfiguration CreateNoIntegrationConfiguration( params Type[] types )
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.Types.Add( types );
        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        var ts = engineConfig.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        ts.TargetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath( "YarnSettingsErrors" );
        ts.IntegrationMode = CKGenIntegrationMode.None;
        return engineConfig;
    }

    [TestCase( typeof( DevExtension ), "a package extension can add a Dependency or a PeerDependency only" )]
    [TestCase( typeof( NoRangeSelector ), "the selector must be a package name followed by a range" )]
    public async Task invalid_declarations_are_generation_errors_Async( Type package, string message )
    {
        await CreateNoIntegrationConfiguration( package ).GetFailedAutomaticServicesAsync( message );
    }

    [Test]
    public async Task overlapping_selectors_and_incompatible_resolutions_are_generation_errors_Async()
    {
        await CreateNoIntegrationConfiguration( typeof( AnySelector ), typeof( OverlappingSelector ) ).GetFailedAutomaticServicesAsync( "their ranges overlap" );
        await CreateNoIntegrationConfiguration( typeof( Resolution6 ), typeof( Resolution7 ) ).GetFailedAutomaticServicesAsync( "incompatible versions" );
    }

    [Test]
    public async Task package_extensions_and_resolutions_are_installed_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();
        var yarnRc = targetProjectPath.AppendPart( ".yarnrc.yml" );
        var marker = targetProjectPath.Combine( ".yarn/ck-install-state" );
        var pnp = targetProjectPath.AppendPart( ".pnp.cjs" );

        // 1 - The extension and the resolution are written and installed.
        await RunAsync( targetProjectPath, typeof( WithExtension ) );
        File.ReadAllText( yarnRc ).ShouldBe( """
            packageExtensions:
              "tslib@*":
                dependencies:
                  # CK.TypeScript.Tests.YarnSettingsSupport.WithExtension: Test extension.
                  "is-number": "^7.0.0"

            """.ReplaceLineEndings( Environment.NewLine ) );
        var packageJson = targetProjectPath.AppendPart( "package.json" );
        File.ReadAllText( packageJson ).ShouldContain( "\"is-number\": \"7.0.0\"" );
        // The "ck-watch" script doesn't depend on the build configuration.
        File.ReadAllText( packageJson ).ShouldContain( "\"ck-watch\": \"dotnet \\\"$PROJECT_CWD/../../bin/${BUILD_CONFIGURATION:-Debug}/" );
        // The dependency added by the extension is redirected by the resolution. The yarn.lock keeps the original
        // manifest of tslib: the installed dependencies are in the .pnp.cjs.
        TslibDependencies( pnp ).ShouldContain( "[\"is-number\", \"npm:7.0.0\"]" );
        File.Exists( marker ).ShouldBeTrue();
        var markerTime = File.GetLastWriteTimeUtc( marker );

        // 2 - Nothing changed: no install (the marker is not rewritten) and the package.json keeps its bytes.
        var packageJsonBytes = File.ReadAllBytes( packageJson );
        await RunAsync( targetProjectPath, typeof( WithExtension ) );
        File.GetLastWriteTimeUtc( marker ).ShouldBe( markerTime );
        File.ReadAllBytes( packageJson ).ShouldBe( packageJsonBytes );

        // 3 - The extension is no more declared: the .yarnrc.yml changes and the install runs.
        //     The resolution stays in the package.json (the engine can't tell its entries from the author's ones).
        await RunAsync( targetProjectPath, typeof( WithoutExtension ) );
        File.Exists( yarnRc ).ShouldBe( false, "The file contained only the package extensions." );
        TslibDependencies( pnp ).ShouldNotContain( "is-number" );
        File.GetLastWriteTimeUtc( marker ).ShouldNotBe( markerTime );

        // 4 - A failed (or interrupted) install leaves no marker: the next run installs.
        //     The package.json has an old "ck-watch" script (with the configuration folder) and no final newline:
        //     the script is fixed and the file ends with a newline, like the package.json that Yarn writes.
        var oldScript = File.ReadAllText( packageJson ).Replace( "${BUILD_CONFIGURATION:-Debug}", "Release" ).TrimEnd();
        File.WriteAllText( packageJson, oldScript );
        File.Delete( marker );
        await RunAsync( targetProjectPath, typeof( WithoutExtension ) );
        File.Exists( marker ).ShouldBeTrue();
        var fixedScript = File.ReadAllText( packageJson );
        fixedScript.ShouldContain( "/bin/${BUILD_CONFIGURATION:-Debug}/" );
        fixedScript.ShouldEndWith( "}" + Environment.NewLine );

        // 5 - Nothing changed: the package.json keeps its bytes.
        packageJsonBytes = File.ReadAllBytes( packageJson );
        await RunAsync( targetProjectPath, typeof( WithoutExtension ) );
        File.ReadAllBytes( packageJson ).ShouldBe( packageJsonBytes );

        // The "packageDependencies" of tslib in the .pnp.cjs.
        static string TslibDependencies( NormalizedPath pnp )
        {
            var text = File.ReadAllText( pnp );
            int start = text.IndexOf( "[\"tslib\", [" );
            start.ShouldBeGreaterThan( 0 );
            start = text.IndexOf( "\"packageDependencies\"", start );
            int end = text.IndexOf( "\"linkType\"", start );
            return text.Substring( start, end - start );
        }

        static async Task RunAsync( NormalizedPath targetProjectPath, Type package )
        {
            var configuration = TestHelper.CreateDefaultEngineConfiguration();
            configuration.FirstBinPath.Types.Add( package );
            configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, package );
            await configuration.RunSuccessfullyAsync();
        }
    }
}
