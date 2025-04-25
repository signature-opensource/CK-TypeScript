
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CK.Core;

sealed partial class LocalItem : TransformableItem, ILocalInput
{
    ILocalInput? _prev;
    ILocalInput? _next;

    public LocalItem( IResPackageResources resources,
                      string fullResourceName,
                      int languageIndex,
                      string text,
                      NormalizedPath targetPath )
        : base( resources, fullResourceName, languageIndex, text, targetPath )
    {
    }

    public string FullPath => _fullResourceName;

    ILocalInput? ILocalInput.Prev { get => _prev; set => _prev = value; }

    ILocalInput? ILocalInput.Next { get => _next; set => _next = value; }

    public bool InitializeApplyChanges( IActivityMonitor monitor,
                                        TransformEnvironment environment,
                                        ref HashSet<NormalizedPath>? removedTargets )
    {
        Throw.DebugAssert( environment.IsLive );
        if( ILocalInput.TryReadText( monitor, this, out var newText ) )
        {
            if( newText != null )
            {
                SetText( newText );
                return true;
            }
            // We return false, the text didn't change, we are done.
        }
        else
        {
            environment.Tracker.Remove( this );
            Throw.DebugAssert( environment.Items.TryGetValue( TargetPath, out var found ) && found == this );
            environment.Items.Remove( TargetPath );
            removedTargets ??= new HashSet<NormalizedPath>();
            removedTargets.Add( TargetPath );
            var f = FirstFunction;
            while( f != null )
            {
                environment.UnboundFunctions.Add( f );
                f = f.NextFunction;
            }
        }
        return false;
    }

    public void ApplyChanges( IActivityMonitor monitor, TransformEnvironment environment, HashSet<LocalItem> toBeInstalled )
    {
        // If we are called, then we have changed and must be installed.
        toBeInstalled.Add( this );
    }


}
