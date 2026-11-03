using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
internal sealed record AngularUpgradeRun( NormalizedPath AppPath,
                                          int FromMajor,
                                          int ToMajor,
                                          IReadOnlyList<AngularUpgradeStep> Steps,
                                          IReadOnlyList<(string Package, string Name)> OptionalMigrations,
                                          IReadOnlyList<string> PackagesWithoutMigrations );

public partial class AngularCodeGeneratorImpl
{
    internal sealed partial class AngularCodeGen
    {
        static readonly SVersionBound _defaultAngularCliBound = SVersionBound.NpmTryParse( DefaultAngularCliVersion ).Result;

        // The definition source of the dependencies the upgrade writes: a reload reads them back as the app's own.
        const string _upgradeSource = "Angular upgrade";

        // Strict PnP: an inherited YARN_PNP_MODE must not apply.
        static readonly Dictionary<string, string?> _strictEnvironment = new() { ["YARN_PNP_MODE"] = null };

        // Only an install that writes .pnp.cjs applies the PnP mode: this one runs right before the migrations.
        static readonly Dictionary<string, string?> _looseEnvironment = new() { ["YARN_PNP_MODE"] = "loose" };

        // An inherited NG_FORCE_TTY=1 would make the optional migrations prompt block on stdin. Without YARN_PNP_MODE,
        // the install that ng update runs itself is strict.
        static readonly Dictionary<string, string?> _ngEnvironment = new()
        {
            ["NG_DISABLE_VERSION_CHECK"] = "1",
            ["NG_FORCE_TTY"] = "0",
            ["NO_COLOR"] = "1",
            ["FORCE_COLOR"] = null,
            ["YARN_PNP_MODE"] = null
        };

        AngularUpgradeRun? _upgradeRun;

        // The Stopwatch timestamp of the start of the upgrade: the summary's elapsed time ends after the compile check.
        long _upgradeStarted;

        /// <summary>
        /// Gets the Angular major of the engine: the major of <see cref="DefaultAngularCliVersion"/>.
        /// </summary>
        internal static int TargetMajor => _defaultAngularCliBound.Base.Major;

        /// <summary>
        /// Gets the message of the undo stash of an app folder: <c>ck-angular-upgrade undo &lt;app folder name&gt; &lt;UTC time&gt;</c>.
        /// </summary>
        /// <param name="appPath">The app folder.</param>
        /// <returns>The stash message.</returns>
        internal static string CreateStashMessage( NormalizedPath appPath )
        {
            return $"ck-angular-upgrade undo {appPath.LastPart} {DateTime.UtcNow.ToString( AngularUpgradeMarker.StartedFormat, CultureInfo.InvariantCulture )}";
        }

        /// <summary>
        /// Undoes an upgrade that failed after its first write: moves the changes of the app folder into the stash
        /// <paramref name="stashMessage"/>, runs <c>yarn install</c> on the restored files, then deletes the
        /// <see cref="AngularUpgradeMarker"/>. The marker stays when the stash fails.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="integrationContext">The integration context.</param>
        /// <param name="appPath">The app folder.</param>
        /// <param name="stashMessage">The stash message, from <see cref="CreateStashMessage"/>.</param>
        /// <param name="stashCreated">Whether a stash has been created, as <see cref="AngularGitHelper.StashUndo"/> reports it.</param>
        /// <returns>The outcome of the undo.</returns>
        internal static AngularUndoOutcome UndoUpgrade( IActivityMonitor monitor,
                                                        TypeScriptIntegrationContext integrationContext,
                                                        NormalizedPath appPath,
                                                        string stashMessage,
                                                        out bool stashCreated )
        {
            if( !AngularGitHelper.StashUndo( monitor, appPath, stashMessage, out stashCreated ) )
            {
                return AngularUndoOutcome.NotRestored;
            }
            var outcome = AngularUndoOutcome.Restored;
            if( !RunYarnInstall( monitor, integrationContext, appPath ) )
            {
                outcome = AngularUndoOutcome.RestoredInstallFailed;
            }
            AngularUpgradeMarker.Delete( appPath );
            return outcome;
        }

        /// <summary>
        /// Runs an undo and its report. An exception is logged as one short error instead of the report:
        /// <see cref="UndoUpgrade"/> throws after the stash when the <c>yarn install</c> process cannot run or the marker
        /// cannot be deleted, and the marker left in place makes the next run finish the undo.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="integrationFailed">
        /// True when the undo follows an integration that failed after a successful upgrade, false when it follows a
        /// failed step: the error's lead says which.
        /// </param>
        /// <param name="undoAndReport">The undo and the report of the failure.</param>
        internal static void GuardUndo( IActivityMonitor monitor, bool integrationFailed, Action undoAndReport )
        {
            try
            {
                undoAndReport();
            }
            catch( Exception ex )
            {
                var lead = integrationFailed ? "The integration failed after the upgrade" : "The upgrade failed";
                monitor.Error( $"{lead} and its undo stopped: {ex.Message.TrimEnd( '.' )}. Rerun the builder: it finishes the undo.", ex );
            }
        }

        /// <summary>
        /// Deletes the <see cref="AngularUpgradeMarker"/> once the compile check has given its result, or failed to give
        /// one. A deletion that throws is logged as an error that names the file to delete by hand, since the next build
        /// would undo this upgrade.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="appPath">The app folder.</param>
        internal static void DeleteMarkerAfterCheck( IActivityMonitor monitor, NormalizedPath appPath )
        {
            try
            {
                AngularUpgradeMarker.Delete( appPath );
            }
            catch( Exception ex )
            {
                monitor.Error( $"Delete '{appPath.AppendPart( AngularUpgradeMarker.FileName )}' by hand before the next build, or it undoes this upgrade: {ex.Message.TrimEnd( '.' )}.", ex );
            }
        }

        void OnAfterCodeGeneration( object? sender, EventMonitoredArgs e )
        {
            Throw.DebugAssert( sender is TypeScriptContext );
            var context = (TypeScriptContext)sender;
            Throw.DebugAssert( "Inline mode => IntegrationContext.", context.IntegrationContext != null );
            var integrationContext = context.IntegrationContext;
            var monitor = e.Monitor;
            var appPath = integrationContext.Configuration.TargetProjectPath;

            if( AngularUpgradeMarker.Exists( appPath ) && !RecoverPreviousUpgrade( monitor, integrationContext, appPath ) )
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
            var steps = hasPath ? AngularUpgradePlanner.Plan( fromMajor, targetMajor, appDependencies, declaredBounds ) : null;
            if( failures.Count > 0 )
            {
                var manualCommands = steps != null
                                        ? AngularUpgradePlanner.RenderManualCommands( steps, ReadInstalledVersions( appPath, steps ) )
                                        : null;
                monitor.Error( AngularUpgradeSafety.RenderMessage( appPath, fromMajor, targetMajor, failures, manualCommands ) );
                return;
            }
            Throw.DebugAssert( "The first safety condition checks the path.", steps != null );
            RunUpgrade( monitor, integrationContext, appPath, fromMajor, steps, declaredLibraries );
        }

        void RunUpgrade( IActivityMonitor monitor,
                         TypeScriptIntegrationContext integrationContext,
                         NormalizedPath appPath,
                         int fromMajor,
                         IReadOnlyList<AngularUpgradeStep> steps,
                         IReadOnlyList<DeclaredLibrary> declaredLibraries )
        {
            int toMajor = steps[^1].ToMajor;
            // Read before the first write: the manual commands of a failure message start from the restored app.
            var manualCommands = AngularUpgradePlanner.RenderManualCommands( steps, ReadInstalledVersions( appPath, steps ) );
            using( monitor.OpenInfo( $"Upgrading the Angular app '{appPath}' from Angular {fromMajor} to {toMajor}. Do not edit the files of this folder, and do not run another build on this app, until the builder finishes." ) )
            {
                new AngularUpgradeMarker( fromMajor, DateTime.UtcNow ).Write( appPath );
                _upgradeStarted = Stopwatch.GetTimestamp();
                var optionalMigrations = new List<(string Package, string Name)>();
                var packagesWithoutMigrations = new List<string>();
                foreach( var step in steps )
                {
                    using( monitor.OpenInfo( $"Angular {step.FromMajor} to {step.ToMajor}." ) )
                    {
                        StepFailure? failed;
                        try
                        {
                            failed = RunStep( monitor, integrationContext, appPath, step, optionalMigrations, packagesWithoutMigrations );
                        }
                        catch( Exception ex )
                        {
                            // An exception leaves the app half-upgraded exactly as a failed command does: same undo, same message.
                            monitor.Warn( $"The step Angular {step.FromMajor} to {step.ToMajor} threw an exception.", ex );
                            failed = new StepFailure( "", default, null, $"An exception stopped the step: {ex.Message}" );
                        }
                        if( failed != null )
                        {
                            GuardUndo( monitor, integrationFailed: false, () => ReportStepFailure( monitor, integrationContext, appPath, fromMajor, step, failed, declaredLibraries, manualCommands ) );
                            return;
                        }
                    }
                }
                _upgradeRun = new AngularUpgradeRun( appPath, fromMajor, toMajor, steps, optionalMigrations, packagesWithoutMigrations );
            }
        }

        // On a run that upgraded the app: the undo when the integration failed, otherwise the compile check, the
        // marker deletion and the summary. The check needs the integrated ck-gen and installed packages.
        void OnAfterIntegrationUpgrade( TypeScriptIntegrationContext.AfterEventArgs e )
        {
            var run = _upgradeRun;
            if( run == null )
            {
                return;
            }
            _upgradeRun = null;
            var monitor = e.Monitor;
            var integrationContext = e.IntegrationContext;
            var appPath = run.AppPath;
            if( !e.Success )
            {
                GuardUndo( monitor, integrationFailed: true, () =>
                {
                    var stashMessage = CreateStashMessage( appPath );
                    var undo = UndoUpgrade( monitor, integrationContext, appPath, stashMessage, out bool stashCreated );
                    monitor.Error( AngularUpgradeReport.RenderIntegrationFailure( appPath,
                                                                                  run.FromMajor,
                                                                                  run.ToMajor,
                                                                                  stashMessage,
                                                                                  undo,
                                                                                  stashCreated,
                                                                                  IsRootGitIgnoreUntracked( appPath ) ) );
                } );
                return;
            }
            CompileCheckResult compileCheck;
            List<string> output = [];
            try
            {
                var result = RunCommand( monitor, integrationContext, appPath, AngularUpgradeReport.CompileCheckCommand, _ngEnvironment, out output );
                compileCheck = AngularUpgradeReport.EvaluateCompileCheck( result.ExitCode, result.TimedOut, output );
            }
            catch( Exception ex )
            {
                // RunYarnProcess throws when the process cannot start, or when a timed-out one cannot be killed: the
                // check gave no answer, which never reads as a pass, and the lines it printed before are kept.
                monitor.Warn( $"'yarn {AngularUpgradeReport.CompileCheckCommand}' threw in '{appPath}'.", ex );
                // The output readers of a process that could not be killed may still be adding lines.
                List<string> lastLines;
                lock( output ) lastLines = AngularUpgradeReport.LastLines( output );
                compileCheck = new CompileCheckResult( CompileCheckOutcome.DidNotRun, [], ex.Message.TrimEnd( '.' ), lastLines );
            }
            var elapsed = Stopwatch.GetElapsedTime( _upgradeStarted );
            // Deleted before the status: the marker is not a change of the upgrade.
            DeleteMarkerAfterCheck( monitor, appPath );
            var changedFiles = AngularGitHelper.GetDirtyFiles( monitor, appPath );
            // After GetDirtyFiles: an unknown file list refers to its warning above.
            using( monitor.OpenWarn( $"Summary of the Angular upgrade of '{appPath}'." ) )
            {
                monitor.Warn( AngularUpgradeReport.RenderSummary( run,
                                                                  elapsed,
                                                                  changedFiles,
                                                                  compileCheck,
                                                                  CreateStashMessage( appPath ),
                                                                  IsRootGitIgnoreUntracked( appPath ) ) );
            }
            var guideLink = AngularUpgradeReport.GuideLink( run.FromMajor, run.ToMajor );
            if( compileCheck.Outcome == CompileCheckOutcome.Errors )
            {
                var hints = run.Steps.SelectMany( s => s.Tooling.Hints ).ToList();
                var yarnLock = ReadYarnLock( appPath );
                var typeScriptVersion = yarnLock != null ? YarnLockReader.GetInstalledVersion( yarnLock, "typescript" ) : null;
                monitor.Error( AngularUpgradeReport.RenderCompileFailure( appPath, compileCheck.Errors, hints, guideLink, typeScriptVersion ) );
            }
            else if( compileCheck.Outcome == CompileCheckOutcome.DidNotRun )
            {
                monitor.Error( AngularUpgradeReport.RenderCompileCheckNotRun( appPath, compileCheck, guideLink ) );
            }
        }

        /// <summary>
        /// A failure of an upgrade step.
        /// </summary>
        /// <param name="Command">The yarn command that failed, without the <c>yarn </c> prefix, or empty when no command failed.</param>
        /// <param name="Result">The process result.</param>
        /// <param name="Failure">The classified failure, or null when the failure is not a command's.</param>
        /// <param name="Found">What was found when the failure alone does not describe it, or null.</param>
        internal sealed record StepFailure( string Command, TypeScriptIntegrationContext.ProcessResult Result, UpgradeFailure? Failure, string? Found = null );

        // Runs one step on a model that matches the files. Returns null on success.
        static StepFailure? RunStep( IActivityMonitor monitor,
                                     TypeScriptIntegrationContext integrationContext,
                                     NormalizedPath appPath,
                                     AngularUpgradeStep step,
                                     List<(string Package, string Name)> optionalMigrations,
                                     List<string> packagesWithoutMigrations )
        {
            var packageJson = integrationContext.TargetPackageJson;
            foreach( var (name, version, kind) in step.PreUpdateBounds )
            {
                packageJson.Dependencies.AddOrReplace( new PackageDependency( name, version, kind, _upgradeSource ), cloneAddedDependency: false );
            }
            packageJson.Save();
            var failed = RunClassified( monitor, integrationContext, appPath, "install", _strictEnvironment );
            if( failed != null )
            {
                return failed;
            }
            var yarnLock = ReadYarnLock( appPath );
            var fromVersions = step.Packages.ToDictionary( p => p.Name, p => YarnLockReader.GetFromVersion( monitor, yarnLock, p.Name, p.FromMajor ).ToString() );

            // Under PnP, the package command is expected to crash in its migration phase: the migrations run below.
            var packageCommand = RunCommand( monitor, integrationContext, appPath, step.PackageCommand, _ngEnvironment, out var packageOutput );
            var packageFailure = UpgradeFailureClassifier.Classify( UpgradeCommandKind.PackageCommand, packageCommand.ExitCode, packageCommand.TimedOut, packageOutput );
            // Fails even at major N: a registry that hung this command would hang the dedupe and installs that follow.
            if( packageFailure.Kind == UpgradeFailureKind.Timeout )
            {
                return new StepFailure( step.PackageCommand, packageCommand, packageFailure );
            }
            if( packageFailure.Kind == UpgradeFailureKind.InstallInsideNgUpdate )
            {
                // ng update hides yarn's output: yarn's own install says what failed.
                failed = RunClassified( monitor, integrationContext, appPath, "install", _strictEnvironment );
                if( failed != null )
                {
                    return failed;
                }
            }
            if( !ReloadAppFiles( monitor, integrationContext ) )
            {
                return new StepFailure( step.PackageCommand, packageCommand, packageFailure, $"The package.json or tsconfig.json of '{appPath}' cannot be read after the command (see the errors above)." );
            }
            int? reached = AngularUpgradePlanner.GetAppMajor( GetAppDependencies( packageJson ) );
            if( reached != step.ToMajor )
            {
                if( packageFailure.Kind == UpgradeFailureKind.None )
                {
                    packageFailure = new UpgradeFailure( UpgradeFailureKind.Generic, AngularUpgradeReport.LastLines( packageOutput ), [], null, null );
                }
                var core = packageJson.Dependencies.TryGetValue( "@angular/core", out var c ) ? $"'{c.NpmVersionRange}'" : "missing";
                return new StepFailure( step.PackageCommand, packageCommand, packageFailure, $"Afterwards, @angular/core in package.json is {core}, not Angular {step.ToMajor}." );
            }
            failed = RunClassified( monitor, integrationContext, appPath, "dedupe", _strictEnvironment )
                     ?? RunClassified( monitor, integrationContext, appPath, "install", _looseEnvironment );
            if( failed != null )
            {
                return failed;
            }
            foreach( var package in AngularUpgradePlanner.GetMigrationOrder( step ) )
            {
                var command = step.MigrationCommand( package, fromVersions[package.Name] );
                var result = RunCommand( monitor, integrationContext, appPath, command, _ngEnvironment, out var output );
                var failure = UpgradeFailureClassifier.Classify( UpgradeCommandKind.Migration, result.ExitCode, result.TimedOut, output );
                if( failure.Kind == UpgradeFailureKind.NoMigrations )
                {
                    packagesWithoutMigrations.Add( package.Name );
                }
                else if( failure.Kind != UpgradeFailureKind.None )
                {
                    return new StepFailure( command, result, failure );
                }
                foreach( var line in output )
                {
                    var m = OptionalMigrationRegex().Match( AngularUpgradeReport.AnsiRegex().Replace( line, "" ) );
                    if( m.Success )
                    {
                        optionalMigrations.Add( (m.Groups[1].Value, m.Groups[2].Value) );
                    }
                }
            }
            // The migrations may edit package.json and tsconfig.json: the next step and the integration start from the files.
            if( !ReloadAppFiles( monitor, integrationContext ) )
            {
                return new StepFailure( "", default, null, $"The package.json or tsconfig.json of '{appPath}' cannot be read after the migrations (see the errors above)." );
            }
            if( step.ReplacedPackages.Count > 0 )
            {
                ReplacePackages( monitor, packageJson, step );
            }
            return null;
        }

        // A replacement takes the @angular/core range (^N.0.0 when it is missing) and the kind of the replaced package.
        // The next install updates yarn.lock.
        static void ReplacePackages( IActivityMonitor monitor, PackageJsonFile packageJson, AngularUpgradeStep step )
        {
            var core = packageJson.Dependencies.TryGetValue( "@angular/core", out var c )
                        ? c.Version
                        : new SVersionBound( SVersion.Create( step.ToMajor, 0, 0 ), SVersionLock.LockMajor, PackageQuality.Stable );
            foreach( var (package, replacement) in step.ReplacedPackages )
            {
                if( packageJson.Dependencies.TryGetValue( package, out var replaced ) )
                {
                    packageJson.Dependencies.Remove( package );
                    if( !packageJson.Dependencies.ContainsKey( replacement ) )
                    {
                        packageJson.Dependencies.AddOrReplace( new PackageDependency( replacement, core, replaced.DependencyKind, _upgradeSource ), cloneAddedDependency: false );
                    }
                    monitor.Info( $"Replaced '{package}' by '{replacement}' in package.json: Angular {step.ToMajor} replaces it." );
                }
            }
            packageJson.Save();
        }

        static StepFailure? RunClassified( IActivityMonitor monitor,
                                           TypeScriptIntegrationContext integrationContext,
                                           NormalizedPath appPath,
                                           string command,
                                           Dictionary<string, string?> environment )
        {
            var result = RunCommand( monitor, integrationContext, appPath, command, environment, out var output );
            var failure = UpgradeFailureClassifier.Classify( UpgradeCommandKind.YarnInstall, result.ExitCode, result.TimedOut, output );
            return failure.Kind == UpgradeFailureKind.None ? null : new StepFailure( command, result, failure );
        }

        static TypeScriptIntegrationContext.ProcessResult RunCommand( IActivityMonitor monitor,
                                                                      TypeScriptIntegrationContext integrationContext,
                                                                      NormalizedPath appPath,
                                                                      string command,
                                                                      Dictionary<string, string?> environment,
                                                                      out List<string> output )
        {
            monitor.Info( $"Running 'yarn {command}'." );
            output = new List<string>();
            return integrationContext.RunYarnProcess( monitor, command, environment, appPath, output, UpgradeFailureClassifier.CommandTimeout );
        }

        // The "doing" and "found" parts of a step failure message.
        internal static (string Doing, string Found) DescribeStepFailure( NormalizedPath appPath,
                                                                          AngularUpgradeStep step,
                                                                          StepFailure failed,
                                                                          IReadOnlyList<DeclaredLibrary> declaredLibraries )
        {
            var updated = step.Packages.Select( p => p.Name ).ToHashSet();
            var hint = failed.Failure != null
                        ? UpgradeFailureClassifier.DescribeHint( failed.Failure,
                                                                 step.ToMajor,
                                                                 updated,
                                                                 name => declaredLibraries.FirstOrDefault( l => l.Name == name )?.Source )
                        : "";
            var doing = failed.Command.Length > 0
                            ? $"Upgrading the Angular app '{appPath}', step Angular {step.FromMajor} to {step.ToMajor}: running 'yarn {failed.Command}'."
                            : $"Upgrading the Angular app '{appPath}', step Angular {step.FromMajor} to {step.ToMajor}.";
            var exit = failed.Command.Length == 0 || failed.Result.TimedOut
                            ? ""
                            : $"'yarn {failed.Command}' ended with exit code {failed.Result.ExitCode}. ";
            var found = failed.Found != null ? $"{exit}{failed.Found} {hint}" : $"{exit}{hint}";
            return (doing, found);
        }

        static void ReportStepFailure( IActivityMonitor monitor,
                                       TypeScriptIntegrationContext integrationContext,
                                       NormalizedPath appPath,
                                       int fromMajor,
                                       AngularUpgradeStep step,
                                       StepFailure failed,
                                       IReadOnlyList<DeclaredLibrary> declaredLibraries,
                                       string manualCommands )
        {
            var stashMessage = CreateStashMessage( appPath );
            var undo = UndoUpgrade( monitor, integrationContext, appPath, stashMessage, out bool stashCreated );
            var (doing, found) = DescribeStepFailure( appPath, step, failed, declaredLibraries );
            var rerun = $"rerun the builder: it upgrades the app again from Angular {fromMajor}.";
            var byHand = $"Or upgrade the app by hand with these commands, then rerun the builder:{Environment.NewLine}{manualCommands}";
            IReadOnlyList<string> nextActions = undo == AngularUndoOutcome.NotRestored
                                                    ? [$"Run the commands above in the app folder, fix what is described above, then {rerun}", byHand]
                                                    : [$"Fix what is described above, then {rerun}", byHand];
            monitor.Error( AngularUpgradeFailureMessage.Render( doing,
                                                                found.TrimEnd(),
                                                                appPath,
                                                                stashMessage,
                                                                undo,
                                                                stashCreated,
                                                                IsRootGitIgnoreUntracked( appPath ),
                                                                nextActions ) );
        }

        [GeneratedRegex( @"^\s*ng update (\S+) --name (\S+)\s*$" )]
        private static partial Regex OptionalMigrationRegex();

        // Undoes the upgrade an in-progress marker reports. Returns false when the run must stop: an error has been logged.
        static bool RecoverPreviousUpgrade( IActivityMonitor monitor,
                                            TypeScriptIntegrationContext integrationContext,
                                            NormalizedPath appPath )
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
                if( !ReloadAppFiles( monitor, integrationContext ) )
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
                                ? $"started {marker.StartedUtc.ToString( AngularUpgradeMarker.StartedFormat, CultureInfo.InvariantCulture )}"
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
        static bool ReloadAppFiles( IActivityMonitor monitor, TypeScriptIntegrationContext integrationContext )
        {
            var packageJson = integrationContext.TargetPackageJson;
            var packageManager = packageJson.PackageManager;
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
            var yarnLock = ReadYarnLock( appPath );
            if( yarnLock == null )
            {
                return installed;
            }
            foreach( var name in steps.SelectMany( s => s.Packages ).Select( p => p.Name ).Distinct() )
            {
                var version = YarnLockReader.GetInstalledVersion( yarnLock, name );
                if( version != null )
                {
                    installed.Add( name, version );
                }
            }
            return installed;
        }

        static string? ReadYarnLock( NormalizedPath appPath )
        {
            var yarnLockPath = appPath.AppendPart( "yarn.lock" );
            return File.Exists( yarnLockPath ) ? File.ReadAllText( yarnLockPath ) : null;
        }
    }
}
