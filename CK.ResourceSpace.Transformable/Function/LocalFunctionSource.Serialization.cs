using CK.BinarySerialization;
using System.ComponentModel;

namespace CK.Core;

[SerializationVersion( 0 )]
sealed partial class LocalFunctionSource
{
    [EditorBrowsable( EditorBrowsableState.Never )]
    public LocalFunctionSource( IBinaryDeserializer d, ITypeReadInfo info )
        : base( Sliced.Instance )
    {
    }

    public static void Write( IBinarySerializer s, in LocalFunctionSource o )
    {
    }
}
