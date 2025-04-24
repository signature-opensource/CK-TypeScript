//using CK.BinarySerialization;
//using CK.Transform.Core;
//using System.Collections.Generic;
//using System.ComponentModel;

//namespace CK.Core;

//[SerializationVersion( 0 )]
//sealed partial class TFunctionSource : ICKSlicedSerializable
//{
//    [EditorBrowsable( EditorBrowsableState.Never )]
//    public TFunctionSource( IBinaryDeserializer d, ITypeReadInfo info )
//        : base( Sliced.Instance )
//    {
//        _functions = d.ReadObject<List<TFunction>>();
//        _languageHintIndex = d.Reader.ReadSmallInt32();
//        _sourceName = d.Reader.ReadString();
//    }

//    public static void Write( IBinarySerializer s, in TFunctionSource o )
//    {
//        Throw.DebugAssert( o.IsInitialized );
//        s.WriteObject( o._functions );
//        s.Writer.WriteSmallInt32( o._languageHintIndex );
//        s.Writer.Write( o._sourceName );
//    }

//    internal void PostDeserialization( IActivityMonitor monitor, TransformEnvironment environment )
//    {
//        var functions = environment.TransformerHost.TryParseFunctions( monitor, Text );
//        if( functions == null )
//        {
//            Throw.CKException( $"""
//                Unable to parse {_functions.Count} functions from:
//                {Text}
//                """ );
//        }
//        if( _functions.Count != functions.Count )
//        {
//            Throw.CKException( $"""
//                Expected {_functions.Count} functions but parsed {functions.Count} from:
//                {Text}
//                """ );
//        }
//        for( int i = 0; i < functions.Count; i++ )
//        {
//            _functions[i].SetFunction( functions[i] );
//        }
//    }

//}
