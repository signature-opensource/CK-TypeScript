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
        /// <para>
        /// <see cref="decimal"/> is mapped to an external library that must export a 'Decimal' type.
        /// This is driven by <see cref="TypeScriptRoot.DecimalLibraryName"/> that defaults to https://github.com/MikeMcl/decimal.js-light/
        /// but https://www.npmjs.com/package/decimal.js ca also be used.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="withNumbers">When true, byte, sbyte, short, ushort, int and uint are mapped to Number with a 0 default.</param>
        /// <param name="withBigInts">When true, long, ulong, BigInteger are mapped to BigInt with a 0n default.</param>
        /// <param name="withDecimal">When true, decimal is mapped to the 'Decimal' type of <see cref="TypeScriptRoot.DecimalLibraryName"/>.</param>
        /// <param name="withLuxonTypes">When true, DateTime, DateTimeOffset and TimeSpan are mapped to Luxon's DatTime and Duration types.</param>
        public void RegisterStandardTypes( IActivityMonitor monitor,
                                           bool withNumbers = true,
                                           bool withBigInts = true,
                                           bool withDecimal = true,
                                           bool withLuxonTypes = true )
        {
            Throw.CheckState( GenerateCodeDone is false );
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
            }
            if( withDecimal )
            {
                var decimalLib = _root.LibraryManager.RegisterDefaultOrConfiguredDecimalLibrary( monitor );
                var tsDecimal = new TSDecimalType( this, decimalLib );
                RegisterValueType<decimal>( tsDecimal );
            }
            if( withLuxonTypes )
            {
                var luxonLib = _root.LibraryManager.RegisterLuxonLibrary( monitor );
                var dateTime = new TSLuxonDateTime( this, luxonLib );

                // Luxon DateTime handles .Net DateTime and DateTimeOffset.
                RegisterValueType<DateTime>( dateTime );
                RegisterValueType<DateTimeOffset>( dateTime );
                // TimeSpan is Duration (works on milliseconds only).
                RegisterValueType<TimeSpan>( new TSLuxonDuration( this, luxonLib ) );
            }
        }

    }

}

