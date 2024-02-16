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
        public void RegisterStandardTypes( IActivityMonitor monitor,
                                           bool withNumbers = true,
                                           bool withBigInts = true,
                                           bool withLuxonTypes = true )
        {
            _types.Add( typeof( string ), new TSStringType( this ) );
            RegisterValueType<bool>( new TSBooleanType( this ) );
            if( withNumbers )
            {
                var number = new TSNumberType( this );
                RegisterValueType<byte>( number );
                RegisterValueType<sbyte>( number );
                RegisterValueType<short>( number );
                RegisterValueType<ushort>( number );
                RegisterValueType<int>( number );
                RegisterValueType<uint>( number );
                RegisterValueType<float>( number );
                RegisterValueType<double>( number );
                RegisterValueType<Half>( number );
            }
            if( withBigInts )
            {
                var bigInt = new TSBigIntType( this );
                RegisterValueType<long>( bigInt );
                RegisterValueType<ulong>( bigInt );
                RegisterValueType<BigInteger>( bigInt );
                RegisterValueType<decimal>( bigInt );
            }
            if( withLuxonTypes )
            {
                var luxonTypesLib = RegisterLibrary( monitor, "@types/luxon", DependencyKind.DevDependency, TypeScriptRoot.LuxonTypesVersion );
                var luxonLib = RegisterLibrary( monitor, "luxon", DependencyKind.Dependency, TypeScriptRoot.LuxonVersion, luxonTypesLib );
                var dateTime = new TSLuxonDateTime( this, luxonLib );

                RegisterValueType<DateTime>( dateTime );
                RegisterValueType<DateTimeOffset>( dateTime );
                RegisterValueType<TimeSpan>( new TSLuxonDuration( this, luxonLib ) );
            }
        }

    }

}

