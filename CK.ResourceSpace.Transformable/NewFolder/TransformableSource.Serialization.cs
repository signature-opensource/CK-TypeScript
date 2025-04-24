//using CK.BinarySerialization;
//using CK.EmbeddedResources;
//using CK.Transform.Core;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;

//namespace CK.Core;

//[SerializationVersion(0)]
//partial class TransformableSource : ICKSlicedSerializable
//{
//#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
//    protected TransformableSource( Sliced _ )
//#pragma warning restore CS8618
//    {
//    }

//    [EditorBrowsable(EditorBrowsableState.Never)]
//    public TransformableSource( IBinaryDeserializer d, ITypeReadInfo info )
//    {
//        _resources = d.ReadObject<IResPackageResources>();
//        _origin = d.ReadValue<ResourceLocator>();
//        _text = d.Reader.ReadString();
//    }

//    public static void Write( IBinarySerializer s, in TransformableSource o )
//    {
//        s.WriteObject( o._resources );
//        s.WriteValue( o._origin );
//        s.Writer.Write( o._text );
//    }


//}
