using System.Collections.Generic;

namespace CK.Core;

sealed partial class LocalItem : TransformableItem, ILocalInput
{
    ILocalInput? _prev;
    ILocalInput? _next;

    public LocalItem( IResPackageResources resources,
                      string fullResourceName,
                      string text,
                      int languageIndex,
                      NormalizedPath targetPath )
        : base( resources, fullResourceName, text, languageIndex, targetPath )
    {
    }

    public string FullPath => _fullResourceName;

    public override bool IsLocalItem => true;

    ILocalInput? ILocalInput.Prev { get => _prev; set => _prev = value; }

    ILocalInput? ILocalInput.Next { get => _next; set => _next = value; }

    public bool InitializeApplyChanges( IActivityMonitor monitor,
                                        TransformEnvironment environment,
                                        ref List<LocalItem>? toBeRemoved )
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
            var f = FirstFunction;
            while( f != null )
            {
                environment.UnboundFunctions.Add( f );
                f = f.NextFunction;
            }
            toBeRemoved ??= new List<LocalItem>();
            toBeRemoved.Add( this );
        }
        return false;
    }

    public void ApplyChanges( IActivityMonitor monitor, TransformEnvironment environment, HashSet<LocalItem> toBeInstalled )
    {
        // If we are called, then we have changed and must be installed.
        toBeInstalled.Add( this );
    }


}
