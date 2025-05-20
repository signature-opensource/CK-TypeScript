using CK.BinarySerialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CK.Core;

[SerializationVersion( 0 )]
sealed partial class LocalFunctionSource
{
    [EditorBrowsable( EditorBrowsableState.Never )]
    public LocalFunctionSource( IBinaryDeserializer d, ITypeReadInfo info )
        : base( Sliced.Instance )
    {
        _prev = d.ReadNullableObject<ILocalInput>();
        _next = d.ReadNullableObject<ILocalInput>();
    }

    public static void Write( IBinarySerializer s, in LocalFunctionSource o )
    {
        s.WriteNullableObject( o._prev );
        s.WriteNullableObject( o._next );
    }


    internal void OnAppear( IActivityMonitor monitor, TransformEnvironment environment, HashSet<TransformableItem> toBeInstalled )
    {
        foreach( var f in Functions )
        {
            environment.Rebind( monitor, f );
            toBeInstalled.Add( f.PeeledTarget );
        }
    }
}
