using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CK.TS.Angular.Engine;

/// <summary>
/// The kind of command of an Angular upgrade whose output is classified.
/// </summary>
internal enum UpgradeCommandKind
{
    /// <summary>
    /// A <c>yarn install</c> or a <c>yarn dedupe</c>.
    /// </summary>
    YarnInstall,

    /// <summary>
    /// The <c>ng update</c> command that updates the packages of a step.
    /// </summary>
    PackageCommand,

    /// <summary>
    /// An <c>ng update &lt;package&gt; --migrate-only</c> command.
    /// </summary>
    Migration
}

/// <summary>
/// The failure kinds of the failure catalogue of an Angular upgrade.
/// </summary>
internal enum UpgradeFailureKind
{
    None,

    Timeout,

    /// <summary>
    /// The npm registry cannot be reached.
    /// </summary>
    RegistryUnreachable,

    /// <summary>
    /// The registry has no such package or version, or answered with an HTTP error (Yarn <c>YN0035</c>).
    /// </summary>
    PackageNotFound,

    /// <summary>
    /// The registry asks for credentials (Yarn <c>YN0041</c>).
    /// </summary>
    Authentication,

    /// <summary>
    /// <c>ng update</c> refused incompatible peer dependencies. Nothing is written by the command.
    /// </summary>
    PeerRefusal,

    /// <summary>
    /// The install run by <c>ng update</c> failed. <c>ng update</c> does not show yarn's output.
    /// </summary>
    InstallInsideNgUpdate,

    /// <summary>
    /// The Angular CLI refused the Node.js version (exit code 3).
    /// </summary>
    NodeRefused,

    /// <summary>
    /// The package provides no migrations. This is information, not a failure of the upgrade.
    /// </summary>
    NoMigrations,

    MigrationFailed,

    Generic
}

/// <summary>
/// One line of an <c>ng update</c> peer refusal: <paramref name="Package"/> requires <paramref name="Peer"/>
/// in <paramref name="Range"/>, and <paramref name="WouldInstall"/> would be installed.
/// </summary>
/// <param name="Package">The package that declares the peer dependency.</param>
/// <param name="Peer">The peer package.</param>
/// <param name="Range">The range <paramref name="Package"/> requires.</param>
/// <param name="WouldInstall">The version of <paramref name="Peer"/> the update would install.</param>
internal sealed record PeerConflict( string Package, string Peer, string Range, string WouldInstall );

/// <summary>
/// A classified failure of an Angular upgrade command.
/// </summary>
/// <param name="Kind">The failure kind.</param>
/// <param name="Details">
/// What the kind was recognised by, ANSI-stripped and trimmed: for a Yarn failure, the messages that follow the
/// <c>YN</c> code (for <see cref="UpgradeFailureKind.PackageNotFound"/> and <see cref="UpgradeFailureKind.Authentication"/>,
/// only those that name a descriptor); for <see cref="UpgradeFailureKind.MigrationFailed"/>, the <c>ERROR!</c> lines of a
/// conflict and the error message; for the other CLI failures, the recognised lines; for <see cref="UpgradeFailureKind.Generic"/>,
/// the last 30 output lines (ANSI-stripped, not trimmed). Empty for <see cref="UpgradeFailureKind.None"/> and
/// <see cref="UpgradeFailureKind.Timeout"/>.
/// </param>
/// <param name="Peers">The conflicts of a <see cref="UpgradeFailureKind.PeerRefusal"/>, empty otherwise.</param>
/// <param name="MigrationName">
/// The title of the migration that failed (the first sentence of its description, as the CLI prints it), or null
/// when no title was printed before the failure.
/// </param>
/// <param name="LogPath">The error log the CLI names in a migration failure, or null.</param>
internal sealed record UpgradeFailure( UpgradeFailureKind Kind,
                                       IReadOnlyList<string> Details,
                                       IReadOnlyList<PeerConflict> Peers,
                                       string? MigrationName,
                                       string? LogPath );

/// <summary>
/// Classifies the output of the commands of an Angular upgrade into the failure catalogue, and describes
/// each failure for the developer. Pure: no I/O.
/// </summary>
internal static partial class UpgradeFailureClassifier
{
    /// <summary>
    /// The timeout of every yarn command of an upgrade. The <see cref="UpgradeFailureKind.Timeout"/> hint names it.
    /// </summary>
    internal static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes( 10 );

    static readonly string[] _pacoteNetworkCodes = ["ENOTFOUND", "ECONNREFUSED", "ETIMEDOUT"];

    /// <summary>
    /// Classifies the output of a command. ANSI codes are stripped first.
    /// </summary>
    /// <param name="command">The kind of command that ran.</param>
    /// <param name="exitCode">The exit code. Ignored when <paramref name="timedOut"/> is true.</param>
    /// <param name="timedOut">Whether the engine timeout expired.</param>
    /// <param name="outputLines">The stdout and stderr lines of the command.</param>
    /// <returns>The classified failure, of kind <see cref="UpgradeFailureKind.None"/> on success.</returns>
    internal static UpgradeFailure Classify( UpgradeCommandKind command, int exitCode, bool timedOut, IReadOnlyList<string> outputLines )
    {
        if( timedOut )
        {
            return Of( UpgradeFailureKind.Timeout, [] );
        }
        if( exitCode == 0 )
        {
            return Of( UpgradeFailureKind.None, [] );
        }
        var raw = outputLines.Select( l => AngularUpgradeReport.AnsiRegex().Replace( l, "" ) ).ToArray();
        var lines = raw.Select( l => l.Trim() ).ToArray();
        if( exitCode == 3 )
        {
            return Of( UpgradeFailureKind.NodeRefused, lines.Where( l => l.StartsWith( "Node.js version ", StringComparison.Ordinal )
                                                                       || l.StartsWith( "The Angular CLI requires a minimum", StringComparison.Ordinal ) ) );
        }
        var yarn = ClassifyYarn( lines );
        if( yarn != null )
        {
            return yarn;
        }
        var registry = lines.Where( IsRegistryUnreachable ).ToArray();
        if( registry.Length > 0 )
        {
            return Of( UpgradeFailureKind.RegistryUnreachable, registry );
        }
        if( exitCode == 1 && lines.Any( l => l.Contains( "Incompatible peer dependencies found", StringComparison.Ordinal ) ) )
        {
            var peers = new List<PeerConflict>();
            var peerLines = new List<string>();
            for( int i = 0; i < raw.Length; i++ )
            {
                var m = PeerLineRegex().Match( raw[i] );
                if( m.Success )
                {
                    // The CLI prints some conflicts twice, the second time with "(extended)" or a final period.
                    var conflict = new PeerConflict( m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value );
                    if( !peers.Contains( conflict ) )
                    {
                        peers.Add( conflict );
                        peerLines.Add( lines[i] );
                    }
                }
            }
            // A summary without a parsable conflict line names nothing to fix: the generic last lines say more.
            if( peers.Count > 0 )
            {
                return new UpgradeFailure( UpgradeFailureKind.PeerRefusal, peerLines, peers, null, null );
            }
        }
        var install = lines.Where( l => l.Contains( "Unable to install packages", StringComparison.Ordinal ) ).ToArray();
        if( install.Length > 0 )
        {
            return Of( UpgradeFailureKind.InstallInsideNgUpdate, install );
        }
        // The package command prints "Migration failed:" for refusals of its update schematic, and is expected to crash
        // with exit 127 in its migration phase under PnP: neither is a failure of a migration.
        if( command == UpgradeCommandKind.Migration )
        {
            var migration = ClassifyMigration( exitCode, lines );
            if( migration != null )
            {
                return migration;
            }
        }
        return Of( UpgradeFailureKind.Generic, AngularUpgradeReport.LastLines( outputLines ) );
    }

    static UpgradeFailure Of( UpgradeFailureKind kind, IEnumerable<string> details ) => new UpgradeFailure( kind, details.ToArray(), [], null, null );

    static UpgradeFailure? ClassifyYarn( string[] lines )
    {
        // The first recognised Yarn error decides the kind; its Details are the matching messages of the same code.
        foreach( var line in lines )
        {
            if( TryGetYarnMessage( line, "YN0001", out var m ) && IsYarnNetworkError( m ) )
            {
                return Of( UpgradeFailureKind.RegistryUnreachable, Messages( "YN0001" ).Where( IsYarnNetworkError ) );
            }
            if( TryGetYarnMessage( line, "YN0035", out _ ) )
            {
                return Of( UpgradeFailureKind.PackageNotFound, Messages( "YN0035" ).Where( NamesDescriptor ) );
            }
            if( TryGetYarnMessage( line, "YN0041", out _ ) )
            {
                return Of( UpgradeFailureKind.Authentication, Messages( "YN0041" ).Where( NamesDescriptor ) );
            }
        }
        return null;

        IEnumerable<string> Messages( string code )
        {
            foreach( var line in lines )
            {
                if( TryGetYarnMessage( line, code, out var m ) )
                {
                    yield return m;
                }
            }
        }

        static bool IsYarnNetworkError( string message )
        {
            // With the default httpRetry, a DNS failure surfaces as ERR_SOCKET_CLOSED_BEFORE_CONNECTION, not as a RequestError.
            return message.StartsWith( "RequestError:", StringComparison.Ordinal )
                   || message.Contains( "ERR_SOCKET_CLOSED_BEFORE_CONNECTION", StringComparison.Ordinal );
        }

        // A descriptor message is "<ident>@<range>: <reason>"; the detail lines ("Response Code: 404") hold no '@' before ": ".
        static bool NamesDescriptor( string message )
        {
            int colon = message.IndexOf( ": ", StringComparison.Ordinal );
            return colon > 0 && message.AsSpan( 0, colon ).Contains( '@' );
        }
    }

    static bool TryGetYarnMessage( string line, string code, out string message )
    {
        int idx = line.IndexOf( code + ":", StringComparison.Ordinal );
        if( idx < 0 )
        {
            message = "";
            return false;
        }
        // Yarn indents the messages of a report section with U+2502 (box drawing light vertical) and a space.
        message = line.Substring( idx + code.Length + 1 ).TrimStart( ' ', '\u2502' );
        return true;
    }

    static bool IsRegistryUnreachable( string line )
    {
        if( line.StartsWith( "Error fetching manifest for '", StringComparison.Ordinal ) )
        {
            return true;
        }
        // CLI 20 reports a pacote network error through its own metadata fetch or through the update schematic.
        if( line.StartsWith( "Error fetching metadata for '", StringComparison.Ordinal )
            || line.Contains( "Migration failed:", StringComparison.Ordinal ) )
        {
            return (line.Contains( "request to ", StringComparison.Ordinal ) && line.Contains( " failed, reason:", StringComparison.Ordinal ))
                   || _pacoteNetworkCodes.Any( c => line.Contains( c, StringComparison.Ordinal ) );
        }
        return false;
    }

    static UpgradeFailure? ClassifyMigration( int exitCode, string[] lines )
    {
        const string noMigrations = "Package does not provide migrations.";
        if( exitCode == 1 && lines.Any( l => l.Contains( noMigrations, StringComparison.Ordinal ) ) )
        {
            return Of( UpgradeFailureKind.NoMigrations, [noMigrations] );
        }
        var marker = exitCode switch
        {
            1 => "Migration failed",
            127 => "An unhandled exception occurred:",
            _ => null
        };
        if( marker == null )
        {
            return null;
        }
        int failed = Array.FindIndex( lines, l => l.Contains( marker, StringComparison.Ordinal ) );
        if( failed < 0 )
        {
            return null;
        }
        // A conflict prints "ERROR! <path> ..." lines, then "Migration failed. See above for further details.".
        var details = lines.Take( failed ).Where( l => l.StartsWith( "ERROR! ", StringComparison.Ordinal ) ).ToList();
        var rest = lines[failed].Substring( lines[failed].IndexOf( marker, StringComparison.Ordinal ) + marker.Length );
        // "Migration failed." starts the conflict shape and carries no message; a message may itself start with '.'.
        if( !rest.StartsWith( '.' ) )
        {
            var message = (rest.StartsWith( ':' ) ? rest.Substring( 1 ) : rest).Trim();
            if( message.Length > 0 )
            {
                details.Add( message );
            }
        }
        string? title = null;
        for( int i = failed - 1; i >= 0 && !lines[i].StartsWith( "** Executing migrations of package ", StringComparison.Ordinal ); i-- )
        {
            // The listr2 pointer is U+276F, or '>' when listr2 does not detect unicode support.
            if( lines[i].StartsWith( "\u276F ", StringComparison.Ordinal ) || lines[i].StartsWith( "> ", StringComparison.Ordinal ) )
            {
                title = lines[i].Substring( 2 ).Trim();
                break;
            }
        }
        string? logPath = null;
        for( int i = failed; i < lines.Length && logPath == null; i++ )
        {
            var m = LogPathRegex().Match( lines[i] );
            if( m.Success )
            {
                logPath = m.Groups[1].Value;
            }
        }
        return new UpgradeFailure( UpgradeFailureKind.MigrationFailed, details.ToArray(), [], title, logPath );
    }

    /// <summary>
    /// Describes a failure for the developer: the "Hint" column of the failure catalogue, with the data it names
    /// (descriptors, peer conflicts, migration and log path, the last output lines of a generic failure).
    /// The command and its exit code are not part of it: the caller states them.
    /// </summary>
    /// <param name="failure">The failure to describe.</param>
    /// <param name="stepToMajor">The Angular major of the step that failed.</param>
    /// <param name="updatedPackages">
    /// The packages of the step's package command. Every <c>@angular/*</c> package also counts as updated, since the
    /// CLI updates the whole <c>@angular/core</c> package group.
    /// </param>
    /// <param name="findDeclarer">
    /// Returns the CK type that declares a package, <see cref="PackageDependency.ConfigurationSourceName"/> when a
    /// <c>LibraryVersions</c> entry sets it, or null when only the app's package.json declares it.
    /// </param>
    /// <returns>The hint text, empty for <see cref="UpgradeFailureKind.None"/>.</returns>
    internal static string DescribeHint( UpgradeFailure failure,
                                         int stepToMajor,
                                         IReadOnlySet<string> updatedPackages,
                                         Func<string, string?> findDeclarer )
    {
        var b = new StringBuilder();
        switch( failure.Kind )
        {
            case UpgradeFailureKind.None:
                break;
            case UpgradeFailureKind.Timeout:
                b.Append( $"The command ran over {CommandTimeout.TotalMinutes} minutes: the registry is slow or a process hung. Rerun the builder." );
                break;
            case UpgradeFailureKind.RegistryUnreachable:
                b.Append( "This machine cannot reach the npm registry" );
                if( failure.Details.Count > 0 )
                {
                    b.Append( " (" ).Append( failure.Details[0] ).Append( ')' );
                }
                b.Append( ". Check the network and the proxy settings, then rerun the builder." );
                var manifestPackage = failure.Details.Select( GetManifestPackage ).FirstOrDefault( p => p != null );
                if( manifestPackage != null )
                {
                    b.Append( $" If the network is fine, run 'yarn npm info {manifestPackage}' in the app folder to see why: the Angular CLI prints this message for a missing version or a refused login too." );
                }
                break;
            case UpgradeFailureKind.PackageNotFound:
                b.Append( "The registry did not provide a package" );
                AppendDescriptors( b, failure.Details );
                b.Append( ". A declared version does not exist, or the registry failed: fix the version, or rerun the builder later." );
                break;
            case UpgradeFailureKind.Authentication:
                b.Append( "The registry asks for credentials" );
                AppendDescriptors( b, failure.Details );
                b.Append( ". Configure the credentials of this registry, then rerun the builder." );
                break;
            case UpgradeFailureKind.PeerRefusal:
                DescribePeers( b, failure.Peers, stepToMajor, updatedPackages, findDeclarer );
                break;
            case UpgradeFailureKind.InstallInsideNgUpdate:
                b.Append( "ng update could not install the packages and does not show yarn's output. Rerun the builder; when it fails again, run the manual commands to see yarn's own message." );
                break;
            case UpgradeFailureKind.NodeRefused:
                b.Append( "The Angular CLI refused this Node.js version" );
                if( failure.Details.Count > 0 )
                {
                    b.Append( ": " ).AppendJoin( " ", failure.Details );
                }
                else
                {
                    b.Append( '.' );
                }
                b.Append( " Install a supported Node.js version (https://angular.dev/reference/versions), then rerun the builder." );
                break;
            case UpgradeFailureKind.NoMigrations:
                b.Append( "This package provides no ng update migrations: there is nothing to run." );
                break;
            case UpgradeFailureKind.MigrationFailed:
                b.Append( failure.MigrationName != null ? $"The migration '{failure.MigrationName}' failed" : "A migration failed" );
                if( failure.Details.Count > 0 )
                {
                    b.Append( ": " ).AppendJoin( " ", failure.Details );
                }
                if( b[^1] != '.' )
                {
                    b.Append( '.' );
                }
                if( failure.LogPath != null )
                {
                    b.Append( " Its log is '" ).Append( failure.LogPath ).Append( "'." );
                }
                // "<a> tried to access <b>, but it isn't declared in its dependencies" is Yarn PnP refusing a module load.
                if( failure.Details.Any( d => d.Contains( "tried to access", StringComparison.Ordinal ) ) )
                {
                    b.Append( " A migration cannot load a package it needs: the cause is the upgrade tooling, not the app. Report it to CK, and upgrade by hand with the manual commands meanwhile." );
                }
                else
                {
                    b.Append( " The migration cannot handle this app's code: upgrade by hand with the manual commands, fixing what the migration stops on, or report it to the package's maintainers." );
                }
                break;
            default:
                b.Append( "The output matches no known failure. Its last lines:" );
                foreach( var line in failure.Details )
                {
                    b.AppendLine().Append( "  " ).Append( line );
                }
                b.AppendLine().Append( "The full output is in the build log." );
                break;
        }
        return b.ToString();
    }

    static void DescribePeers( StringBuilder b,
                               IReadOnlyList<PeerConflict> peers,
                               int stepToMajor,
                               IReadOnlySet<string> updatedPackages,
                               Func<string, string?> findDeclarer )
    {
        b.Append( "ng update refused incompatible peer dependencies; nothing was written by this command." );
        var angularMajors = peers.Where( p => IsAngular( p.Peer ) )
                                 .Select( p => GetMajor( p.WouldInstall ) )
                                 .OfType<int>()
                                 .Distinct()
                                 .Order()
                                 .ToList();
        // A split Angular group explains every conflict: blaming each package would send the developer the wrong way.
        bool split = angularMajors.Count > 1;
        if( split )
        {
            b.AppendLine().Append( $"ng update would install Angular packages from majors {string.Join( " and ", angularMajors )}: a peer range of a package pulled part of the @angular/core group to the next major. This is a defect of the upgrade tooling: report it to CK, and upgrade by hand with the manual commands meanwhile." );
        }
        foreach( var p in peers )
        {
            b.AppendLine().Append( $"'{p.Package}' needs '{p.Peer}' {p.Range}, and {p.WouldInstall} would be installed." );
            if( split )
            {
                continue;
            }
            b.Append( ' ' );
            // The CLI checks the peers of the packages it updates and the installed packages that peer on them, so one
            // side is updated: the other one blocks. When both are, the requested release of Package does.
            bool peerBlocks = IsUpdated( p.Package ) && !IsUpdated( p.Peer );
            var culprit = peerBlocks ? p.Peer : p.Package;
            var declarer = findDeclarer( culprit );
            if( IsAngular( culprit ) )
            {
                b.Append( $"'{culprit}' is an Angular package the upgrade moves to Angular {stepToMajor} itself: report it to CK, and upgrade by hand with the manual commands meanwhile." );
            }
            else if( declarer == PackageDependency.ConfigurationSourceName )
            {
                b.Append( $"'{culprit}' is set by the LibraryVersions entry '{culprit}': update or remove that entry, or stay on the previous CK major." );
            }
            else if( declarer != null )
            {
                b.Append( $"'{culprit}' is declared by the CK package '{declarer}', which is not ready for Angular {stepToMajor}: report it to its maintainers, or stay on the previous CK major." );
            }
            else if( peerBlocks )
            {
                b.Append( $"To fix: set '{culprit}' in the app's package.json to a version in {p.Range}, then rerun the builder." );
            }
            else
            {
                b.Append( $"To fix: set '{culprit}' in the app's package.json to a release whose peer range accepts '{p.Peer}' {p.WouldInstall}, then rerun the builder." );
                // The CLI widens a peer range on the @angular/core package group by one major, and no other peer range.
                if( p.Peer == "@angular/core" )
                {
                    b.Append( $" Its release for Angular {stepToMajor - 1} is enough: the CLI accepts @angular/core one major above a library's peer range." );
                }
            }
        }

        // The CLI updates the whole @angular/core package group, which the step's package command does not list.
        bool IsUpdated( string name ) => updatedPackages.Contains( name ) || IsAngular( name );

        static bool IsAngular( string name ) => name.StartsWith( "@angular/", StringComparison.Ordinal );

        static int? GetMajor( string version )
        {
            int dot = version.IndexOf( '.' );
            return int.TryParse( dot < 0 ? version : version.Substring( 0, dot ), out var major ) ? major : null;
        }
    }

    static void AppendDescriptors( StringBuilder b, IReadOnlyList<string> details )
    {
        if( details.Count > 0 )
        {
            b.Append( ": " ).AppendJoin( "; ", details );
        }
    }

    static string? GetManifestPackage( string line )
    {
        const string prefix = "Error fetching manifest for '";
        if( !line.StartsWith( prefix, StringComparison.Ordinal ) )
        {
            return null;
        }
        int end = line.IndexOf( '\'', prefix.Length );
        return end > prefix.Length ? line.Substring( prefix.Length, end - prefix.Length ) : null;
    }

    [GeneratedRegex( @"^\s*Package ""([^""]+)"" has an incompatible peer dependency to ""([^""]+)"" \(requires ""([^""]+)""(?: \(extended\))?, would install ""([^""]+)""\)\.?\s*$" )]
    private static partial Regex PeerLineRegex();

    [GeneratedRegex( @"See ""([^""]+)"" for further details" )]
    private static partial Regex LogPathRegex();
}
