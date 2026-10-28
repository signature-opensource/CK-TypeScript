using CK.Core;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.TS.Angular.Engine;

/// <summary>
/// A library bound declared to the engine, as the library manager holds it after code generation.
/// </summary>
/// <param name="Name">The library name.</param>
/// <param name="Version">The declared bound.</param>
/// <param name="Source">
/// The declaring CK type, or <see cref="PackageDependency.ConfigurationSourceName"/> when the bound is a
/// <c>LibraryVersions</c> entry.
/// </param>
internal sealed record DeclaredLibrary( string Name, SVersionBound Version, string Source );

/// <summary>
/// What the safety check of an Angular upgrade reads.
/// </summary>
/// <param name="AppMajor">The app's Angular major.</param>
/// <param name="TargetMajor">The Angular major of the engine.</param>
/// <param name="NodeVersion">The Node.js version yarn runs with, null when <c>node --version</c> did not run.</param>
/// <param name="DirtyFiles">The uncommitted changes of the app folder, null when it is not under git.</param>
/// <param name="DeclaredLibraries">
/// The library bounds declared by CK types and by the <c>LibraryVersions</c> configuration, one entry per library
/// name (compared ignoring case): the library manager has already merged the declarations of a name into one bound.
/// </param>
/// <param name="ConfiguredCli">The <c>@angular/cli</c> entry of <c>LibraryVersions</c>, if any.</param>
/// <param name="AppDependencies">The app's dependencies.</param>
/// <param name="ContinuousIntegration">The signal that detected a CI run, null when not on CI.</param>
internal sealed record AngularUpgradeSafetyInput( int AppMajor,
                                                  int TargetMajor,
                                                  SVersion? NodeVersion,
                                                  List<string>? DirtyFiles,
                                                  IReadOnlyList<DeclaredLibrary> DeclaredLibraries,
                                                  SVersionBound? ConfiguredCli,
                                                  IReadOnlyDictionary<string, SVersionBound> AppDependencies,
                                                  string? ContinuousIntegration );

/// <summary>
/// A failed safety condition.
/// </summary>
/// <param name="Text">What was found and what fixes it.</param>
/// <param name="HandUpgradeHelps">
/// Whether the manual upgrade commands are an alternative to fixing the cause. When false, the second next action
/// of the message refers to this issue by number, and the text gives the action.
/// </param>
internal sealed record SafetyFailure( string Text, bool HandUpgradeHelps );

/// <summary>
/// The safety check that runs before an Angular upgrade writes anything, and its message.
/// </summary>
internal static class AngularUpgradeSafety
{
    const string _nodeVersionsLink = "https://angular.dev/reference/versions";

    const int _maxDirtyFiles = 20;

    static readonly TimeSpan _nodeTimeout = TimeSpan.FromSeconds( 30 );

    /// <summary>
    /// Evaluates every condition of the safety check against <see cref="AngularMajorTooling.Rows"/>.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The failures, in the order of the conditions. Empty when the upgrade can run.</returns>
    internal static IReadOnlyList<SafetyFailure> Check( AngularUpgradeSafetyInput input ) => Check( input, AngularMajorTooling.Rows );

    /// <summary>
    /// Evaluates every condition of the safety check against the given tooling rows.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="rows">The tooling rows, indexed by major.</param>
    /// <returns>The failures, in the order of the conditions. Empty when the upgrade can run.</returns>
    internal static IReadOnlyList<SafetyFailure> Check( AngularUpgradeSafetyInput input, IReadOnlyDictionary<int, AngularMajorTooling> rows )
    {
        var failures = new List<SafetyFailure>();
        int target = input.TargetMajor;
        int supportedMajor = rows.Keys.Min() - 1;

        var crossedMajors = Enumerable.Range( input.AppMajor + 1, Math.Max( 0, target - input.AppMajor ) ).ToList();

        // Condition 1: a row for every crossed major.
        var missing = crossedMajors.Where( m => !rows.ContainsKey( m ) ).ToList();
        if( input.AppMajor < supportedMajor )
        {
            failures.Add( new SafetyFailure( $"The app is on Angular {input.AppMajor}: the engine upgrades from Angular {supportedMajor} or above. Upgrade the app by hand to Angular {supportedMajor} first, then rerun the builder, or stay on the previous CK major.",
                                             false ) );
        }
        else if( missing.Count > 0 )
        {
            failures.Add( new SafetyFailure( $"The engine has no upgrade tooling for Angular {string.Join( ", ", missing )}. Stay on the previous CK major.", false ) );
        }

        // Condition 2: Node satisfies every crossed row.
        var crossed = crossedMajors.Where( rows.ContainsKey ).Select( m => rows[m] ).ToList();
        if( input.NodeVersion == null )
        {
            failures.Add( new SafetyFailure( $"The Node.js version is unknown: 'node --version' did not run. The Angular CLI refuses a Node.js version outside its range in the middle of an upgrade. Check that Node.js runs, in a version listed at {_nodeVersionsLink}.",
                                             false ) );
        }
        else
        {
            var node = input.NodeVersion;
            var refusing = crossed.Where( r => !r.NodeRange.Any( b => b.Satisfy( node ) ) ).ToList();
            if( refusing.Count > 0 )
            {
                var ranges = refusing.Select( r => $"Angular {r.Major} needs {string.Join( " || ", r.NodeRange.Select( b => b.ToNpmString() ) )}" );
                failures.Add( new SafetyFailure( $"Node.js {node} is not supported by a crossed Angular major: {string.Join( "; ", ranges )}. Install a Node.js version that fits every range, see {_nodeVersionsLink}.",
                                                 false ) );
            }
        }

        // Condition 3: the app folder is under git and has no uncommitted change.
        if( input.DirtyFiles == null )
        {
            failures.Add( new SafetyFailure( "The app folder is not in a git work tree: the upgrade needs git to undo itself when a step fails. Put the app under git and commit it.", true ) );
        }
        else if( input.DirtyFiles.Count > 0 )
        {
            var b = new StringBuilder();
            b.Append( input.DirtyFiles.Count == 1
                        ? "The app folder has 1 uncommitted change: commit or stash it, so that the upgrade can be reviewed and undone on its own."
                        : $"The app folder has {input.DirtyFiles.Count} uncommitted changes: commit or stash them, so that the upgrade can be reviewed and undone on its own." );
            foreach( var line in input.DirtyFiles.Take( _maxDirtyFiles ) )
            {
                b.AppendLine().Append( "  " ).Append( line );
            }
            if( input.DirtyFiles.Count > _maxDirtyFiles )
            {
                b.AppendLine().Append( $"  ... and {input.DirtyFiles.Count - _maxDirtyFiles} more." );
            }
            failures.Add( new SafetyFailure( b.ToString(), true ) );
        }

        // Condition 4: each CK declaration of an Angular-numbered library accepts the target major.
        foreach( var library in input.DeclaredLibraries )
        {
            if( !IsAngularNumbered( library.Name ) || !AngularUpgradePlanner.HasMajor( library.Version ) || AcceptsMajor( library.Version, target ) )
            {
                continue;
            }
            var bound = library.Version.ToNpmString();
            failures.Add( library.Source == PackageDependency.ConfigurationSourceName
                            ? new SafetyFailure( $"The LibraryVersions entry '{library.Name}' {bound} does not accept Angular {target}, and it wins over every CK package. Remove or update the entry, or stay on the previous CK major.", false )
                            : new SafetyFailure( $"'{library.Name}' {bound} declared by '{library.Source}' does not accept Angular {target}: that CK package is not ready for Angular {target}. Use a version of it that supports Angular {target}, or stay on the previous CK major.", false ) );
        }

        // Condition 5: the CLI pin's base major is the target major, so ~22.1.0 passes and >=20.0.0 does not.
        // A pin without a major (*) names no other major.
        if( input.ConfiguredCli is SVersionBound cli && AngularUpgradePlanner.HasMajor( cli ) && cli.Base.Major != target )
        {
            failures.Add( new SafetyFailure( $"The LibraryVersions entry '@angular/cli' {cli.ToNpmString()} pins another major than Angular {target}, the major of this CK version. Remove the pin, or stay on the previous CK major.", false ) );
        }

        // Condition 6: more than two steps cannot carry a library outside Angular's numbering. The path counts from
        // the supported major, the one condition 1 asks to reach by hand.
        int steps = target - Math.Max( input.AppMajor, supportedMajor );
        if( steps > 2 )
        {
            var ahead = AngularUpgradePlanner.GetLastStepLibraries( input.AppDependencies,
                                                                    input.DeclaredLibraries.ToDictionary( l => l.Name, l => l.Version, StringComparer.OrdinalIgnoreCase ) );
            if( ahead.Count > 0 )
            {
                var names = ahead.Select( p => $"{p.Name} ({p.FromMajor} to {p.ToMajor})" );
                failures.Add( new SafetyFailure( $"The upgrade to Angular {target} crosses {steps} majors, and CK packages declare libraries outside Angular's numbering above the app's version: {string.Join( ", ", names )}. The Angular CLI accepts such a library one Angular major beyond its range only, so it cannot cross more than two majors at once. Go through the previous CK major first.",
                                                 false ) );
            }
        }

        // Condition 7: not on CI.
        if( input.ContinuousIntegration != null )
        {
            failures.Add( new SafetyFailure( $"This build runs on CI ({input.ContinuousIntegration} detected): the Angular upgrade never runs on CI, where Yarn refuses to change the lockfile. Run the builder on your machine, review and commit the upgraded app, then push.",
                                             false ) );
        }
        return failures;
    }

    /// <summary>
    /// Gets the version of the <c>node</c> found on the path.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <returns>The version, or null (a warning is logged) when <c>node --version</c> did not run, failed or printed no version.</returns>
    internal static SVersion? GetNodeVersion( IActivityMonitor monitor )
    {
        try
        {
            var result = AngularGitHelper.Run( Environment.CurrentDirectory, "--version", _nodeTimeout, "node" );
            if( result.TimedOut || result.ExitCode != 0 )
            {
                monitor.Warn( $"'node --version' {(result.TimedOut ? $"did not answer within {_nodeTimeout.TotalSeconds} seconds" : $"failed with exit code {result.ExitCode}")}.{Environment.NewLine}{string.Join( Environment.NewLine, result.Output )}" );
                return null;
            }
            var text = (result.Output.FirstOrDefault( l => !string.IsNullOrWhiteSpace( l ) ) ?? "").Trim().TrimStart( 'v' );
            var version = SVersion.TryParse( text );
            if( !version.IsValid )
            {
                monitor.Warn( $"'node --version' printed '{text}', which is not a version." );
                return null;
            }
            return version;
        }
        catch( Exception ex )
        {
            monitor.Warn( "Unable to run 'node --version'.", ex );
            return null;
        }
    }

    /// <summary>
    /// Renders the safety check failures in the four parts of an upgrade message: what the engine was doing, what it
    /// found, the app folder state (untouched) and the numbered next actions.
    /// </summary>
    /// <param name="appPath">The app folder.</param>
    /// <param name="appMajor">The app's Angular major.</param>
    /// <param name="targetMajor">The Angular major of the engine.</param>
    /// <param name="failures">The failures from <see cref="Check(AngularUpgradeSafetyInput)"/>.</param>
    /// <param name="manualCommands">The manual upgrade commands, or null when there is no upgrade plan.</param>
    /// <returns>The message.</returns>
    internal static string RenderMessage( NormalizedPath appPath,
                                          int appMajor,
                                          int targetMajor,
                                          IReadOnlyList<SafetyFailure> failures,
                                          string? manualCommands )
    {
        var b = new StringBuilder();
        b.AppendLine( $"Checking that the Angular app '{appPath}' can be upgraded from Angular {appMajor} to {targetMajor}." );
        b.AppendLine( failures.Count == 1 ? "Found 1 issue that blocks the upgrade:" : $"Found {failures.Count} issues that block the upgrade:" );
        for( int i = 0; i < failures.Count; i++ )
        {
            b.AppendLine( $"[{i + 1}] {failures[i].Text}" );
        }
        b.AppendLine( "The app folder is untouched." );
        b.AppendLine( "Next actions:" );
        b.AppendLine( failures.Count == 1
                        ? "1. Fix the issue above, then rerun the builder (recommended)."
                        : "1. Fix the issues above, then rerun the builder (recommended)." );
        if( failures.All( f => f.HandUpgradeHelps ) )
        {
            if( manualCommands != null )
            {
                b.AppendLine( "2. Upgrade by hand: run these commands in the app folder, then rerun the builder." );
                b.AppendLine( manualCommands.TrimEnd() );
            }
            else
            {
                b.AppendLine( "2. Upgrade by hand, then rerun the builder." );
            }
        }
        else
        {
            var numbers = Enumerable.Range( 1, failures.Count ).Where( n => !failures[n - 1].HandUpgradeHelps ).Select( n => $"[{n}]" ).ToList();
            b.AppendLine( numbers.Count == 1
                            ? $"2. The manual upgrade commands do not apply to issue {numbers[0]}: do what it says instead."
                            : $"2. The manual upgrade commands do not apply to issues {string.Join( ", ", numbers.Take( numbers.Count - 1 ) )} and {numbers[^1]}: do what each of them says instead." );
        }
        b.Append( "3. Stay on the previous CK major." );
        return b.ToString();
    }

    static bool IsAngularNumbered( string name ) => name.StartsWith( "@angular/", StringComparison.Ordinal ) || name == "ng-zorro-antd";

    // A bound's versions run from its base up to its lock: it holds a version of the major when it holds
    // major.0.0 or starts inside the major.
    static bool AcceptsMajor( SVersionBound bound, int major ) => bound.Base.Major == major || bound.Satisfy( SVersion.Create( major, 0, 0 ) );
}
