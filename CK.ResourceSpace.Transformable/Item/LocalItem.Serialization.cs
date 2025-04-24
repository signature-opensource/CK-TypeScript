using CK.BinarySerialization;
using System.ComponentModel;

namespace CK.Core;

sealed partial class LocalItem
{
    [EditorBrowsable( EditorBrowsableState.Never )]
    public LocalItem( IBinaryDeserializer d, ITypeReadInfo info )
        : base( Sliced.Instance )
    {
        _prev = d.ReadNullableObject<ILocalInput>();
        _next = d.ReadNullableObject<ILocalInput>();
    }

    public static void Write( IBinarySerializer s, in LocalItem o )
    {
        s.WriteNullableObject( o._prev );
        s.WriteNullableObject( o._next );
    }
}
