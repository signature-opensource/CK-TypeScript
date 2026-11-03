using CK.Core;
using CK.TS.Angular.Engine;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularUpgradeFailureMessageTests
{
    const string _stash = "ck-angular-upgrade undo CK_TS_Angular 2026-10-23T10:00:00Z";

    static readonly NormalizedPath _app = "D:/Dev/MyApp/CK_TS_Angular";

    static string Render( AngularUndoOutcome undo, bool stashCreated = true, bool excludeRootGitIgnore = false )
    {
        return AngularUpgradeFailureMessage.Render( "Upgrading from Angular 20 to 21: yarn ng update @angular/core --migrate-only --from 20.3.15 --to 21 --allow-dirty",
                                                    "The migration 'control-flow' of @angular/core failed.",
                                                    _app,
                                                    _stash,
                                                    undo,
                                                    stashCreated,
                                                    excludeRootGitIgnore,
                                                    ["Fix the app code the migration stops on, then rerun the builder (recommended).",
                                                     "Stay on the previous CK major."] );
    }

    [Test]
    public void restored_state_names_the_stash_and_the_drop_command()
    {
        var text = Render( AngularUndoOutcome.Restored );

        text.ShouldContain( $"The app folder '{_app}' has been restored." );
        text.ShouldContain( _stash );
        text.ShouldContain( "git stash drop \"stash@{<n>}\"" );
        text.ShouldNotContain( "git stash push" );
        text.ShouldNotContain( "yarn install" );
    }

    [Test]
    public void restored_state_without_a_stash_says_the_folder_is_as_it_was()
    {
        var text = Render( AngularUndoOutcome.Restored, stashCreated: false );

        text.ShouldContain( $"The app folder '{_app}' is as it was: the upgrade had changed no tracked file, so the undo created no stash." );
        text.ShouldNotContain( _stash );
        text.ShouldNotContain( "git stash" );
        text.ShouldNotContain( "yarn install" );
    }

    [Test]
    public void failed_install_after_a_restore_names_the_stash_and_says_to_install_again()
    {
        var text = Render( AngularUndoOutcome.RestoredInstallFailed );

        text.ShouldContain( $"The files of the app folder '{_app}' have been restored, but 'yarn install' failed" );
        text.ShouldContain( "run 'yarn install' in the folder, or run the builder again, once the registry is reachable" );
        text.ShouldContain( _stash );
        text.ShouldContain( "git stash drop" );
        text.ShouldNotContain( "git stash push" );
    }

    [Test]
    public void failed_install_without_a_stash_says_the_folder_is_as_it_was_and_to_install_again()
    {
        var text = Render( AngularUndoOutcome.RestoredInstallFailed, stashCreated: false );

        text.ShouldContain( $"The app folder '{_app}' is as it was: the upgrade had changed no tracked file, so the undo created no stash." );
        text.ShouldContain( "run 'yarn install' in the folder, or run the builder again, once the registry is reachable" );
        text.ShouldNotContain( _stash );
        text.ShouldNotContain( "git stash" );
    }

    [TestCase( false )]
    [TestCase( true )]
    public void failed_undo_gives_the_commands_to_run( bool excludeRootGitIgnore )
    {
        var text = Render( AngularUndoOutcome.NotRestored, stashCreated: false, excludeRootGitIgnore );
        var lines = text.Split( '\n' ).Select( l => l.TrimEnd( '\r' ) ).ToList();

        text.ShouldContain( $"The app folder '{_app}' has not been restored." );
        var expected = $"git stash push --include-untracked -m \"{_stash}\" -- . \":(exclude)ck-gen\" \":(exclude).yarn\" \":(exclude).yarnrc.yml\" \":(exclude).ck-angular-upgrade\""
                       + (excludeRootGitIgnore ? " \":(exclude).gitignore\"" : "");
        int stash = lines.IndexOf( expected );
        stash.ShouldBeGreaterThan( 0 );
        lines[stash + 1].ShouldBe( "yarn install" );
        lines[stash + 2].ShouldBe( "Remove-Item .ck-angular-upgrade" );
        text.ShouldNotContain( "git stash drop" );
    }

    [TestCase( "Restored", true, "git stash drop" )]
    [TestCase( "Restored", false, "created no stash" )]
    [TestCase( "RestoredInstallFailed", true, "git stash drop" )]
    [TestCase( "RestoredInstallFailed", false, "registry is reachable" )]
    [TestCase( "NotRestored", false, "Remove-Item .ck-angular-upgrade" )]
    public void parts_come_in_the_spec_order( string outcome, bool stashCreated, string stateEndText )
    {
        var text = Render( Enum.Parse<AngularUndoOutcome>( outcome ), stashCreated );

        int doing = text.IndexOf( "Upgrading from Angular 20 to 21: yarn ng update @angular/core --migrate-only" );
        int found = text.IndexOf( "The migration 'control-flow' of @angular/core failed." );
        int state = text.IndexOf( $"'{_app}'" );
        int stateEnd = text.IndexOf( stateEndText );
        int first = text.IndexOf( "1. Fix the app code the migration stops on" );
        int second = text.IndexOf( "2. Stay on the previous CK major." );
        doing.ShouldBe( 0 );
        found.ShouldBeGreaterThan( doing );
        state.ShouldBeGreaterThan( found );
        stateEnd.ShouldBeGreaterThan( state );
        first.ShouldBeGreaterThan( stateEnd );
        second.ShouldBeGreaterThan( first );
    }
}
