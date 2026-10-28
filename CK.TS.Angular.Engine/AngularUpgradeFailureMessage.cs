using CK.Core;
using System.Collections.Generic;
using System.Text;

namespace CK.TS.Angular.Engine;

/// <summary>
/// The outcome of the undo of a failed Angular upgrade.
/// </summary>
internal enum AngularUndoOutcome
{
    /// <summary>
    /// The undo stash holds the changes, if there were any, and <c>yarn install</c> ran on the restored files.
    /// </summary>
    Restored,

    /// <summary>
    /// The undo stash holds the changes, if there were any, but <c>yarn install</c> failed: the installed packages may
    /// not match the restored files.
    /// </summary>
    RestoredInstallFailed,

    /// <summary>
    /// The stash failed: the app folder may still hold the changes of the upgrade.
    /// </summary>
    NotRestored
}

/// <summary>
/// The message of an Angular upgrade that failed after its first write.
/// </summary>
internal static class AngularUpgradeFailureMessage
{
    /// <summary>
    /// Renders the four parts of an upgrade failure message: what the engine was doing, what it found, the app
    /// folder state and the numbered next actions.
    /// </summary>
    /// <param name="doing">What the engine was doing: the step and the command.</param>
    /// <param name="found">What it found: the failure and its data.</param>
    /// <param name="appPath">The app folder.</param>
    /// <param name="stashMessage">The message of the undo stash.</param>
    /// <param name="undo">The outcome of the undo.</param>
    /// <param name="stashCreated">Whether the undo created a stash, as <see cref="AngularGitHelper.StashUndo"/> reports it.</param>
    /// <param name="excludeRootGitIgnore">Whether the printed stash command excludes the app root <c>.gitignore</c>.</param>
    /// <param name="nextActions">The next actions, the recommended one first. They are numbered here.</param>
    /// <returns>The message.</returns>
    internal static string Render( string doing,
                                   string found,
                                   NormalizedPath appPath,
                                   string stashMessage,
                                   AngularUndoOutcome undo,
                                   bool stashCreated,
                                   bool excludeRootGitIgnore,
                                   IReadOnlyList<string> nextActions )
    {
        var b = new StringBuilder();
        b.AppendLine( doing );
        b.AppendLine( found );
        const string install = "run 'yarn install' in the folder, or run the builder again, once the registry is reachable.";
        // The quotes keep PowerShell from reading the braces of the stash reference as a script block.
        var stash = $"The changes of the upgrade, and any edit made in the folder since the build started, are kept in the stash '{stashMessage}': find it with 'git stash list', and remove it with 'git stash drop \"stash@{{<n>}}\"' once you have no use for it.";
        var asItWas = $"The app folder '{appPath}' is as it was: the upgrade had changed no tracked file, so the undo created no stash.";
        if( undo == AngularUndoOutcome.Restored )
        {
            b.AppendLine( stashCreated ? $"The app folder '{appPath}' has been restored. {stash}" : asItWas );
        }
        else if( undo == AngularUndoOutcome.RestoredInstallFailed )
        {
            b.AppendLine( stashCreated
                            ? $"The files of the app folder '{appPath}' have been restored, but 'yarn install' failed, so the installed packages still match the upgrade: {install} {stash}"
                            : $"{asItWas} But 'yarn install' failed, so the installed packages may not match the app: {install}" );
        }
        else
        {
            b.AppendLine( $"The app folder '{appPath}' has not been restored. Run these commands in it, in PowerShell, to undo the upgrade:" );
            b.AppendLine( "git " + AngularGitHelper.StashPushArguments( stashMessage, excludeRootGitIgnore ) );
            b.AppendLine( "yarn install" );
            b.AppendLine( $"Remove-Item {AngularUpgradeMarker.FileName}" );
        }
        b.Append( "Next actions:" );
        for( int i = 0; i < nextActions.Count; i++ )
        {
            b.AppendLine().Append( $"{i + 1}. {nextActions[i]}" );
        }
        return b.ToString();
    }
}
