using CK.BinarySerialization;
using System.ComponentModel;

namespace CK.Core;

[SerializationVersion( 0 )]
sealed partial class TFunction : ICKSlicedSerializable
{
    [EditorBrowsable( EditorBrowsableState.Never )]
    public TFunction( IBinaryDeserializer d, ITypeReadInfo info )
    {
        _source = d.ReadObject<FunctionSource>();
        _functionName = d.Reader.ReadString();
        _target = d.ReadObject<ITransformable>();
        _nextFunction = d.ReadNullableObject<TFunction>();
        _prevFunction = d.ReadNullableObject<TFunction>();
        _transformableImpl.Read( d );
        // This is initialized by reparsing the FunctionSource.Text
        // in FunctionSource.PostDeserialization.
        _function = null!;
    }

    public static void Write( IBinarySerializer s, in TFunction o )
    {
        s.WriteObject( o._source );
        s.Writer.Write( o._functionName );
        s.WriteObject( o._target );
        s.WriteNullableObject( o._nextFunction );
        s.WriteNullableObject( o._prevFunction );
        o._transformableImpl.Write( s );
    }

}
