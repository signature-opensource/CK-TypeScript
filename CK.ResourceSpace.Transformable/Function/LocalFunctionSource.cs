
using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CK.Core;

sealed partial class LocalFunctionSource : FunctionSource, ILocalInput
{
    ILocalInput? _prev;
    ILocalInput? _next;

    public LocalFunctionSource( IResPackageResources resources, string fullResourceName, string text )
        : base( resources, fullResourceName, text )
    {
    }

    public string FullPath => _fullResourceName;

    ILocalInput? ILocalInput.Prev { get => _prev; set => _prev = value; }

    ILocalInput? ILocalInput.Next { get => _next; set => _next = value; }

    public bool InitializeApplyChanges( IActivityMonitor monitor, TransformEnvironment environment, ref HashSet<NormalizedPath>? removedTargets )
    {
        Throw.DebugAssert( environment.IsLive );
        if( ILocalInput.TryReadText( monitor, this, out var newText ) )
        {
            if( newText != null )
            {
                SetText( newText );
                // We cannot parse and install the new text now: the resolved target may be a disappearing one.
                // But we must remove the current functions to free the environment from them. 
                foreach( var f in Functions )
                {
                    // This doesn't touch the UnboundFunctions set.
                    f.Remove( environment );
                }
                return true;
            }
            // We return false, the text didn't change, we are done.
        }
        else
        {
            environment.Tracker.Remove( this );
            foreach( var f in Functions )
            {
                f.Remove( environment );
                f.Destroy( environment );
            }
        }
        return false;
    }

    public void ApplyChanges( IActivityMonitor monitor, TransformEnvironment environment, HashSet<LocalItem> toBeInstalled )
    {
        var updated = new BitArray( Functions.Count );
        var functions = Parse( monitor, environment, strict: false );
        if( functions != null )
        {
            foreach( var p in functions )
            {

            }
        }
        int idx = 0;
        for( int i = 0; i < updated.Count; ++i )
        {
            if( !updated[i] )
            {
                Functions[idx].Destroy( environment );
                Functions.RemoveAt( idx );
            }
            else
            {
                ++idx;
            }
        }
    }

}
