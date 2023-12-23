using CK.Core;
using System.Numerics;
using System;

namespace CK.TypeScript.CodeGen
{
    public sealed partial class TSTypeManager
    {
        /// <summary>
        /// Registers the standard types.
        /// String and booleans are always mapped to <see cref="ITSType"/> with a '' (empty string) and
        /// false default values.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="withNumbers">When true, byte, sbyte, short, ushort, int and uint are mapped to Number with a 0 default.</param>
        /// <param name="withBigInts">When true, long, ulong, BigInteger and decimal are mapped to BigInt with a 0n default.</param>
        /// <param name="withLuxonTypes">When true, DateTime, DateTimeOffset and TimeSpan are mapped to Luxon's DatTime and Duration types.</param>
        /// <param name="withGeneratedGuid">When true, Guid is mapped to a simple Guid class that wraps a string in folder System/Guid.ts.</param>
        public void RegisterStandardTypes( IActivityMonitor monitor,
                                           bool withNumbers = true,
                                           bool withBigInts = true,
                                           bool withLuxonTypes = true,
                                           bool withGeneratedGuid = true )
        {
            _types.Add( typeof( string ), new TSStringType() );
            _types.Add( typeof( bool ), new TSBooleanType() );
            if( withNumbers )
            {
                var number = new TSNumberType();
                _types.Add( typeof( byte ), number );
                _types.Add( typeof( sbyte ), number );
                _types.Add( typeof( short ), number );
                _types.Add( typeof( ushort ), number );
                _types.Add( typeof( int ), number );
                _types.Add( typeof( uint ), number );
            }
            if( withBigInts )
            {
                var bigInt = new TSBigIntType();
                _types.Add( typeof( long ), bigInt );
                _types.Add( typeof( ulong ), bigInt );
                _types.Add( typeof( BigInteger ), bigInt );
                _types.Add( typeof( decimal ), bigInt );
            }
            if( withLuxonTypes )
            {
                var luxonTypesLib = RegisterLibrary( monitor, "@types/luxon", DependencyKind.DevDependency, "^3.1.0" );
                var luxonLib = RegisterLibrary( monitor, "@types/luxon", DependencyKind.DevDependency, "^3.1.1", luxonTypesLib );
                var dateTime = new TSLuxonDateTime( luxonLib );

                _types.Add( typeof( DateTime ), dateTime );
                _types.Add( typeof( DateTimeOffset ), dateTime );
                _types.Add( typeof( TimeSpan ), new TSLuxonDuration( luxonLib ) );
            }
            if( withGeneratedGuid )
            {
                var fGuid = _root.Root.FindOrCreateFile( "System/Guid.ts" );
                var tGuid = new TSGeneratedType( typeof( Guid ), "Guid", fGuid, "Guid.empty", ( w, o ) =>
                {
                    if( o is Guid g )
                    {
                        w.Append( "new Guid(" ).AppendSourceString( g.ToString() ).Append( ")" );
                        return true;
                    }
                    return false;
                }, null );
                var code = tGuid.EnsureTypePart().Append( @"
/**
* Simple immutable encapsulation of a string. No check is currently done on the 
* value format but it should be in the '00000000-0000-0000-0000-000000000000' form.
*/
export class Guid {

    /**
    * The empty Guid is '00000000-0000-0000-0000-000000000000'.
    */
    public static readonly empty : Guid = new Guid('00000000-0000-0000-0000-000000000000');
    
    constructor( public readonly guid: string ) {
    }

    get value() {
        return this.guid;
      }

    toString() {
        return this.guid;
      }

    toJSON() {
        return this.guid;
      }
}
" );
                _types.Add( typeof( Guid ), tGuid );
            }
        }

    }

}

