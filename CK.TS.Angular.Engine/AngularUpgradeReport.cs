using CK.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CK.TS.Angular.Engine;

/// <summary>
/// The outcome of the compile check that follows an Angular upgrade.
/// </summary>
internal enum CompileCheckOutcome
{
    /// <summary>
    /// The check ran and reported no error.
    /// </summary>
    Passed,

    /// <summary>
    /// The check reported <c>error TS</c> or <c>error NG</c> lines.
    /// </summary>
    Errors,

    /// <summary>
    /// The check gave no answer: it timed out, or exited non-zero without an error line. This never reads as a pass.
    /// </summary>
    DidNotRun
}

/// <summary>
/// The result of the compile check that follows an Angular upgrade.
/// </summary>
/// <param name="Outcome">The outcome.</param>
/// <param name="Errors">The compile errors, ANSI-stripped. Empty unless <see cref="CompileCheckOutcome.Errors"/>.</param>
/// <param name="NotRunReason">
/// Why the check did not run (<c>timed out after 10 minutes</c> or <c>exit code &lt;n&gt;</c>), or null unless
/// <see cref="CompileCheckOutcome.DidNotRun"/>.
/// </param>
/// <param name="LastLines">The last 30 output lines, ANSI-stripped.</param>
internal sealed record CompileCheckResult( CompileCheckOutcome Outcome, IReadOnlyList<string> Errors, string? NotRunReason, IReadOnlyList<string> LastLines );

/// <summary>
/// The compile check and the summary that follow the integration of an upgraded Angular app, and the message of an
/// integration that failed after the upgrade. Pure: no I/O.
/// </summary>
internal static partial class AngularUpgradeReport
{
    const int _lastLineCount = 30;

    const int _maxErrorLines = 50;

    /// <summary>
    /// The compile check command, without the <c>yarn </c> prefix. The Angular compiler reports the template errors
    /// that <c>tsc</c> does not see.
    /// </summary>
    internal const string CompileCheckCommand = "ngc -p tsconfig.app.json --noEmit";

    /// <summary>
    /// Gets the link to the Angular update guide from <paramref name="fromMajor"/> to <paramref name="toMajor"/>, at the
    /// advanced level. The guide shows another range when a version is not in its own list.
    /// </summary>
    /// <param name="fromMajor">The Angular major before the upgrade.</param>
    /// <param name="toMajor">The Angular major reached.</param>
    /// <returns>The link.</returns>
    internal static string GuideLink( int fromMajor, int toMajor ) => $"https://angular.dev/update-guide?v={fromMajor}.0-{toMajor}.0&l=3";

    /// <summary>
    /// Gets the compile errors of the compile check output: the lines that contain <c>error TS</c> or <c>error NG</c>
    /// once ANSI codes are stripped. <c>ngc</c> colours its output even with <c>NO_COLOR=1</c>, and the codes split
    /// <c>error</c> from the error code.
    /// </summary>
    /// <param name="outputLines">The output lines of the compile check.</param>
    /// <returns>The error lines, ANSI-stripped.</returns>
    internal static IReadOnlyList<string> GetCompileErrors( IReadOnlyList<string> outputLines )
    {
        return outputLines.Select( l => AnsiRegex().Replace( l, "" ) )
                          .Where( l => l.Contains( "error TS", StringComparison.Ordinal ) || l.Contains( "error NG", StringComparison.Ordinal ) )
                          .ToList();
    }

    /// <summary>
    /// Evaluates the compile check: a timeout did not run; any <see cref="GetCompileErrors"/> line is
    /// <see cref="CompileCheckOutcome.Errors"/>; a non-zero exit code without such a line did not run; otherwise it passed.
    /// </summary>
    /// <param name="exitCode">The exit code. Ignored when <paramref name="timedOut"/> is true.</param>
    /// <param name="timedOut">Whether the engine timeout expired.</param>
    /// <param name="outputLines">The stdout and stderr lines of the check.</param>
    /// <returns>The result.</returns>
    internal static CompileCheckResult EvaluateCompileCheck( int exitCode, bool timedOut, IReadOnlyList<string> outputLines )
    {
        var lastLines = LastLines( outputLines );
        if( timedOut )
        {
            return new CompileCheckResult( CompileCheckOutcome.DidNotRun, [], $"timed out after {(int)UpgradeFailureClassifier.CommandTimeout.TotalMinutes} minutes", lastLines );
        }
        var errors = GetCompileErrors( outputLines );
        if( errors.Count > 0 )
        {
            return new CompileCheckResult( CompileCheckOutcome.Errors, errors, null, lastLines );
        }
        if( exitCode != 0 )
        {
            return new CompileCheckResult( CompileCheckOutcome.DidNotRun, [], $"exit code {exitCode}", lastLines );
        }
        return new CompileCheckResult( CompileCheckOutcome.Passed, [], null, lastLines );
    }

    /// <summary>
    /// Renders the message of a compile check that reported errors, in four parts: the check that ran, the error count
    /// and the first 50 error lines grouped by file (in the order files first appear, each matching hint on the line
    /// after its error, a file under <c>ck-gen</c> said to be fixed in its CK package), the app folder state (upgraded,
    /// not undone) and the next actions.
    /// </summary>
    /// <param name="appPath">The app folder.</param>
    /// <param name="errors">The errors, from <see cref="GetCompileErrors"/>.</param>
    /// <param name="hints">The hints of the rows crossed by the upgrade.</param>
    /// <param name="guideLink">The link from <see cref="GuideLink"/>.</param>
    /// <returns>The message.</returns>
    internal static string RenderCompileFailure( NormalizedPath appPath, IReadOnlyList<string> errors, IReadOnlyList<AngularUpgradeHint> hints, string guideLink )
    {
        var b = new StringBuilder();
        b.AppendLine( $"Checking the upgraded Angular app '{appPath}': running 'yarn {CompileCheckCommand}' in the app folder." );
        var count = Plural( errors.Count, "error" );
        b.AppendLine( errors.Count > _maxErrorLines
                        ? $"The compile check found {count}. The first {_maxErrorLines}, grouped by file:"
                        : $"The compile check found {count}, grouped by file:" );
        var shown = errors.GroupBy( GetErrorFile ).SelectMany( g => g ).Take( _maxErrorLines );
        string? file = null;
        foreach( var error in shown )
        {
            var errorFile = GetErrorFile( error );
            if( errorFile != file )
            {
                file = errorFile;
                b.AppendLine( file.Length == 0
                                ? "Errors without a file:"
                                : ("/" + file.Replace( '\\', '/' )).Contains( "/ck-gen/", StringComparison.Ordinal )
                                    ? $"{file} (generated from a CK package: fix it in that package, the next build overwrites this copy):"
                                    : $"{file}:" );
            }
            b.AppendLine( $"  {error}" );
            foreach( var hint in hints.Where( h => Matches( h, error ) ) )
            {
                b.AppendLine( $"    Hint: {hint.Text}" );
            }
        }
        if( errors.Count > _maxErrorLines )
        {
            b.AppendLine( $"And {Plural( errors.Count - _maxErrorLines, "more error" )}: run the command in the app folder to see them all." );
        }
        // ngc type-checks the templates only when no earlier stage reported an error, and every TS error is from one.
        if( errors.Any( e => e.Contains( "error TS", StringComparison.Ordinal ) ) )
        {
            b.AppendLine( "While TypeScript errors (TS codes) remain, the Angular compiler does not check the templates: more errors (NG codes) can show once these are fixed." );
        }
        b.AppendLine( $"The app folder '{appPath}' stays upgraded: the upgrade is not undone, fixing the app code is up to you." );
        b.AppendLine( "Next actions:" );
        b.AppendLine( $"1. Fix these errors before committing: a rerun of the builder passes without this check, which runs only after an upgrade. Run 'yarn {CompileCheckCommand}' in the app folder to check again." );
        b.Append( $"2. Review the diff with the Angular update guide, for the changes no compiler sees: {guideLink} (tick 'I use Angular Material' if the app uses it)." );
        return b.ToString();
    }

    /// <summary>
    /// Renders the message of a compile check that did not run, in four parts: the check that ran, why it gave no
    /// answer with its last output lines, the app folder state (upgraded, not checked) and the next actions.
    /// </summary>
    /// <param name="appPath">The app folder.</param>
    /// <param name="command">The compile check command, without the <c>yarn </c> prefix.</param>
    /// <param name="result">The <see cref="CompileCheckOutcome.DidNotRun"/> result.</param>
    /// <param name="guideLink">The link from <see cref="GuideLink"/>.</param>
    /// <returns>The message.</returns>
    internal static string RenderCompileCheckNotRun( NormalizedPath appPath, string command, CompileCheckResult result, string guideLink )
    {
        var b = new StringBuilder();
        b.AppendLine( $"Checking the upgraded Angular app '{appPath}': running 'yarn {command}' in the app folder." );
        if( result.LastLines.Count > 0 )
        {
            b.AppendLine( $"The upgrade is done, but the compile check did not run ({result.NotRunReason}). Its last output lines:" );
            foreach( var line in result.LastLines )
            {
                b.AppendLine( $"  {line}" );
            }
        }
        else
        {
            b.AppendLine( $"The upgrade is done, but the compile check did not run ({result.NotRunReason}). It printed nothing." );
        }
        b.AppendLine( $"The app folder '{appPath}' stays upgraded, but its code has not been checked." );
        b.AppendLine( "Next actions:" );
        b.AppendLine( $"1. Run 'yarn {command}' in the app folder and fix what it reports before committing: a rerun of the builder does not repeat the check." );
        b.Append( $"2. Review the diff with the Angular update guide, for the changes no compiler sees: {guideLink} (tick 'I use Angular Material' if the app uses it)." );
        return b.ToString();
    }

    /// <summary>
    /// Renders the summary of an upgrade: the majors reached, the steps done and the elapsed time, the files changed,
    /// the compile check result (<c>passed</c>, <c>&lt;n&gt; errors</c> or <c>did not run (&lt;reason&gt;)</c>), the hints
    /// of the rows crossed, the optional migrations not run with their commands, the packages without migrations, the
    /// replaced packages, the update guide link and how to undo the upgrade: the stash command of the in-run undo
    /// (<see cref="AngularGitHelper.StashPushArguments"/>), then <c>yarn install</c>. Repeated optional migrations and
    /// packages are listed once.
    /// </summary>
    /// <param name="run">The upgrade run. Its <see cref="AngularUpgradeRun.Elapsed"/> is the elapsed time shown.</param>
    /// <param name="changedFiles">The <c>git status --porcelain</c> lines of the app folder, or null when git did not answer.</param>
    /// <param name="compileCheck">The compile check result.</param>
    /// <param name="stashMessage">The stash message of the printed undo command.</param>
    /// <param name="excludeRootGitIgnore">
    /// Whether the app root <c>.gitignore</c> is untracked, so that the printed stash command excludes it as the
    /// in-run undo does.
    /// </param>
    /// <returns>The summary.</returns>
    internal static string RenderSummary( AngularUpgradeRun run,
                                          IReadOnlyList<string>? changedFiles,
                                          CompileCheckResult compileCheck,
                                          string stashMessage,
                                          bool excludeRootGitIgnore )
    {
        var b = new StringBuilder();
        var elapsed = run.Elapsed;
        b.AppendLine( $"The Angular app '{run.AppPath}' has been upgraded from Angular {run.FromMajor} to {run.ToMajor}. Elapsed time: {(int)elapsed.TotalMinutes} min {elapsed.Seconds} s." );
        b.AppendLine( $"Steps: {string.Join( ", ", run.Steps.Select( s => $"{s.FromMajor} to {s.ToMajor}" ) )}." );
        b.AppendLine( compileCheck.Outcome switch
        {
            CompileCheckOutcome.Passed => "Compile check: passed.",
            CompileCheckOutcome.Errors => $"Compile check: {Plural( compileCheck.Errors.Count, "error" )}.",
            _ => $"Compile check: did not run ({compileCheck.NotRunReason})."
        } );
        if( changedFiles == null )
        {
            b.AppendLine( "Files changed: unknown, git did not answer (see the warning above)." );
        }
        else if( changedFiles.Count == 0 )
        {
            b.AppendLine( "Files changed: none." );
        }
        else
        {
            b.AppendLine( $"Files changed ({changedFiles.Count}):" );
            foreach( var file in changedFiles )
            {
                b.AppendLine( $"  {file}" );
            }
        }
        var hints = run.Steps.SelectMany( s => s.Tooling.Hints.Select( h => $"Angular {s.ToMajor}: {h.Text}" ) ).ToList();
        if( hints.Count > 0 )
        {
            b.AppendLine( "To know about this upgrade:" );
            foreach( var hint in hints )
            {
                b.AppendLine( $"  {hint}" );
            }
        }
        var optional = run.OptionalMigrations.Distinct().ToList();
        if( optional.Count > 0 )
        {
            b.AppendLine( "Optional migrations not run: run in the app folder each one you want." );
            foreach( var (package, name) in optional )
            {
                b.AppendLine( $"  yarn ng update {package} --name {name}" );
            }
        }
        var withoutMigrations = run.PackagesWithoutMigrations.Distinct().ToList();
        if( withoutMigrations.Count > 0 )
        {
            b.AppendLine( $"Packages without migrations: {string.Join( ", ", withoutMigrations )}." );
        }
        var replaced = run.Steps.SelectMany( s => s.ReplacedPackages.Select( r => $"'{r.Package}' replaced by '{r.Replacement}' (Angular {s.ToMajor})." ) ).ToList();
        if( replaced.Count > 0 )
        {
            b.AppendLine( "Replaced packages:" );
            foreach( var line in replaced )
            {
                b.AppendLine( $"  {line}" );
            }
        }
        b.AppendLine( $"Angular update guide, for the changes no compiler sees: {GuideLink( run.FromMajor, run.ToMajor )} (tick 'I use Angular Material' if the app uses it)." );
        b.AppendLine( "Review the diff before committing. To undo the upgrade, run these commands in the app folder, in PowerShell: the stash keeps the changes (find it with 'git stash list')." );
        b.AppendLine( "git " + AngularGitHelper.StashPushArguments( stashMessage, excludeRootGitIgnore ) );
        b.Append( "yarn install" );
        return b.ToString();
    }

    /// <summary>
    /// Renders, through <see cref="AngularUpgradeFailureMessage.Render"/>, the message of an integration that failed
    /// after a successful upgrade, once the upgrade has been undone.
    /// </summary>
    /// <param name="appPath">The app folder.</param>
    /// <param name="fromMajor">The Angular major before the upgrade.</param>
    /// <param name="toMajor">The Angular major the upgrade reached.</param>
    /// <param name="stashMessage">The message of the undo stash.</param>
    /// <param name="undo">The outcome of the undo.</param>
    /// <param name="stashCreated">Whether the undo created a stash.</param>
    /// <param name="excludeRootGitIgnore">Whether the printed stash command excludes the app root <c>.gitignore</c>.</param>
    /// <returns>The message.</returns>
    internal static string RenderIntegrationFailure( NormalizedPath appPath,
                                                     int fromMajor,
                                                     int toMajor,
                                                     string stashMessage,
                                                     AngularUndoOutcome undo,
                                                     bool stashCreated,
                                                     bool excludeRootGitIgnore )
    {
        var rerun = $"fix the cause of the integration error logged above, then rerun the builder: it upgrades the app again from Angular {fromMajor}.";
        return AngularUpgradeFailureMessage.Render( $"Integrating the Angular app '{appPath}' after its upgrade from Angular {fromMajor} to {toMajor}.",
                                                    "The integration failed after the upgrade: the upgrade itself worked, but the integration that follows it failed (its own error is logged above).",
                                                    appPath,
                                                    stashMessage,
                                                    undo,
                                                    stashCreated,
                                                    excludeRootGitIgnore,
                                                    [undo == AngularUndoOutcome.NotRestored
                                                        ? $"Run the commands above in the app folder, {rerun}"
                                                        : char.ToUpperInvariant( rerun[0] ) + rerun[1..]] );
    }

    /// <summary>
    /// Gets the last 30 lines of a command output, ANSI-stripped.
    /// </summary>
    /// <param name="output">The output lines.</param>
    /// <returns>The last lines.</returns>
    internal static List<string> LastLines( IReadOnlyList<string> output ) => output.Skip( Math.Max( 0, output.Count - _lastLineCount ) ).Select( l => AnsiRegex().Replace( l, "" ) ).ToList();

    /// <summary>
    /// Matches the ANSI colour codes of a command output.
    /// </summary>
    [GeneratedRegex( @"\x1b\[[0-9;]*m" )]
    internal static partial Regex AnsiRegex();

    // ngc prefixes an error with its location, "<file>:<line>:<column> - ". An error without one has an empty file.
    static string GetErrorFile( string error )
    {
        var m = ErrorLocationRegex().Match( error );
        return m.Success ? m.Groups[1].Value : "";
    }

    static bool Matches( AngularUpgradeHint hint, string error )
    {
        if( hint.MatchFragment == null && hint.MatchErrorCode == null )
        {
            return false;
        }
        return (hint.MatchFragment == null || error.Contains( hint.MatchFragment, StringComparison.Ordinal ))
               && (hint.MatchErrorCode == null || Regex.IsMatch( error, $@"\berror {Regex.Escape( hint.MatchErrorCode )}\b" ));
    }

    static string Plural( int count, string noun ) => count.ToString( CultureInfo.InvariantCulture ) + " " + (count == 1 ? noun : noun + "s");

    [GeneratedRegex( @"^\s*(.+?):\d+:\d+ - " )]
    private static partial Regex ErrorLocationRegex();
}
