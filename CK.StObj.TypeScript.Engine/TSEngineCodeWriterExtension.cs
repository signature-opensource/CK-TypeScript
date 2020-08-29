using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Diagnostics;
using CK.Setup;
using CK.Core;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Provides Append fluent extension methods to <see cref="ITSCodeWriter"/> specializations.
    /// </summary>
    public static class TSEngineCodeWriterExtensions
    {
        /// <summary>
        /// Appends an enum definition. The underlying type should be safely convertible into Int32.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="enumType">The enum type.</param>
        /// <param name="typeName">Teh TypeScript type name.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T AppendEnumDefinition<T>( this T @this, IActivityMonitor monitor, Type enumType, string typeName ) where T : ITSCodeWriter
        {
            if( !enumType.IsEnum ) throw new ArgumentException( $"Must be an enum: {enumType.Name}.", nameof( enumType ) );
            var uT = enumType.GetEnumUnderlyingType();
            if( uT == typeof(UInt32) || uT == typeof(Int64) || uT == typeof(UInt64) )
            {
                monitor.Error( $"Enum: {enumType.Name} cannot be generated as TypeScript since it is based on {uT.Name} type. Only types that can be safely converted into Int32 should be used." );
                return @this;
            }
            return @this.Append( "enum " ).Append( typeName )
                .OpenBlock()
                .Append( b =>
                {
                    string[] names = Enum.GetNames( enumType );
                    int[] values = Enum.GetValues( enumType ).Cast<object>().Select( x => Convert.ToInt32( x ) ).ToArray();
                    for( int i = 0; i < names.Length; ++i )
                    {
                        if( i > 0 ) b.Append( "," ).NewLine();
                        b.Append( names[i] ).Append( " = " ).Append( values[i] );
                    }
                } )
                .CloseBlock();
        }

    }
}
