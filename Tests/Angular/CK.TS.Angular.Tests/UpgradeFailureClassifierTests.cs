using CK.TS.Angular.Engine;
using CK.TypeScript.CodeGen;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class UpgradeFailureClassifierTests
{
    static readonly HashSet<string> _updated22 = ["@angular/core", "@angular/cli", "ng-zorro-antd", "@angular/cdk", "@fortawesome/angular-fontawesome"];

    static UpgradeFailure Classify( UpgradeCommandKind command, int exitCode, params string[] lines )
    {
        return UpgradeFailureClassifier.Classify( command, exitCode, false, lines );
    }

    static string Hint( UpgradeFailure failure, string? declarer = null )
    {
        return UpgradeFailureClassifier.DescribeHint( failure, 22, _updated22, _ => declarer );
    }

    // failure-surface exploration, Q1 (CLI 20.x, 21.x, 22.0.x summary as printed).
    static readonly string[] _cli20PeerRefusal =
    [
        "    Package \"@ngx-translate/core\" has an incompatible peer dependency to \"@angular/core\" (requires \"^19.0.0\" (extended), would install \"21.0.0\").",
        "    Package \"@angular/core\" has an incompatible peer dependency to \"rxjs\" (requires \"^6.5.3 || ^7.4.0\", would install \"6.4.0\")",
        "✖ Migration failed: Incompatible peer dependencies found.",
        "Peer dependency warnings when installing dependencies means that those dependencies might not work correctly together.",
        "You can use the '--force' option to ignore incompatible peer dependencies and instead address these warnings later.",
        "  See \"/tmp/ng-Ab12Cd/angular-errors.log\" for further details."
    ];

    // failure-surface exploration, Q1 (reverse check, CLI 21 at the step to 22).
    static readonly string[] _libraryPeerRefusal =
    [
        "    Package \"@ngx-translate/core\" has an incompatible peer dependency to \"@angular/core\" (requires \"^20.0.0\" (extended), would install \"22.2.3\").",
        "✖ Migration failed: Incompatible peer dependencies found.",
    ];

    [Test]
    public void timeout_wins_over_any_output()
    {
        var failure = UpgradeFailureClassifier.Classify( UpgradeCommandKind.PackageCommand, 0, true, _cli20PeerRefusal );
        failure.Kind.ShouldBe( UpgradeFailureKind.Timeout );
        UpgradeFailureClassifier.CommandTimeout.ShouldBe( TimeSpan.FromMinutes( 10 ) );
        Hint( failure ).ShouldContain( "10 minutes" );
    }

    [Test]
    public void exit_0_is_none_whatever_the_output()
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, 0, _cli20PeerRefusal );
        failure.Kind.ShouldBe( UpgradeFailureKind.None );
        Hint( failure ).ShouldBeEmpty();
    }

    // failure-surface exploration, Q8: DNS failure with YARN_HTTP_RETRY=0, proxy unreachable,
    // and the same DNS failure with the default httpRetry.
    [TestCase( "➤ YN0001: │ RequestError: getaddrinfo ENOTFOUND registry.yarnpkg.com" )]
    [TestCase( "➤ YN0001: │ RequestError: tunneling socket could not be established, cause=connect ECONNREFUSED 127.0.0.1:3128" )]
    [TestCase( "➤ YN0001: │ Error [ERR_SOCKET_CLOSED_BEFORE_CONNECTION]: Socket closed before the connection was established" )]
    public void yarn_request_error_is_registry_unreachable( string line )
    {
        var failure = Classify( UpgradeCommandKind.YarnInstall, 1, line, "    at ClientRequest.<anonymous>" );
        failure.Kind.ShouldBe( UpgradeFailureKind.RegistryUnreachable );
        Hint( failure ).ShouldContain( "cannot reach the npm registry" );
    }

    [Test]
    public void yarn_404_is_package_not_found()
    {
        // failure-surface exploration, Q8 (HTTP 404).
        var failure = Classify( UpgradeCommandKind.YarnInstall,
                                1,
                                "➤ YN0035: │ @angular/core@npm:99.0.0: Package not found",
                                "➤ YN0035: │   Response Code: 404 (Not Found)",
                                "➤ YN0035: │   Request Method: GET",
                                "➤ YN0035: │   Request URL: https://registry.yarnpkg.com/@angular%2fcore" );
        failure.Kind.ShouldBe( UpgradeFailureKind.PackageNotFound );
        failure.Details.ShouldBe( ["@angular/core@npm:99.0.0: Package not found"] );
        Hint( failure ).ShouldContain( "@angular/core@npm:99.0.0" );
    }

    [Test]
    public void yarn_401_is_authentication()
    {
        // failure-surface exploration, Q8 (HTTP 401, observed against npm.fontawesome.com).
        var failure = Classify( UpgradeCommandKind.YarnInstall,
                                1,
                                "➤ YN0041: │ @fortawesome/pro-solid-svg-icons@npm:^6.0.0: Invalid authentication (as an anonymous user)" );
        failure.Kind.ShouldBe( UpgradeFailureKind.Authentication );
        var hint = Hint( failure );
        hint.ShouldContain( "@fortawesome/pro-solid-svg-icons@npm:^6.0.0" );
        hint.ShouldContain( "credentials" );
    }

    // failure-surface exploration, Q8: the YN0035 and YN0041 templates. Whether Yarn prints them without the
    // "<descriptor>: " prefix is not recorded; the code alone gives the kind.
    [TestCase( "➤ YN0035: │ The remote server failed to provide the requested resource", "PackageNotFound" )]
    [TestCase( "➤ YN0041: │ Invalid authentication (as an anonymous user)", "Authentication" )]
    public void yarn_error_code_without_a_descriptor_gives_its_kind( string line, string kind )
    {
        var failure = Classify( UpgradeCommandKind.YarnInstall, 1, line );
        failure.Kind.ToString().ShouldBe( kind );
        failure.Details.ShouldBeEmpty();
        Hint( failure ).ShouldNotContain( ": ." );
    }

    [Test]
    public void cli_21_manifest_error_is_registry_unreachable()
    {
        // failure-surface exploration, Q7 (CLI 21 and 22 under Yarn Berry) and "Not determined".
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, "Error fetching manifest for '@angular/core': Process exited with code 1." );
        failure.Kind.ShouldBe( UpgradeFailureKind.RegistryUnreachable );
        Hint( failure ).ShouldContain( "If the network is fine, run 'yarn npm info @angular/core'" );
    }

    // failure-surface exploration, Q7: a pacote network error of the schematic's metadata fetch arrives as
    // "Migration failed: <pacote message>", and CLI 20's own fetch prints "Error fetching metadata for '<package>': <message>".
    [TestCase( "✖ Migration failed: request to https://registry.npmjs.org/@angular%2fcore failed, reason: getaddrinfo ENOTFOUND registry.npmjs.org" )]
    [TestCase( "Error fetching metadata for '@angular/core': request to https://registry.npmjs.org/@angular%2fcore failed, reason: getaddrinfo ENOTFOUND registry.npmjs.org" )]
    public void cli_20_pacote_network_failure_is_registry_unreachable( string line )
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, line, "  See \"/tmp/ng-Ab12Cd/angular-errors.log\" for further details." );
        failure.Kind.ShouldBe( UpgradeFailureKind.RegistryUnreachable );
    }

    [Test]
    public void peer_refusal_cli_20_shape()
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, _cli20PeerRefusal );
        failure.Kind.ShouldBe( UpgradeFailureKind.PeerRefusal );
        failure.Peers.ShouldBe( [new PeerConflict( "@ngx-translate/core", "@angular/core", "^19.0.0", "21.0.0" ),
                                 new PeerConflict( "@angular/core", "rxjs", "^6.5.3 || ^7.4.0", "6.4.0" )] );
    }

    [Test]
    public void peer_refusal_cli_22_1_shape()
    {
        // failure-surface exploration, Q1 (CLI 22.1.0+), reached by a future step to 23.
        var failure = Classify( UpgradeCommandKind.PackageCommand,
                                1,
                                "  Package \"@ngx-translate/core\" has an incompatible peer dependency to \"@angular/core\" (requires \"^21.0.0\", would install \"23.0.0\").",
                                "Incompatible peer dependencies found. See above for details. You can bypass this check using the --force option." );
        failure.Kind.ShouldBe( UpgradeFailureKind.PeerRefusal );
        failure.Peers.ShouldBe( [new PeerConflict( "@ngx-translate/core", "@angular/core", "^21.0.0", "23.0.0" )] );
    }

    [Test]
    public void peer_summary_without_a_parsable_conflict_line_is_generic()
    {
        // failure-surface exploration, Q1 (CLI 22.1.0+ summary), with a conflict line in a shape the parser does not know.
        var failure = Classify( UpgradeCommandKind.PackageCommand,
                                1,
                                "  @ngx-translate/core requires @angular/core ^21.0.0 but 23.0.0 would be installed.",
                                "Incompatible peer dependencies found. See above for details. You can bypass this check using the --force option." );
        failure.Kind.ShouldBe( UpgradeFailureKind.Generic );
        Hint( failure ).ShouldContain( "@ngx-translate/core requires @angular/core ^21.0.0" );
    }

    [Test]
    public void unable_to_install_packages_is_install_inside_ng_update()
    {
        // failure-surface exploration, Q7 "Failures after the write": only the substring is certain.
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, "UPDATE package.json (2310 bytes)", "✖ Unable to install packages" );
        failure.Kind.ShouldBe( UpgradeFailureKind.InstallInsideNgUpdate );
    }

    [Test]
    public void exit_3_is_node_refused()
    {
        // failure-surface exploration, Q6 (CLI 22.x bin/ng.js).
        var failure = Classify( UpgradeCommandKind.Migration,
                                3,
                                "Node.js version v24.11.0 detected.",
                                "The Angular CLI requires a minimum Node.js version of v22.22.3 or v24.15.0 or v26.0.0.",
                                "",
                                "Please update your Node.js version or visit https://nodejs.org/ for additional instructions." );
        failure.Kind.ShouldBe( UpgradeFailureKind.NodeRefused );
        var hint = Hint( failure );
        hint.ShouldContain( "v24.11.0" );
        hint.ShouldContain( "v22.22.3 or v24.15.0 or v26.0.0" );
        hint.ShouldContain( "https://angular.dev/reference/versions" );
    }

    [Test]
    public void no_migrations_message_is_not_a_failure_of_the_migration()
    {
        // failure-surface exploration, Q2 (@fortawesome/angular-fontawesome has no ng-update field).
        var failure = Classify( UpgradeCommandKind.Migration, 1, "Package does not provide migrations." );
        failure.Kind.ShouldBe( UpgradeFailureKind.NoMigrations );
    }

    // failure-surface exploration, Q3, and "Output conventions", Glyphs: listr2 prints the pointer and the cross as
    // '❯' and '✖' when it detects unicode support, '>' and '×' otherwise.
    [TestCase( "❯", "✖" )]
    [TestCase( ">", "×" )]
    public void migration_failure_takes_the_name_from_the_preceding_title_line( string pointer, string cross )
    {
        var failure = Classify( UpgradeCommandKind.Migration,
                                1,
                                "** Executing migrations of package '@angular/core' **",
                                "",
                                $"{pointer} Updates two-way bindings that have an invalid expression to use the longform expression instead.",
                                "  Migration completed (No changes made).",
                                "",
                                $"{pointer} Replaces usages of the deprecated InjectFlags enum.",
                                $"{cross} Migration failed: Cannot read properties of undefined (reading 'kind')",
                                "  See \"/tmp/ng-Ab12Cd/angular-errors.log\" for further details." );
        failure.Kind.ShouldBe( UpgradeFailureKind.MigrationFailed );
        failure.MigrationName.ShouldBe( "Replaces usages of the deprecated InjectFlags enum." );
        failure.LogPath.ShouldBe( "/tmp/ng-Ab12Cd/angular-errors.log" );
        var hint = Hint( failure );
        hint.ShouldContain( "Replaces usages of the deprecated InjectFlags enum." );
        hint.ShouldContain( "Cannot read properties of undefined (reading 'kind')" );
        hint.ShouldContain( "/tmp/ng-Ab12Cd/angular-errors.log" );
        hint.ShouldContain( "this app's code" );
    }

    [Test]
    public void migration_conflict_shows_the_error_lines()
    {
        // failure-surface exploration, Q3 (a conflict reported by the dry-run sink).
        var failure = Classify( UpgradeCommandKind.Migration,
                                1,
                                "** Executing migrations of package '@angular/core' **",
                                "",
                                "❯ Replaces usages of the deprecated InjectFlags enum.",
                                "ERROR! /src/app/app.ts already exists.",
                                "✖ Migration failed. See above for further details." );
        failure.Kind.ShouldBe( UpgradeFailureKind.MigrationFailed );
        var hint = Hint( failure );
        hint.ShouldContain( "ERROR! /src/app/app.ts already exists." );
        hint.ShouldNotContain( "See above" );
    }

    [Test]
    public void migration_error_message_keeps_its_leading_dot()
    {
        // failure-surface exploration, Q3 ("Migration failed: <error.message>"), with a message that starts with a file name.
        var failure = Classify( UpgradeCommandKind.Migration, 1, "✖ Migration failed: .browserslistrc could not be parsed." );
        Hint( failure ).ShouldContain( "failed: .browserslistrc could not be parsed." );
    }

    [Test]
    public void unhandled_exception_in_a_migration_is_a_migration_failure()
    {
        // failure-surface exploration, Q3 (exit 127 shape) with the text observed in the Task 0 probe (ng-zorro-antd 22, strict PnP).
        var failure = Classify( UpgradeCommandKind.Migration,
                                127,
                                "An unhandled exception occurred: @angular/cdk tried to access @angular-devkit/schematics, but it isn't declared in its dependencies",
                                "See \"/tmp/ng-Ab12Cd/angular-errors.log\" for further details." );
        failure.Kind.ShouldBe( UpgradeFailureKind.MigrationFailed );
        failure.MigrationName.ShouldBeNull();
        failure.LogPath.ShouldBe( "/tmp/ng-Ab12Cd/angular-errors.log" );
        var hint = Hint( failure );
        hint.ShouldContain( "the cause is the upgrade tooling, not the app. Report it to CK" );
        hint.ShouldContain( "manual commands" );
        hint.ShouldNotContain( "this app's code" );
    }

    // The package command's own failure shapes: the expected crash of its migration phase (Task 0 probe, exit 127),
    // and a refusal of the update schematic (failure-surface exploration, Q7, "Schematic-level refusals").
    [TestCase( 127, "An unhandled exception occurred: Cannot find package '@jridgewell/sourcemap-codec' imported from magic-string/dist/index.mjs" )]
    [TestCase( 1, "✖ Migration failed: Package \"ng-zorro-antd\" was not found in package.json." )]
    public void migration_failure_texts_of_the_package_command_are_generic( int exitCode, string line )
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, exitCode, line );
        failure.Kind.ShouldBe( UpgradeFailureKind.Generic );
    }

    [Test]
    public void ansi_codes_are_ignored()
    {
        // failure-surface exploration, "Output conventions": warn lines are coloured when the logger detects colour support.
        var failure = Classify( UpgradeCommandKind.PackageCommand,
                                1,
                                "\u001b[33m    Package \"@angular/core\" has an incompatible peer dependency to \"rxjs\" (requires \"^6.5.3 || ^7.4.0\", would install \"6.4.0\")\u001b[39m",
                                "\u001b[31m✖ Migration failed: Incompatible peer dependencies found.\u001b[39m" );
        failure.Kind.ShouldBe( UpgradeFailureKind.PeerRefusal );
        failure.Peers.ShouldBe( [new PeerConflict( "@angular/core", "rxjs", "^6.5.3 || ^7.4.0", "6.4.0" )] );
    }

    [Test]
    public void unknown_output_is_generic_with_the_last_30_lines()
    {
        var lines = Enumerable.Range( 1, 40 ).Select( i => $"line {i}" ).ToArray();
        var failure = Classify( UpgradeCommandKind.YarnInstall, 1, lines );
        failure.Kind.ShouldBe( UpgradeFailureKind.Generic );
        failure.Details.ShouldBe( lines.Skip( 10 ).ToArray() );
        var hint = Hint( failure );
        hint.ShouldContain( "line 40" );
        hint.ShouldNotContain( "line 10" );
        hint.ShouldContain( "build log" );
    }

    [Test]
    public void peer_hint_names_the_ck_declarer()
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, _libraryPeerRefusal );
        var hint = Hint( failure, "CK.Ng.Translate.TranslatePackage" );
        hint.ShouldContain( "'@ngx-translate/core' needs '@angular/core' ^20.0.0, and 22.2.3 would be installed" );
        hint.ShouldContain( "CK package 'CK.Ng.Translate.TranslatePackage'" );
        hint.ShouldContain( "not ready for Angular 22" );
    }

    [Test]
    public void peer_hint_for_a_configured_library_names_the_libraryversions_entry()
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, _libraryPeerRefusal );
        var hint = Hint( failure, PackageDependency.ConfigurationSourceName );
        hint.ShouldContain( "LibraryVersions entry '@ngx-translate/core'" );
        hint.ShouldNotContain( "CK package" );
    }

    [Test]
    public void peer_hint_for_an_app_library_advises_its_release_for_the_previous_angular_major()
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, _libraryPeerRefusal );
        var hint = Hint( failure );
        hint.ShouldContain( "set '@ngx-translate/core' in the app's package.json to a release whose peer range accepts '@angular/core' 22.2.3" );
        hint.ShouldContain( "Its release for Angular 21 is enough" );
    }

    [Test]
    public void peer_hint_for_a_library_on_another_peer_does_not_promise_the_previous_major()
    {
        // failure-surface exploration, Q1 (reverse check): the CLI widens only a peer range on @angular/core.
        var failure = Classify( UpgradeCommandKind.PackageCommand,
                                1,
                                "    Package \"@acme/cdk-widgets\" has an incompatible peer dependency to \"@angular/cdk\" (requires \"^21.0.0\", would install \"22.0.0\").",
                                "✖ Migration failed: Incompatible peer dependencies found." );
        var hint = Hint( failure );
        hint.ShouldContain( "set '@acme/cdk-widgets' in the app's package.json to a release whose peer range accepts '@angular/cdk' 22.0.0" );
        hint.ShouldNotContain( "Angular 21" );
    }

    [Test]
    public void angular_package_group_member_counts_as_updated()
    {
        // failure-surface exploration, Q1 (forward check, CLI 20 to 22.0 shape): @angular/compiler-cli is updated with
        // the @angular/core package group, though the step's package command does not name it.
        var failure = Classify( UpgradeCommandKind.PackageCommand,
                                1,
                                "    Package \"@angular/compiler-cli\" has an incompatible peer dependency to \"typescript\" (requires \">=5.9 <6.1\", would install \"5.8.3\")",
                                "✖ Migration failed: Incompatible peer dependencies found." );
        var hint = Hint( failure );
        hint.ShouldContain( "set 'typescript' in the app's package.json to a version in >=5.9 <6.1" );
        hint.ShouldNotContain( "set '@angular/compiler-cli'" );
    }

    [Test]
    public void zone_js_hint_names_zone_js_as_the_culprit()
    {
        // failure-surface exploration, Q1 (CLI 20 to 22.0 forward check of @angular/core on its zone.js peer: no final period).
        var failure = Classify( UpgradeCommandKind.PackageCommand,
                                1,
                                "Package \"@angular/core\" has an incompatible peer dependency to \"zone.js\" (requires \"~0.15.0\", would install \"0.14.10\")",
                                "✖ Migration failed: Incompatible peer dependencies found." );
        failure.Peers.ShouldBe( [new PeerConflict( "@angular/core", "zone.js", "~0.15.0", "0.14.10" )] );
        var hint = Hint( failure );
        hint.ShouldContain( "set 'zone.js' in the app's package.json to a version in ~0.15.0" );
        hint.ShouldNotContain( "set '@angular/core'" );
    }

    // Task 9 first run (CLI 21 at the step to 21): @angular/cdk@21 peers on ^21 || ^22 and pulled part of the group to 22.
    static readonly string[] _splitGroupRefusal =
    [
        "                  Package \"@angular/build\" has an incompatible peer dependency to \"typescript\" (requires \">=5.9 <6.0\", would install \"6.0.3\")",
        "                  Package \"@angular/animations\" has an incompatible peer dependency to \"@angular/core\" (requires \"22.2.3\", would install \"21.2.25\")",
        "                  Package \"@angular/core\" has an incompatible peer dependency to \"@angular/compiler\" (requires \"21.2.25\", would install \"22.2.3\")",
        "                  Package \"ng-zorro-antd\" has an incompatible peer dependency to \"@angular/forms\" (requires \"^21.0.0\", would install \"22.2.3\")",
        "                  Package \"@angular/build\" has an incompatible peer dependency to \"typescript\" (requires \">=5.9 <6.0\", would install \"6.0.3\").",
        "                  Package \"@angular/animations\" has an incompatible peer dependency to \"@angular/core\" (requires \"22.2.3\" (extended), would install \"21.2.25\").",
        "✖ Migration failed: Incompatible peer dependencies found."
    ];

    [Test]
    public void a_conflict_printed_twice_is_listed_once()
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, _splitGroupRefusal );
        failure.Peers.Count.ShouldBe( 4 );
        var hint = UpgradeFailureClassifier.DescribeHint( failure, 21, _updated22, _ => null );
        hint.Split( "'@angular/animations' needs" ).Length.ShouldBe( 2 );
    }

    [Test]
    public void a_split_angular_group_is_reported_as_a_tooling_defect_without_blaming_packages()
    {
        var failure = Classify( UpgradeCommandKind.PackageCommand, 1, _splitGroupRefusal );
        var hint = UpgradeFailureClassifier.DescribeHint( failure, 21, _updated22, _ => "CK.Ng.Zorro.ZorroPackage" );
        hint.ShouldContain( "ng update would install Angular packages from majors 21 and 22" );
        hint.ShouldContain( "'ng-zorro-antd' needs '@angular/forms' ^21.0.0, and 22.2.3 would be installed." );
        hint.ShouldNotContain( "CK package" );
        hint.ShouldNotContain( "To fix" );
    }

    [Test]
    public void an_angular_package_as_culprit_is_neither_blamed_on_its_declarer_nor_set_by_hand()
    {
        // The engine registers @angular/core itself, so its declarer is a source file path, not a CK package.
        var failure = Classify( UpgradeCommandKind.PackageCommand,
                                1,
                                "    Package \"@angular/core\" has an incompatible peer dependency to \"@angular/compiler\" (requires \"22.2.3\", would install \"22.1.0\")",
                                "✖ Migration failed: Incompatible peer dependencies found." );
        var hint = Hint( failure, @"D:\src\CK.TS.Angular.Engine\AngularCodeGeneratorImpl.cs" );
        hint.ShouldContain( "'@angular/core' is an Angular package the upgrade moves to Angular 22 itself" );
        hint.ShouldNotContain( "AngularCodeGeneratorImpl.cs" );
        hint.ShouldNotContain( "To fix" );
    }
}
