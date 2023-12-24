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
            RegisterValueType<bool>( new TSBooleanType() );
            if( withNumbers )
            {
                var number = new TSNumberType();
                RegisterValueType<byte>( number );
                RegisterValueType<sbyte>( number );
                RegisterValueType<short>( number );
                RegisterValueType<ushort>( number );
                RegisterValueType<int>( number );
                RegisterValueType<uint>( number );
            }
            if( withBigInts )
            {
                var bigInt = new TSBigIntType();
                RegisterValueType<long>( bigInt );
                RegisterValueType<ulong>( bigInt );
                RegisterValueType<BigInteger>( bigInt );
                RegisterValueType<decimal>( bigInt );
            }
            if( withLuxonTypes )
            {
                var luxonTypesLib = RegisterLibrary( monitor, "@types/luxon", DependencyKind.DevDependency, "^3.1.0" );
                var luxonLib = RegisterLibrary( monitor, "@types/luxon", DependencyKind.DevDependency, "^3.1.1", luxonTypesLib );
                var dateTime = new TSLuxonDateTime( luxonLib );

                RegisterValueType<DateTime>( dateTime );
                RegisterValueType<DateTimeOffset>( dateTime );
                RegisterValueType<TimeSpan>( new TSLuxonDuration( luxonLib ) );
            }
            if( withGeneratedGuid )
            {
                // Another way to register a type by creating a TSGeneratedType bound to a
                // TypeScriptFile, a default value source code, a value writer function and
                // by configuring its TypePart with its implementation.
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
                tGuid.EnsureTypePart().Append( @"
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
                RegisterValueType<Guid>( tGuid );
            }
        }
    }

}

