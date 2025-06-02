using CK.BinarySerialization;
using System.Collections.Generic;
using System.ComponentModel;

namespace CK.Core;

[SerializationVersion( 0 )]
partial class FunctionSource : ICKSlicedSerializable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    [EditorBrowsable( EditorBrowsableState.Never )]
    protected FunctionSource( Sliced _ )
    {
    }
#pragma warning restore CS8618

    [EditorBrowsable( EditorBrowsableState.Never )]
    public FunctionSource( IBinaryDeserializer d, ITypeReadInfo info )
    {
        _resources = d.ReadObject<IResPackageResources>();
        _fullResourceName = d.Reader.ReadString();
        _text = d.Reader.ReadString();
        _functions = d.ReadObject<List<TFunction>>();
        _languageHintIndex = d.Reader.ReadSmallInt32();
        _sourceName = d.Reader.ReadString();
    }

    public static void Write( IBinarySerializer s, in FunctionSource o )
    {
        Throw.DebugAssert( o.IsInitialized );
        s.WriteObject( o._resources );
        s.Writer.Write( o._fullResourceName );
        s.Writer.Write( o._text );
        s.WriteObject( o._functions );
        s.Writer.WriteSmallInt32( o._languageHintIndex );
        s.Writer.Write( o._sourceName );
    }

    internal void PostDeserialization( IActivityMonitor monitor, TransformEnvironment environment )
    {
        var functions = environment.TransformerHost.TryParseFunctions( monitor, Text );
        if( functions == null )
        {
            Throw.CKException( $"""
                Unable to parse {_functions.Count} functions from:
                {Text}
                """ );
        }
        if( _functions.Count != functions.Count )
        {
            // Removes from the parsed functions the functions
            // that target external items: external item transfomers
            // are not registered in their source and not serialized.
            for( int i = 0; i < functions.Count; ++i )
            {
                var target = functions[i].Target;
                if( target != null && target.StartsWith("../") )
                {
                    monitor.Trace( $"Ignoring external item transformer on '{target}'." );
                    functions.RemoveAt( i-- );
                }
            }
            // Now, we must have as much parsed functions as serialized functions.
            if( _functions.Count != functions.Count )
            {
                Throw.CKException( $"""
                        Expected {_functions.Count} functions but parsed {functions.Count} from:
                        {Text}
                        """ );
            }
        }
        // Updates the functions with their transformer function.
        for( int i = 0; i < functions.Count; i++ )
        {
            var f = _functions[i];
            f.Update( functions[i], f.FunctionName );
        }
    }

}
