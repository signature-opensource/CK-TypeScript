using CK.BinarySerialization;
using System.ComponentModel;

namespace CK.Core;

[SerializationVersion( 0 )]
partial class TransformableItem : ICKSlicedSerializable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    [EditorBrowsable( EditorBrowsableState.Never )]
    protected TransformableItem( Sliced _ )
    {
    }
#pragma warning restore CS8618

    [EditorBrowsable( EditorBrowsableState.Never )]
    public TransformableItem( IBinaryDeserializer d, ITypeReadInfo info )
    {
        _resources = d.ReadObject<IResPackageResources>();
        _fullResourceName = d.Reader.ReadString();
        _text = d.Reader.ReadString();
        _targetPath = d.Reader.ReadString();
        _languageIndex = d.Reader.ReadNonNegativeSmallInt32();
        _nextInPackage = d.ReadNullableObject<TransformableItem>();
        _prevInPackage = d.ReadNullableObject<TransformableItem>();
        _transformableImpl.Read( d );
    }

    public static void Write( IBinarySerializer s, in TransformableItem o )
    {
        s.WriteObject( o._resources );
        s.Writer.Write( o._fullResourceName );
        s.Writer.Write( o._text );
        s.Writer.Write( o._targetPath );
        s.Writer.WriteNonNegativeSmallInt32( o._languageIndex );
        s.WriteNullableObject( o._nextInPackage );
        s.WriteNullableObject( o._prevInPackage );
        o._transformableImpl.Write( s );
    }
}
