
using System.Collections;
using System.Collections.Generic;

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
        var functions = Functions;
        var updated = new BitArray( functions.Count );

        var preFunctions = Parse( monitor, environment, strict: false );
        if( preFunctions != null )
        {
            foreach( var p in preFunctions )
            {
                var idx = functions.IndexOf( f => f.Target == p.Target );
                TFunction f;
                if( idx < 0 )
                {
                    f = AddNewFunction( environment, p );
                }
                else
                {
                    f = functions[idx];
                    updated[idx] = true;
                    f.Update( p.F, p.Name );
                    environment.TransformFunctions.Add( p.Name, f );
                    p.Target.Add( f, p.Before );
                }
                toBeInstalled.Add( f.PeeledTarget );
            }
        }
        RemoveMissing( environment, functions, updated, toBeInstalled );

        static void RemoveMissing( TransformEnvironment environment, List<TFunction> functions, BitArray updated, HashSet<LocalItem> toBeInstalled )
        {
            int idx = 0;
            for( int i = 0; i < updated.Count; ++i )
            {
                if( !updated[i] )
                {
                    var f = functions[idx];
                    // This has already been called in InitializeApplyChanges.
                    // f.Remove( environment );
                    f.Destroy( environment );
                    functions.RemoveAt( idx );
                    toBeInstalled.Add( f.PeeledTarget );
                }
                else
                {
                    ++idx;
                }
            }
        }
    }

}
