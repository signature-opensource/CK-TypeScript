using CK.Core;
using CK.Setup;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CK.TS.Angular.Engine;

/// <summary>
/// What an Angular upgrade did, for the compile check and the summary that follow the integration.
/// </summary>
/// <param name="AppPath">The app folder.</param>
/// <param name="FromMajor">The Angular major of the app before the upgrade.</param>
/// <param name="ToMajor">The Angular major reached.</param>
/// <param name="Steps">The steps done.</param>
/// <param name="OptionalMigrations">The optional migrations the migration commands listed and did not run.</param>
/// <param name="PackagesWithoutMigrations">The packages whose migration command found no migration.</param>
/// <param name="Elapsed">The time the upgrade took.</param>
internal sealed record AngularUpgradeRun( NormalizedPath AppPath,
                                          int FromMajor,
                                          int ToMajor,
                                          IReadOnlyList<AngularUpgradeStep> Steps,
                                          IReadOnlyList<(string Package, string Name)> OptionalMigrations,
                                          IReadOnlyList<string> PackagesWithoutMigrations,
                                          TimeSpan Elapsed );

public partial class AngularCodeGeneratorImpl
{
    internal sealed partial class AngularCodeGen
    {
        static readonly SVersionBound _defaultAngularCliBound = SVersionBound.NpmTryParse( DefaultAngularCliVersion ).Result;

        AngularUpgradeRun? _upgradeRun;

        /// <summary>
        /// Gets the Angular major of the engine: the major of <see cref="DefaultAngularCliVersion"/>.
        /// </summary>
        internal static int TargetMajor => _defaultAngularCliBound.Base.Major;

        /// <summary>
        /// Gets the <c>@angular/cli</c> version: the <c>LibraryVersions</c> entry when there is one, else
        /// <see cref="DefaultAngularCliVersion"/>.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="integrationContext">The integration context.</param>
        /// <returns>The version bound.</returns>
        internal static SVersionBound ResolveAngularCliVersion( IActivityMonitor monitor, TypeScriptIntegrationContext integrationContext )
        {
            if( integrationContext.ConfiguredLibraries.TryGetValue( "@angular/cli", out var configured ) )
            {
                monitor.Info( $"Using @angular/cli version '{configured}' from configured LibraryVersions." );
                return configured;
            }
            Throw.DebugAssert( "The version defined in code is necessarily valid and not All.", _defaultAngularCliBound != SVersionBound.All );
            monitor.Info( $"Using @angular/cli default version '{DefaultAngularCliVersion}'." );
            return _defaultAngularCliBound;
        }

        /// <summary>
        /// Gets the message of the undo stash of an app folder: <c>ck-angular-upgrade undo &lt;app folder name&gt; &lt;UTC time&gt;</c>.
        /// </summary>
        /// <param name="appPath">The app folder.</param>
        /// <returns>The stash message.</returns>
        internal static string CreateStashMessage( NormalizedPath appPath )
        {
            return $"ck-angular-upgrade undo {appPath.LastPart} {DateTime.UtcNow.ToString( "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture )}";
        }

        /// <summary>
        /// Undoes an upgrade that failed after its first write: moves the changes of the app folder into the stash
        /// <paramref name="stashMessage"/>, runs <c>yarn install</c> on the restored files when <paramref name="runYarnInstall"/>
        /// is true, then deletes the <see cref="AngularUpgradeMarker"/>. The marker stays when the stash fails.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="integrationContext">The integration context.</param>
        /// <param name="appPath">The app folder.</param>
        /// <param name="stashMessage">The stash message, from <see cref="CreateStashMessage"/>.</param>
        /// <param name="stashCreated">Whether a stash has been created, as <see cref="AngularGitHelper.StashUndo"/> reports it.</param>
        /// <param name="runYarnInstall">False to skip <c>yarn install</c>.</param>
        /// <returns>The outcome of the undo.</returns>
        internal static AngularUndoOutcome UndoUpgrade( IActivityMonitor monitor,
                                                        TypeScriptIntegrationContext integrationContext,
                                                        NormalizedPath appPath,
                                                        string stashMessage,
                                                        out bool stashCreated,
                                                        bool runYarnInstall = true )
        {
            if( !AngularGitHelper.StashUndo( monitor, appPath, stashMessage, out stashCreated ) )
            {
                return AngularUndoOutcome.NotRestored;
            }
            var outcome = AngularUndoOutcome.Restored;
            if( runYarnInstall && !RunYarnInstall( monitor, integrationContext, appPath ) )
            {
                outcome = AngularUndoOutcome.RestoredInstallFailed;
            }
            AngularUpgradeMarker.Delete( appPath );
            return outcome;
        }

        void OnAfterCodeGeneration( object? sender, EventMonitoredArgs e )
        {
            Throw.DebugAssert( sender is TypeScriptContext );
            var context = (TypeScriptContext)sender;
            Throw.DebugAssert( "Inline mode => IntegrationContext.", context.IntegrationContext != null );
            var integrationContext = context.IntegrationContext;
            var monitor = e.Monitor;
            var appPath = integrationContext.Configuration.TargetProjectPath;
            // Initialize has set it in memory, and every reload resets it from the file.
            var packageManager = integrationContext.TargetPackageJson.PackageManager;

            if( AngularUpgradeMarker.Exists( appPath ) && !RecoverPreviousUpgrade( monitor, integrationContext, appPath, packageManager ) )
            {
                return;
            }

            var appDependencies = GetAppDependencies( integrationContext.TargetPackageJson );
            int? appMajor = AngularUpgradePlanner.GetAppMajor( appDependencies );
            int targetMajor = TargetMajor;
            if( appMajor is not int fromMajor || fromMajor >= targetMajor )
            {
                return;
            }
            // Integration writes only the used libraries into package.json.
            var declaredLibraries = context.Root.LibraryManager.LibraryImports.Values
                                                                              .Where( l => l.IsUsed )
                                                                              .Select( l => new DeclaredLibrary( l.Name, l.Version, l.DefinitionSource ) )
                                                                              .ToList();
            var declaredBounds = declaredLibraries.ToDictionary( l => l.Name, l => l.Version, StringComparer.OrdinalIgnoreCase );
            SVersionBound? configuredCli = integrationContext.ConfiguredLibraries.TryGetValue( "@angular/cli", out var cli ) ? cli : null;
            var input = new AngularUpgradeSafetyInput( fromMajor,
                                                       targetMajor,
                                                       AngularUpgradeSafety.GetNodeVersion( monitor ),
                                                       AngularGitHelper.GetDirtyFiles( monitor, appPath ),
                                                       declaredLibraries,
                                                       configuredCli,
                                                       appDependencies,
                                                       ContinuousIntegrationDetector.Detect( Environment.GetEnvironmentVariable ) );
            var failures = AngularUpgradeSafety.Check( input );
            // Without a tooling row for every crossed major (the first condition), there is no plan.
            bool hasPath = Enumerable.Range( fromMajor + 1, targetMajor - fromMajor ).All( AngularMajorTooling.Rows.ContainsKey );
            if( failures.Count > 0 )
            {
                string? manualCommands = null;
                if( hasPath )
                {
                    var plan = AngularUpgradePlanner.Plan( fromMajor, targetMajor, appDependencies, declaredBounds );
                    manualCommands = AngularUpgradePlanner.RenderManualCommands( plan, ReadInstalledVersions( appPath, plan ) );
                }
                monitor.Error( AngularUpgradeSafety.RenderMessage( appPath, fromMajor, targetMajor, failures, manualCommands ) );
                return;
            }
            Throw.DebugAssert( "The first safety condition checks the path.", hasPath );
            var steps = AngularUpgradePlanner.Plan( fromMajor, targetMajor, appDependencies, declaredBounds );
            RunUpgrade( monitor, integrationContext, appPath, fromMajor, steps, packageManager );
        }

        void RunUpgrade( IActivityMonitor monitor,
                         TypeScriptIntegrationContext integrationContext,
                         NormalizedPath appPath,
                         int fromMajor,
                         IReadOnlyList<AngularUpgradeStep> steps,
                         string? packageManager )
        {
        }

        // Undoes the upgrade an in-progress marker reports. Returns false when the run must stop: an error has been logged.
        static bool RecoverPreviousUpgrade( IActivityMonitor monitor,
                                            TypeScriptIntegrationContext integrationContext,
                                            NormalizedPath appPath,
                                            string? packageManager )
        {
            var marker = AngularUpgradeMarker.Read( appPath );
            var stashMessage = CreateStashMessage( appPath );
            using( monitor.OpenInfo( $"Found '{AngularUpgradeMarker.FileName}' in '{appPath}': undoing the previous Angular upgrade, which was not verified." ) )
            {
                bool stashed;
                bool stashCreated;
                IReadOnlyList<ActivityMonitorSimpleCollector.Entry> gitErrors;
                using( monitor.CollectEntries( out gitErrors, LogLevelFilter.Error ) )
                {
                    stashed = AngularGitHelper.StashUndo( monitor, appPath, stashMessage, out stashCreated );
                }
                if( !stashed )
                {
                    monitor.Error( RenderNotUndone( appPath, stashMessage, gitErrors ) );
                    return false;
                }
                // The model was read before the run: a stale one would be compared, then written over the restored file.
                if( !ReloadAppFiles( monitor, integrationContext, packageManager ) )
                {
                    monitor.Error( $"Unable to read the package.json and tsconfig.json of '{appPath}' after the undo of the previous Angular upgrade." );
                    return false;
                }
                int? restoredMajor = AngularUpgradePlanner.GetAppMajor( GetAppDependencies( integrationContext.TargetPackageJson ) );
                if( marker != null && restoredMajor is int major && major != marker.FromMajor )
                {
                    AngularUpgradeMarker.Delete( appPath );
                    monitor.Error( RenderPartialUpgradeCommitted( appPath, marker.FromMajor, major, stashMessage, stashCreated ) );
                    return false;
                }
                bool installed = RunYarnInstall( monitor, integrationContext, appPath );
                AngularUpgradeMarker.Delete( appPath );
                var started = marker != null
                                ? $"started {marker.StartedUtc.ToString( "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture )}"
                                : "started at an unknown time";
                var undone = stashCreated
                                ? $"It has been undone (stash '{stashMessage}')."
                                : "It had changed no tracked file: there was nothing to undo.";
                var install = installed
                                ? ""
                                : " But 'yarn install' failed on the restored app, so the installed packages may not match it until the next install succeeds.";
                monitor.Warn( $"A previous Angular upgrade of '{appPath}' ({started}) was not verified: the builder stopped before the compile check, or another build was running on this app. {undone}{install} Do not run two builds on the same app at the same time." );
                return true;
            }
        }

        static string RenderNotUndone( NormalizedPath appPath, string stashMessage, IReadOnlyList<ActivityMonitorSimpleCollector.Entry> gitErrors )
        {
            var reported = gitErrors.Select( e => e.Exception != null ? $"{e.Text} {e.Exception.Message}" : e.Text );
            return AngularUpgradeFailureMessage.Render( $"Undoing the previous Angular upgrade of '{appPath}', found by its marker '{AngularUpgradeMarker.FileName}'.",
                                                        $"The previous upgrade could not be undone: git failed.{Environment.NewLine}{string.Join( Environment.NewLine, reported )}",
                                                        appPath,
                                                        stashMessage,
                                                        AngularUndoOutcome.NotRestored,
                                                        stashCreated: false,
                                                        IsRootGitIgnoreUntracked( appPath ),
                                                        [$"Fix what git reports, such as a lock file left by a stopped git process or a file held open, then rerun the builder: the marker '{AngularUpgradeMarker.FileName}' stays, so the next build undoes the previous upgrade again.",
                                                         "Or run the commands above in the app folder, then rerun the builder."] );
        }

        static string RenderPartialUpgradeCommitted( NormalizedPath appPath, int fromMajor, int restoredMajor, string stashMessage, bool stashCreated )
        {
            var stash = stashCreated
                            ? $"The uncommitted changes of the app folder '{appPath}' are kept in the stash '{stashMessage}': find it with 'git stash list'."
                            : $"The app folder '{appPath}' had no uncommitted change.";
            return $"""
                    Undoing the previous Angular upgrade of '{appPath}', from Angular {fromMajor}, found by its marker '{AngularUpgradeMarker.FileName}'.
                    A half-done upgrade was committed: without its uncommitted changes, the app is on Angular {restoredMajor}, not on Angular {fromMajor}. An upgrade from there would skip the migrations the previous upgrade did not run.
                    {stash} The marker has been deleted and 'yarn install' has not run, so the installed packages may not match the app.
                    Next actions:
                    1. Revert the commit that holds the half-done upgrade, so that the app is back on Angular {fromMajor}, then rerun the builder: it installs the packages and upgrades the app again.
                    """;
        }

        // Reload resets CKVersion and PackageManager from the file: both are set in memory before code generation.
        static bool ReloadAppFiles( IActivityMonitor monitor, TypeScriptIntegrationContext integrationContext, string? packageManager )
        {
            var packageJson = integrationContext.TargetPackageJson;
            if( !packageJson.Reload( monitor ) || !integrationContext.TSConfigJson.Reload( monitor ) )
            {
                return false;
            }
            packageJson.CKVersion = TypeScriptContext.CKTypeScriptCurrentVersion;
            packageJson.PackageManager = packageManager;
            return true;
        }

        static bool RunYarnInstall( IActivityMonitor monitor, TypeScriptIntegrationContext integrationContext, NormalizedPath appPath )
        {
            var result = integrationContext.RunYarnProcess( monitor, "install", workingDirectory: appPath, timeout: UpgradeFailureClassifier.CommandTimeout );
            if( result.TimedOut || result.ExitCode != 0 )
            {
                monitor.Warn( result.TimedOut
                                ? $"'yarn install' did not end within {UpgradeFailureClassifier.CommandTimeout.TotalMinutes} minutes in '{appPath}'."
                                : $"'yarn install' failed with exit code {result.ExitCode} in '{appPath}'." );
                return false;
            }
            return true;
        }

        // The printed stash command must exclude what StashUndo excludes. When git cannot say whether the root
        // .gitignore is tracked, excluding it is the safe side: a tracked one is never edited by the upgrade.
        static bool IsRootGitIgnoreUntracked( NormalizedPath appPath )
        {
            if( !File.Exists( appPath.AppendPart( ".gitignore" ) ) )
            {
                return false;
            }
            try
            {
                var tracked = AngularGitHelper.Run( appPath, "ls-files -- .gitignore", AngularGitHelper.GitTimeout );
                return tracked.TimedOut || tracked.ExitCode != 0 || tracked.Output.All( string.IsNullOrWhiteSpace );
            }
            catch( Exception )
            {
                return true;
            }
        }

        static Dictionary<string, SVersionBound> GetAppDependencies( PackageJsonFile packageJson )
        {
            return packageJson.Dependencies.Values.ToDictionary( d => d.Name, d => d.Version );
        }

        static Dictionary<string, SVersion> ReadInstalledVersions( NormalizedPath appPath, IReadOnlyList<AngularUpgradeStep> steps )
        {
            var installed = new Dictionary<string, SVersion>();
            var yarnLockPath = appPath.AppendPart( "yarn.lock" );
            if( File.Exists( yarnLockPath ) )
            {
                var yarnLock = File.ReadAllText( yarnLockPath );
                foreach( var name in steps.SelectMany( s => s.Packages ).Select( p => p.Name ).Distinct() )
                {
                    var version = YarnLockReader.GetInstalledVersion( yarnLock, name );
                    if( version != null )
                    {
                        installed.Add( name, version );
                    }
                }
            }
            return installed;
        }
    }
}
