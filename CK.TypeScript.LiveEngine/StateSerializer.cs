using CK.BinarySerialization;
using CK.Core;
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.LiveEngine;


static class StateSerializer
{
    internal static LiveState? ReadLiveState( IActivityMonitor monitor,
                                              IBinaryDeserializer d,
                                              string targetPath )
    {
    }

    internal static bool WriteFile( IActivityMonitor monitor, NormalizedPath filePath, Action<IActivityMonitor,CKBinaryWriter> write )
    {
        try
        {
            using( var file = File.Create( filePath ) )
            using( var w = new CKBinaryWriter( file ) )
            {
                write( monitor, w );
            }
            return true;
        }
        catch( Exception e )
        {
            monitor.Error( e );
            return false;
        }
    }

    internal static T? ReadFile<T>( IActivityMonitor monitor,
                                    NormalizedPath filePath,
                                    Func<IActivityMonitor,CKBinaryReader,T?> read ) where T : class
    {
        try
        {
            if( !File.Exists( filePath ) )
            {
                monitor.Error( $"Missing '{filePath}' file." );
                return null;
            }
            using( var file = File.OpenRead( filePath ) )
            using( var r = new CKBinaryReader( file ) )
            {
                return read( monitor, r );
            }
        }
        catch( Exception e )
        {
            monitor.Error( e );
            return null;
        }
    }

}
