using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CK.TS.Angular.Engine;

/// <summary>
/// The git commands of an Angular upgrade: the check that the app folder has no uncommitted change, and the undo.
/// Both skip the paths the integration writes before the check (<c>ck-gen</c>, <c>.yarn</c>, <c>.yarnrc.yml</c>).
/// </summary>
internal static class AngularGitHelper
{
    /// <summary>
    /// The timeout of every git command. Its expiry is handled like a non-zero exit code.
    /// </summary>
    internal static readonly TimeSpan GitTimeout = TimeSpan.FromMinutes( 1 );

    static readonly TimeSpan _outputEndTimeout = TimeSpan.FromSeconds( 5 );

    const string _excludes = "\":(exclude)ck-gen\" \":(exclude).yarn\" \":(exclude).yarnrc.yml\"";

    /// <summary>
    /// Runs a process in a folder and captures its standard output and error lines.
    /// </summary>
    /// <param name="folder">The working directory.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="timeout">
    /// The process tree is killed when the process still runs after it. The end of the output is then awaited for
    /// 5 seconds at most, as it is after a normal exit.
    /// </param>
    /// <param name="fileName">The program to run.</param>
    /// <returns>
    /// The exit code, whether the process has been killed by the timeout, and the output lines read until the end of
    /// the output or of the 5 seconds wait. The exit code is -1 when the killed process has still not ended.
    /// </returns>
    internal static (int ExitCode, bool TimedOut, List<string> Output) Run( NormalizedPath folder, string arguments, TimeSpan timeout, string fileName = "git" )
    {
        var output = new List<string>();
        bool outputClosed = false;
        var info = new ProcessStartInfo( fileName, arguments )
        {
            WorkingDirectory = folder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        using var process = new Process { StartInfo = info };
        process.OutputDataReceived += ( sender, data ) => Collect( data.Data );
        process.ErrorDataReceived += ( sender, data ) => Collect( data.Data );
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        bool timedOut = !process.WaitForExit( timeout );
        if( timedOut )
        {
            process.Kill( entireProcessTree: true );
        }
        // The end of the output is awaited for a bounded time: a process that left the tree can keep the pipes open.
        using( var cts = new CancellationTokenSource( _outputEndTimeout ) )
        {
            try
            {
                process.WaitForExitAsync( cts.Token ).GetAwaiter().GetResult();
            }
            catch( OperationCanceledException )
            {
            }
        }
        // Lines that arrive after this point are dropped: the caller owns the list once this method returns.
        lock( output )
        {
            outputClosed = true;
        }
        return (process.HasExited ? process.ExitCode : -1, timedOut, output);

        void Collect( string? line )
        {
            if( line != null )
            {
                lock( output )
                {
                    if( !outputClosed )
                    {
                        output.Add( line );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the uncommitted changes in <paramref name="folder"/> as <c>git status --porcelain</c> lines, whose paths
    /// are relative to the repository root. An untracked <c>.gitignore</c> directly in the folder is not a change.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="folder">The app folder.</param>
    /// <returns>
    /// The changes, or null (a warning is logged) when the folder is not in a git work tree or git did not answer
    /// within <see cref="GitTimeout"/>.
    /// </returns>
    internal static List<string>? GetDirtyFiles( IActivityMonitor monitor, NormalizedPath folder )
    {
        try
        {
            var prefix = Run( folder, "rev-parse --show-prefix", GitTimeout );
            if( !Succeeded( monitor, folder, "rev-parse --show-prefix", prefix, LogLevel.Warn ) )
            {
                return null;
            }
            // The developer's status.showUntrackedFiles configuration would otherwise hide untracked files.
            var status = Run( folder, "status --porcelain --untracked-files=normal -- . " + _excludes, GitTimeout );
            if( !Succeeded( monitor, folder, "status --porcelain", status, LogLevel.Warn ) )
            {
                return null;
            }
            var ignored = "?? " + (prefix.Output.FirstOrDefault() ?? "").Trim() + ".gitignore";
            return GetPorcelainEntries( monitor, folder, status.Output, ignored );
        }
        catch( Exception ex )
        {
            monitor.Warn( $"Unable to run git in '{folder}'.", ex );
            return null;
        }
    }

    /// <summary>
    /// Gets the porcelain v1 entries (two status characters, a space, a path) of the output of <c>git status</c>,
    /// other than <paramref name="ignoredLine"/>. Every other non empty line, such as a git warning on the error
    /// output, is logged as a warning naming the folder.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="folder">The folder git ran in.</param>
    /// <param name="lines">The output lines.</param>
    /// <param name="ignoredLine">The entry that is not a change.</param>
    /// <returns>The entries.</returns>
    internal static List<string> GetPorcelainEntries( IActivityMonitor monitor,
                                                      NormalizedPath folder,
                                                      IEnumerable<string> lines,
                                                      string ignoredLine )
    {
        var entries = new List<string>();
        foreach( var raw in lines )
        {
            var line = raw.TrimEnd( '\r' );
            if( line.Length == 0 || line == ignoredLine )
            {
                continue;
            }
            if( line.Length > 3 && IsStatus( line[0] ) && IsStatus( line[1] ) && line[2] == ' ' )
            {
                entries.Add( line );
            }
            else
            {
                monitor.Warn( $"'git status' in '{folder}' printed a line that is not a change: {line}" );
            }
        }
        return entries;

        static bool IsStatus( char c ) => " MTADRCU?!".Contains( c );
    }

    /// <summary>
    /// Moves the changes of <paramref name="folder"/>, untracked files included, into a new stash. An untracked
    /// <c>.gitignore</c> directly in the folder stays in place, as <see cref="GetDirtyFiles"/> does not see it.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="folder">The app folder.</param>
    /// <param name="message">The stash message.</param>
    /// <returns>
    /// False (an error is logged) when git failed or did not answer within <see cref="GitTimeout"/>. True without
    /// creating a stash when nothing matches; a real undo always has the untracked marker to stash.
    /// </returns>
    internal static bool StashUndo( IActivityMonitor monitor, NormalizedPath folder, string message )
    {
        try
        {
            var excludes = _excludes;
            if( File.Exists( folder.AppendPart( ".gitignore" ) ) )
            {
                var tracked = Run( folder, "ls-files -- .gitignore", GitTimeout );
                if( !Succeeded( monitor, folder, "ls-files", tracked, LogLevel.Error ) )
                {
                    return false;
                }
                if( tracked.Output.All( string.IsNullOrWhiteSpace ) )
                {
                    excludes += " \":(exclude).gitignore\"";
                }
            }
            var stash = Run( folder, $"stash push --include-untracked -m \"{message}\" -- . {excludes}", GitTimeout );
            return Succeeded( monitor, folder, "stash push", stash, LogLevel.Error );
        }
        catch( Exception ex )
        {
            monitor.Error( $"Unable to run git in '{folder}'.", ex );
            return false;
        }
    }

    static bool Succeeded( IActivityMonitor monitor,
                           NormalizedPath folder,
                           string command,
                           (int ExitCode, bool TimedOut, List<string> Output) result,
                           LogLevel level )
    {
        if( result.TimedOut )
        {
            monitor.Log( level, $"'git {command}' did not answer within {GitTimeout.TotalMinutes} minute in '{folder}'." );
            return false;
        }
        if( result.ExitCode != 0 )
        {
            var reason = command == "rev-parse --show-prefix" ? " The folder is not in a git work tree." : "";
            monitor.Log( level, $"'git {command}' failed with exit code {result.ExitCode} in '{folder}'.{reason}{Environment.NewLine}{string.Join( Environment.NewLine, result.Output )}" );
            return false;
        }
        return true;
    }
}
